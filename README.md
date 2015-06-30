#Build Status Indicator

A Microsoft Visual Studio Addin that uses the Blink(1).mk2 USB LED project to indicate build status with pulsating colors of light.

Copyright 2015 John Farrier 
Apache 2.0 License

###Building
I wasn't too fancy setting this up.  First, grab the blink(1).mk2 SDK from https://github.com/todbot/blink1.
Next, check out the BuildStatusIndicator project to:
```
blink1/windows/ManagedBlink1
```
Open the ManagedBlink1.sln file and add the BuildStatusIndicator project.  Compile and you're done.

###Installing

When compiled, copy these files to your user addins directory:

```
C:\Users\John\Documents\Visual Studio 2012\Addins 
```
or
```
C:\Users\John\Documents\Visual Studio 2013\Addins 
```

Then, inside Visual Studio, go to Tools->AddIn Manager and you should see the BuildStatusIndicator plugin.
