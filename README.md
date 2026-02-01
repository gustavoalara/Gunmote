
<img width="600" height="600" alt="image" src="https://github.com/user-attachments/assets/a4a4a11d-7abc-4c0a-9e6d-0633e93d5e46" />


![Descargas última versión](https://img.shields.io/github/downloads/gustavoalara/Gunmote/latest/total?style=for-the-badge&color=orange&logo=github&label=Last-Version-Downloads) ![Descargas totales](https://img.shields.io/github/downloads/gustavoalara/Gunmote/total?style=for-the-badge&color=green&logo=github&label=Total-Downloads)

Gunmote
==============
Introducing Gunmote, the evolution of the Touchmote application created by Symphax and improved by Ryochan7, Suegrini, and others, now focused on using up to 4 Wiimotes on your computer as lightguns with precision and many options.

Aim, move, and shoot with your Wiimote at the screen or HD TV.

Gunmote is an evolution of Touchmote, which was based on the WiiTUIO project, which allows Wii controller data to be translated into genuine Windows touch and motion events.

The position where the Wiimote is pointing is calculated using a Wii sensor bar (up or down), two Wii sensor bars (one up and one down), four infrared LEDs in a square arrangement, or four infrared LEDs in a diamond arrangement.

The application is developed mainly in C# .NET 4.8 and some C++.


Prerequisites
==============
At least:

1x Nintendo Wii Remote<br />
1x Wireless Wii Sensor Bar or 4 IR LEDs in diamond or square arrangement<br />
1x Bluetooth ( 4.x or lower) enabled computer with Windows 8/10/11<br />
Visual C++ Runtimes<br />
.Net 4.8<br />

Bug reports
==============
Please use the GitHub Issue tracker to report bugs. Always include the following information:<br />
1. System configuration, including Bluetooth device vendor and model<br />
2. Steps to reproduce the error<br />
3. Expected output<br />
4. Actual output<br />

How to build
==============
*First install:*  
Microsoft Visual Studio 2019 or higher  
Install **.NET desktop development** Workload  
Install **Desktop development with C++** Workload  
Direct X SDK

1. Install the Touchmote drivers and test certificate by running the installer from this repo<br />
2. Run Visual Studio "as Administrator". Open the project file Touchmote.sln. <br />
3. If you want to use the debugger, edit the file called app.manifest and change uiAccess to false. Otherwise the app has to be run under Program Files. This is for the cursor to be able to show on top of the Modern UI.<br />
4. Go to Build->Configuration manager...<br />
5. Choose solution platform for either x86 or x64 depending on your system. Close it and Build.<br />

Credits
==============
WiimoteLib 1.7:  	http://wiimotelib.codeplex.com/<br />
WiiTUIO project:	http://code.google.com/p/wiituio/<br />
TouchInjector:	  http://touchinjector.codeplex.com/<br />
Scarlet.Crush Xinput wrapper:   http://forums.pcsx2.net/Thread-XInput-Wrapper-for-DS3-and-Play-com-USB-Dual-DS2-Controller<br />
WiiPair:  				http://www.richlynch.com/code/wiipair<br />
EcoTUIOdriver:    https://github.com/ecologylab/EcoTUIODriver<br />
MultiTouchVista:  http://multitouchvista.codeplex.com<br />
OpenFIRE:  https://github.com/TeamOpenFIRE/OpenFIRE-Firmware<br />
Symphax: https://github.com/simphax/Touchmote<br />
Ryochan7: https://github.com/Ryochan7/Touchmote <br />
Suegrini: https://github.com/Suegrini/Touchmote <br />

Translations
==============
If you would like to help translate into new languages, there are some Excel files with the strings to be translated in the translations folder. Send me your translation and I will gladly add it.

Release History
==============

**v1.1.0.5**<br />
- Experimental Calibration: Added experimental calibration for 0°, 90°, and -90° rotation (configurable in Advanced Settings).
- FPS Mouse Mode: Fully functional FPS Mouse Mode (parameters available in Advanced Settings).
- UI Improvements: Reorganized the "Advanced" section and renamed certain parameters for better clarity. Added tooltips that appear when hovering over settings.
- Audio Enhancements: Integrated WiimoteLib improvements for sound (though don't expect Hi-Fi quality). Including ffmpeg in Gunmote folder for sound conversion on-the-fly
- Debug Box: Moved the debug box 50px to the right and 50px down.
- New Profile: Added FPS Mouse Profile.
- Hotkey Toggle: Pressing HOME + PLUS now toggles cursor movement with the lightgun (the controller will vibrate to confirm the change).

**v1.1.0.4**<br />
- Fix issue with XInput D-Pad asignation to Nunchuk/Classic Stick

**v1.1.0.3**<br />
- Disable notifications during calibration to avoid odd behaviours
- Fix window size and behaviour when using minimized to tray or to taskbar

**v1.1.0.2**<br />
- Added 4:3 and 16:9 modes on mouse/cursor input. Remove "lightgun" from the mouse/cursor pointers.
- Added Getting Started video help and reminder message
  
**v1.1.0.1**<br />
- More fixes for vmulti devices control

**v1.1**<br />
- Update to .net 4.8.1.
- Fix problems with the ECOTuio drivers installation and multiple vmulti devices visibility
- Added Portuguese, Brazilian Portuguese, Basque and Galician to the list of supported languages
- Adjusted some shaking parameters
- Extended the autodisconnect timeout
- Replaced the “advanced” button with a more discreet and safer one.
  
**v1.0 beta 36**<br />
- Fix disabled keys not showing in other languages different to spanish
- Fix 4IR calibration after changing from 2IR
- Fix problem in Arcadehook that closed the aplication
- Updated About window with github links

**v1.0 beta 35**<br />
- Fix debug visualizer in 2IR calibration
- Calibration Margin X/Y 0 by default for better calibration results

**v1.0 beta 34**<br />
- 2IR now is working again and uses the new calibration system
- Consolidated Wiimote serial number in calibration profiles for 2IR mode as well
- Added executable search when linking an application
- Several other improvements in the calibration code
- Added sample inis for ArcadeHook

**v1.0 beta 33**<br />
- Total rework on the calibration system. The system now detects automatically the LED position, screens size, etc. and it's not needed manually reconfiguring parameters. Now it uses 5 shoots for any led arrangement and shows the LEDs and shape detected by the wiimote camera
- Now new profiles are linked to the selected LED arrangement when created 
- Internal code cleanup and improvement
  
**v1.0 beta 32**<br />
- Added the ability to display the system monitors in the monitor selection dropdown within the advanced settings screen. For now, this makes the overlays move to the selected screen (including the cursor and cursor lightgun modes). The mouse and mouse lightgun modes remain on the screen where the IR LEDs are located
  
**v1.0 beta 31**<br />
- Fixed an issue where Italian did not display correctly.
- Fixed autostart with windows not working
  
**v1.0 beta 30**<br />
- Added language selector in the UI.
- Fix language issues and size fields
- Modified default wiimote profiles for Mouse Lightgun and Default (XInput)
  
**v1.0 beta 29**<br />
- Name and logo change due to the removal of touch features and a focus on lightgun functionality primarily.
- Updates url changed
  
**v1.0 beta 28**<br />
- Fixed bad translation of "Right" key on Spanish and French

**v1.0 beta 27**<br />
- Improved updating system
- And sorry, I missed the Catalan translations
  
**v1.0 beta 26**<br />
- Added Catalan, Italian and German languages

**v1.0 beta 25**<br />
- Fix french language not working
- Added a grid on 2IR/4IR Square calibration to facilitate the placement of a wii bar
  
**v1.0 beta 24**<br />
- Added French language support thanks to Isma Nc
  
**v1.0 beta 23**<br />
- Fixed some problems in saving calibration data
- Updated overlay on calibration screen. Now a couple of lines have been added to facilitate the placement of the LEDs in each type of arrangement.

**v1.0 beta 22**<br />
- Added new calibration profiles creation capability

- **v1.0 beta 21**<br />
- Multilanguage on updates messages
- Shows compiled version automatically on about window
  
**v1.0 beta 20**<br />
- Multilanguage support implemented
- Check updates in this github repository
- Fix vmulti drivers installation issues
  
**v1.0 beta 17**<br />
- Added new advanced configuration window for all Suegrini's new parameters
  
**v1.0 beta 16**<br />
- Added 4 IR Leds initial support by Suegrini
- Improved 4 IR Leds diamond arrange support
- Performance improvements, thanks to all developers
- Spanish translation (sorry I'll work on multilanguage support soon)
- Multicommand support in Arcadehook
  
**v1.0 beta 15**<br />
- Fix touch input on Windows 10
- New smoothing algorithm, thanks DevNullx64
- Performance improvements, thanks yodus

**v1.0 beta 14**<br />
- FPS cursor mapping and cursor to stick mapping, thanks rVinor
- Updated OSD GUI

**v1.0 beta 13**<br />
- Less GPU usage
- Works together with other Xbox 360 controls
- Bug fixes

**v1.0 beta 12**<br />
- Classic Controller Pro support
- Raw input support
- Automatic check for new versions

**v1.0 beta 11**<br />
- Support for multiple monitors
- More possibilities with analog sticks
- Better pairing
- Bug fixes

**v1.0 beta 10**<br />
- Added visual keymap editor.
- Experimental Windows 7 support.

**v1.0 beta 9**<br />
- Nunchuk and Classic Controller support.
- XBox 360 controller emulation.
- Change keymaps on the fly. Hold the Home button for 5 seconds to open the layout chooser.
- Pointer will consider Wiimote rotation.
- Better more responsive cursor.
- Enabled "Minimize to tray" option.

**v1.0 beta 8**<br />
- Implemented custom cursors
- New windowed UI
- Added Sleepmode to save battery when Wiimote is not in use.
- Added option to pair a Wiimote at startup.
- Increased CPU utilization, for smoother cursor movement.

**v1.0 beta 7**<br />
- Added ability to connect several Wiimotes.
- Enabled individual keymap settings for each Wiimote.
- Added GameMouse pointer mode through keymap setting.
- Moved settings file into the application folder.
- Fixed 64 bit installer default install folder.
- Fixed support for MultiTouchVista drivers (for Windows 7 or lower)

**v1.0 beta 6**<br />
- Added support for new Wiimotes (RVL-CNT-01-TR)
- Added option to specify Sensor Bar position
- Bugfix, using two touch points would sometimes disable edge gestures

**v1.0 beta 5**<br />
- Multi touch! Use the B button to add a second touch point and zoom or rotate with the A button.
- Added application specific keymaps. Edit or add new keymaps in the Keymaps folder.
- Now using native Windows 8 touch cursor.
- Added helpers to perform edge guestures and taps.

**v1.0 beta 4**<br />
- Much better performance and stability on Windows 8
- Driver is now optional
- Only works on Windows 8, use beta3 for Windows 7/Vista
- Completely disconnects the Wiimote so it doesn't drain battery when not used

**v1.0 beta 3**<br />
- Forgot to enable driver detection
- Added error messaging

**v1.0 beta 2**<br />
- Press minus or plus to zoom in or out
- Press 2 to reset connection to touch driver
- No crash on restart
- Pointer settings saves correctly
- Improved pairing
- Bug fixes

**v1.0 beta 1**<br />
- First release.
