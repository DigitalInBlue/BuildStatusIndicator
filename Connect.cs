/// Copyright 2015 John Farrier 
/// Apache 2.0 License

using System;
using Extensibility;
using EnvDTE;
using EnvDTE80;

namespace BuildStatusIndicator
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.ComponentModel;
	using ThingM;
	using ThingM.Blink1;
	using ThingM.Blink1.ColorProcessor;

	/// <summary>A thread to control the blink(1) device.</summary>
	public class BlinkWorker : BackgroundWorker
	{
		// The Blink1 Device (just open the first one found.)
		private Blink1 blink1 = new Blink1();
		private ushort[] ColorClean = new ushort[] { 255, 255, 255 };
		private ushort[] ColorFinished = new ushort[] { 0, 255, 0 };
		private ushort[] ColorFinishedWithErrors = new ushort[] { 255, 0, 0 };
		private ushort[] ColorWarning = new ushort[] { 255, 255, 0 };
		private ushort[] ColorBuilding = new ushort[] { 0, 255, 255 };
		private ushort[] ColorBuildingWithErrors = new ushort[] { 255, 0, 255 };
		private uint PresetBuildingNoErrors = 0;
		private uint PresetBuildingWithErrors = 1;
		private uint PresetFinishedNoErrors = 2;
		private uint PresetFinishedWithErrors = 3;
		private bool FadeIn = true;

		enum BlinkState
		{
			Boot,
			Idle,
			Clean,
			Building,
			BuildingWithErrors,
			Finished,
			FinishedWithErrors,
			Exit
		} 

		private volatile BlinkState blinkState = BlinkState.Boot;

		/// <summary></summary>
		public BlinkWorker()
		{
			blink1.Open();
			blink1.FadeToColor(2000, 64, 64, 64, true);
		}

		/// <summary></summary>
		public void Idle()
		{
			blinkState = BlinkState.Idle;
		}

		/// <summary></summary>
		public void Clean()
		{
			blinkState = BlinkState.Clean;
		}

		/// <summary></summary>
		public void Building()
		{
			blinkState = BlinkState.Building;
		}

		/// <summary></summary>
		public void BuildingWithErrors()
		{
			blinkState = BlinkState.BuildingWithErrors;
		}

		/// <summary></summary>
		public void Finished()
		{
			blinkState = BlinkState.Finished;
		}

		/// <summary></summary>
		public void FinishedWithErrors()
		{
			blinkState = BlinkState.FinishedWithErrors;
		}

		/// <summary></summary>
		public void Exit()
		{
			blinkState = BlinkState.Exit;
		}

		protected override void OnDoWork(DoWorkEventArgs e)
		{
			ushort fadeTime = 1000;
			ushort fadeTime2 = 2500;
			ushort finalStateCounter = 0;

			while(blinkState != BlinkState.Exit)
			{
				switch(blinkState)
				{
					case BlinkState.Boot:
						blink1.FadeToColor(fadeTime, 255, 255, 255, true);
						blink1.FadeToColor(fadeTime, 16, 16, 16, true);
						blinkState = BlinkState.Idle;
						break;

					case BlinkState.Idle:
						if (FadeIn == true)
						{
							blink1.FadeToColor(fadeTime2, 16, 16, 32, true);
						}
						else
						{
							blink1.FadeToColor(fadeTime2, 16, 32, 16, true);
						}
						break;

					case BlinkState.Clean:
						if (FadeIn == true)
						{
							blink1.FadeToColor(fadeTime, ColorClean[0], ColorClean[1], ColorClean[2], true);
						}
						else
						{
							blink1.FadeToColor(fadeTime, 0, 0, 0, true);
						}
						break;
					case BlinkState.Building:
						if (FadeIn == true)
						{
							blink1.FadeToColor(fadeTime, ColorBuilding[0], ColorBuilding[1], ColorBuilding[2], true);
						}
						else
						{
							blink1.FadeToColor(fadeTime, 0, 32, 0, true);
						}
						break;
					case BlinkState.BuildingWithErrors:
						if (FadeIn == true)
						{
							blink1.FadeToColor(fadeTime, ColorBuildingWithErrors[0], ColorBuildingWithErrors[1], ColorBuildingWithErrors[2], true);
						}
						else
						{
							blink1.FadeToColor(fadeTime, 32, 0, 32, true);
						}
						break;
					case BlinkState.Finished:
						if (FadeIn == true)
						{
							blink1.FadeToColor(fadeTime, ColorFinished[0], ColorFinished[1], ColorFinished[2], true);
						}
						else
						{
							blink1.FadeToColor(fadeTime, ColorFinished[0], ColorFinished[2], ColorFinished[1], true);
						}
						finalStateCounter++;
						break;
					case BlinkState.FinishedWithErrors:
						if (FadeIn == true)
						{
							blink1.FadeToColor(fadeTime, ColorFinishedWithErrors[0], ColorFinishedWithErrors[1], ColorFinishedWithErrors[2], true);
						}
						else
						{
							blink1.FadeToColor(fadeTime, ColorFinishedWithErrors[0], ColorFinishedWithErrors[2], ColorFinishedWithErrors[1], true);
						}
						break;
						finalStateCounter++;
					case BlinkState.Exit:
						break;
				}

				FadeIn = !FadeIn;

				if(finalStateCounter == 128)
				{
					finalStateCounter = 0;
					blinkState = BlinkState.Idle;
				}
			}

			blink1.Close();
		}

		protected override void Finalize()
		{
			blink1.Close();
		}

	}

	/// <summary></summary>
	public class Connect : IDTExtensibility2
	{
		// Used to display progress
		private int _projectProgressPercentagePoints;
		private int _nextProgressValue;
		private int _maxProgressValue;

		// Did one of the projects fail to build?
		private bool _buildErrorDetected;

		private BuildEvents _buildEvents;
		private Events _events;

		private BlinkWorker blinkWorker = new BlinkWorker();

		// VS Interface Functions
		private void OnBuildBegin(vsBuildScope scope, vsBuildAction action)
		{
			_buildErrorDetected = false;
			UpdateProgressValue(false);
		}

		private void OnBuildProjConfigDone(string Project, string ProjectConfig, string Platform, string SolutionConfig, bool Success)
		{
			UpdateProgressValue(!Success);
		}

		private void UpdateProgressValue(bool errorThrown)
		{
			if (errorThrown)
			{
				_buildErrorDetected = true;
			}

			if(_buildErrorDetected == true)
			{
				blinkWorker.BuildingWithErrors();
			}
			else
			{
				blinkWorker.Building();
			}
		}

		private void OnBuildDone(vsBuildScope scope, vsBuildAction action)
		{
			if (action == vsBuildAction.vsBuildActionClean)
			{
				blinkWorker.Clean();
			}
			else
			{
				if (_buildErrorDetected)
				{
					// JEF // TaskbarManager.Instance.SetOverlayIcon((Icon)Resources.ResourceManager.GetObject("cross"), "Build Failed");
					blinkWorker.FinishedWithErrors();
				}
				else
				{
					// JEF // TaskbarManager.Instance.SetOverlayIcon((Icon)Resources.ResourceManager.GetObject("tick"), "Build Succeeded");
					blinkWorker.Finished();
				}
			}
		}

		// VS-Generated Functions

		/// <summary>Implements the constructor for the Add-in object. Place your initialization code within this method.</summary>
		public Connect()
		{
			this.blinkWorker.RunWorkerAsync();
		}

		/// <summary>Implements the OnConnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being loaded.</summary>
		/// <param term='application'>Root object of the host application.</param>
		/// <param term='connectMode'>Describes how the Add-in is being loaded.</param>
		/// <param term='addInInst'>Object representing this Add-in.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
		{
			_applicationObject = (DTE2)application;
			_addInInstance = (AddIn)addInInst;

			_events = _applicationObject.Events;
			_buildEvents = _events.BuildEvents;

			_buildEvents.OnBuildBegin += OnBuildBegin;
			_buildEvents.OnBuildDone += OnBuildDone;
			_buildEvents.OnBuildProjConfigDone += OnBuildProjConfigDone;			
		}

		/// <summary>Implements the OnDisconnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being unloaded.</summary>
		/// <param term='disconnectMode'>Describes how the Add-in is being unloaded.</param>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
		{
			blinkWorker.Exit();
			blinkWorker.CancelAsync();
		}

		/// <summary>Implements the OnAddInsUpdate method of the IDTExtensibility2 interface. Receives notification when the collection of Add-ins has changed.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />		
		public void OnAddInsUpdate(ref Array custom)
		{
		}

		/// <summary>Implements the OnStartupComplete method of the IDTExtensibility2 interface. Receives notification that the host application has completed loading.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnStartupComplete(ref Array custom)
		{
		}

		/// <summary>Implements the OnBeginShutdown method of the IDTExtensibility2 interface. Receives notification that the host application is being unloaded.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnBeginShutdown(ref Array custom)
		{
		}
		
		private DTE2 _applicationObject;
		private AddIn _addInInstance;
	}
}