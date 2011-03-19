Windows Mobile Advanced Navigon Starter
===============

When starting Navigon on my HTC Diamond I had a manual workflow which begged for
a automatic solution.

I could have started with MortScript, but I dislike it due to limitations in
having a shortcut with custom icon.
I created this simple .NET Compact Framework application to do the following
things:

  - Disable the memory consuming HTC Touch additions (Manila)
  - Disable Titanium home screen for Windows Mobile 6.5 (CHome)
  - Rotate the screen to have Navigon to support Reality View (primary reason
	for choosing Navigon over TomTom)
  - Edit config file to switch between internal and external GPS
  - Enable Bluetooth for the external GPS
  - Have the appplication as a CAB file so that it can be installed easily

The rotation and GPS selection is asked every time the program starts, and when
it closes (or crashes) it tries to restore as much as possible.

TODO
-----
At the moment a lot is static (Navigon location, COM ports, rotation angle) but
when I have the time, I plan on adding a configuration file and a interface
around that.
