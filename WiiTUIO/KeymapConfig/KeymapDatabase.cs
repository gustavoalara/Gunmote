using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiiTUIO.Properties;
using System.Globalization;

using static WiiTUIO.Resources.Resources;
using System.Threading;
using System.Reflection;

namespace WiiTUIO
{
    class KeymapDatabase
    {
        public string DisableKey { get; private set; }

        private List<KeymapInput> allInputs;
        private List<KeymapOutput> allOutputs;

        private string DEFAULT_JSON_FILENAME = "default.json";
        private string CALIBRATION_JSON_FILENAME = "Calibration.json";
        private string FINISH_CALIBRATION_JSON_FILENAME = "Calibration_Preview.json";

        private static KeymapDatabase currentInstance;
        public static KeymapDatabase Current
        {

            get
            {
                if (currentInstance == null)
                {
                    currentInstance = new KeymapDatabase();
                }
                return currentInstance;
            }
        }

        private KeymapDatabase()
        {

            // Localization
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CurrentUICulture;

            Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture;


            this.DisableKey = "disable";

            allInputs = new List<KeymapInput>();
            allInputs.Add(new KeymapInput(KeymapInputSource.IR, tPointer, "Pointer", true, false, true));
            allInputs.Add(new KeymapInput(KeymapInputSource.IR, tPointerXminus, "PointerX-", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.IR, tPointerXplus, "PointerX+", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.IR, tPointerYminus, "PointerY-", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.IR, tPointerYplus, "PointerY+", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, "A", "A"));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, "B", "B"));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, "Home", "Home"));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tLeft, "Left"));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tRight, "Right"));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tUp, "Up"));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tDown, "Down"));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tPlus, "Plus"));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tMinus, "Minus"));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tOne, "One"));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tTwo, "Two"));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tTiltXMinus, "AccelX-", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tTiltXPlus, "AccelX+", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tTiltYMinus, "AccelY-", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tTiltYPlus, "AccelY+", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tTiltZMinus, "AccelZ-", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tTiltZPlus, "AccelZ+", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tShake, "Shake"));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tExtension, "Extension"));

            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, "C", "Nunchuk.C"));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, "Z", "Nunchuk.Z"));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tStickUp, "Nunchuk.StickUp", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tStickDown, "Nunchuk.StickDown", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tStickLeft, "Nunchuk.StickLeft", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tStickRight, "Nunchuk.StickRight", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tStickRotationPlus, "Nunchuk.Rotation+"));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tStickRotationMinus, "Nunchuk.Rotation-"));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tTiltXMinus, "Nunchuk.AccelX-", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tTiltXPlus, "Nunchuk.AccelX+", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tTiltYMinus, "Nunchuk.AccelY-", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tTiltYPlus, "Nunchuk.AccelY+", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tTiltZMinus, "Nunchuk.AccelZ-", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tTiltZPlus, "Nunchuk.AccelZ+", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tShake, "Nunchuk.Shake"));


            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tLeft, "Classic.Left"));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tRight, "Classic.Right"));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tUp, "Classic.Up"));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tDown, "Classic.Down"));

            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tStickLLeft, "Classic.StickLLeft", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tStickLRight, "Classic.StickLRight", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tStickLUp, "Classic.StickLUp", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tStickLDown, "Classic.StickLDown", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tStickRLeft, "Classic.StickRLeft", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tStickRRight, "Classic.StickRRight", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tStickRUp, "Classic.StickRUp", true, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tStickRDown, "Classic.StickRDown", true, true, false));

            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tMinus, "Classic.Minus"));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tPlus, "Classic.Plus"));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, "Home", "Classic.Home"));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, "Y", "Classic.Y"));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, "X", "Classic.X"));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, "A", "Classic.A"));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, "B", "Classic.B"));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tTriggerL, "Classic.TriggerL", false, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tTriggerR, "Classic.TriggerR", false, true, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tL, "Classic.L"));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tR, "Classic.R"));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, "ZL", "Classic.ZL"));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, "ZR", "Classic.ZR"));


            allInputs.Add(new KeymapInput(KeymapInputSource.IR, tPointer, "OffScreen.Pointer", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.IR, tPointerXminus, "OffScreen.PointerX-", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.IR, tPointerXplus, "OffScreen.PointerX+", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.IR, tPointerYminus, "OffScreen.PointerY-", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.IR, tPointerYplus, "OffScreen.PointerY+", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, "A", "OffScreen.A", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, "B", "OffScreen.B", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, "Home", "OffScreen.Home", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tLeft, "OffScreen.Left", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tRight, "OffScreen.Right", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tUp, "OffScreen.Up", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tDown, "OffScreen.Down", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tPlus, "OffScreen.Plus", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tMinus, "OffScreen.Minus", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tOne, "OffScreen.One", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tTwo, "OffScreen.Two", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tTiltXMinus, "OffScreen.AccelX-", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tTiltXPlus, "OffScreen.AccelX+", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tTiltYMinus, "OffScreen.AccelY-", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tTiltYPlus, "OffScreen.AccelY+", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tTiltZMinus, "OffScreen.AccelZ-", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tTiltZPlus, "OffScreen.AccelZ+", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tShake, "OffScreen.Shake", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.WIIMOTE, tExtension, "OffScreen.Extension", false));

            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, "C", "OffScreen.Nunchuk.C", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, "Z", "OffScreen.Nunchuk.Z", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tStickUp, "OffScreen.Nunchuk.StickUp", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tStickDown, "OffScreen.Nunchuk.StickDown", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tStickLeft, "OffScreen.Nunchuk.StickLeft", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tStickRight, "OffScreen.Nunchuk.StickRight", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tStickRotationPlus, "OffScreen.Nunchuk.Rotation+", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tStickRotationMinus, "OffScreen.Nunchuk.Rotation-", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tTiltXMinus, "OffScreen.Nunchuk.AccelX-", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tTiltXPlus, "OffScreen.Nunchuk.AccelX+", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tTiltYMinus, "OffScreen.Nunchuk.AccelY-", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tTiltYPlus, "OffScreen.Nunchuk.AccelY+", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tTiltZMinus, "OffScreen.Nunchuk.AccelZ-", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tTiltZPlus, "OffScreen.Nunchuk.AccelZ+", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.NUNCHUK, tShake, "OffScreen.Nunchuk.Shake", false));

            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tLeft, "OffScreen.Classic.Left", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tRight, "OffScreen.Classic.Right", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tUp, "OffScreen.Classic.Up", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tDown, "OffScreen.Classic.Down", false));

            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tStickLLeft, "OffScreen.Classic.StickLLeft", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tStickLRight, "OffScreen.Classic.StickLRight", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tStickLUp, "OffScreen.Classic.StickLUp", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tStickLDown, "OffScreen.Classic.StickLDown", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tStickRLeft, "OffScreen.Classic.StickRLeft", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tStickRRight, "OffScreen.Classic.StickRRight", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tStickRUp, "OffScreen.Classic.StickRUp", true, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tStickRDown, "OffScreen.Classic.StickRDown", true, true, false, false));

            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tMinus, "OffScreen.Classic.Minus", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tPlus, "OffScreen.Classic.Plus", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, "Home", "OffScreen.Classic.Home", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, "Y", "OffScreen.Classic.Y", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, "X", "OffScreen.Classic.X", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, "A", "OffScreen.Classic.A", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, "B", "OffScreen.Classic.B", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tTriggerL, "OffScreen.Classic.TriggerL", false, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tTriggerR, "OffScreen.Classic.TriggerR", false, true, false, false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tL, "OffScreen.Classic.L", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, tR, "OffScreen.Classic.R", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, "ZL", "OffScreen.Classic.ZL", false));
            allInputs.Add(new KeymapInput(KeymapInputSource.CLASSIC, "ZR", "OffScreen.Classic.ZR", false));

            allOutputs = new List<KeymapOutput>();
            //allOutputs.Add(new KeymapOutput(KeymapOutputType.MOUSE, tMouse, "mouse", false, false, true, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.MOUSE, tFPSMouse, "fpsmouse", false, false, true, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.MOUSE, tLightgunMouse, "lightgunmouse", false, false, true, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.MOUSE, tLightgunMouse43, "lightgunmouse-4:3", false, false, true, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.MOUSE, tLightgunMouse169, "lightgunmouse-16:9", false, false, true, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.MOUSE, tMouseLeftB, "mouseleft"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.MOUSE, tMouseMiddleB, "mousemiddle"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.MOUSE, tMouseRightB, "mouseright"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.MOUSE, tMouseWheelUp, "mousewheelup"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.MOUSE, tMouseWheelDown, "mousewheeldown"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.MOUSE, tMouseRight, "mousex+", true, true, false, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.MOUSE, tMouseUp, "mousey+", true, true, false, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.MOUSE, tMouseLeft, "mousex-", true, true, false, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.MOUSE, tMouseDown, "mousey-", true, true, false, false));
            //allOutputs.Add(new KeymapOutput(KeymapOutputType.MOUSE, "Mouse Middle", "mbutton"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.MOUSE, tMouseExtra1, "mousexbutton1"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.MOUSE, tMouseExtra2, "mousexbutton2"));


            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Tab", "tab"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tBack, "back"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tReturn, "return"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tShift, "shift"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Control", "control"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Alt", "menu"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tPause, "pause"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tCapsLock, "capital"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Escape", "escape"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tSpace, "space"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tPagUp, "prior"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tPagDown, "next"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tEnd, "end"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tHome, "home"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tNumLeft, "left"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tNumUp, "up"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tNumRight, "right"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tNumDown, "down"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tPrintScreen, "snapshot"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tInsert, "insert"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tDel, "delete"));


            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "0", "vk_0"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "1", "vk_1"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "2", "vk_2"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "3", "vk_3"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "4", "vk_4"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "5", "vk_5"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "6", "vk_6"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "7", "vk_7"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "8", "vk_8"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "9", "vk_9"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "A", "vk_a"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "B", "vk_b"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "C", "vk_c"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "D", "vk_d"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "E", "vk_e"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "F", "vk_f"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "G", "vk_g"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "H", "vk_h"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "I", "vk_i"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "J", "vk_j"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "K", "vk_k"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "L", "vk_l"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "M", "vk_m"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "N", "vk_n"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "O", "vk_o"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "P", "vk_p"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Q", "vk_q"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "R", "vk_r"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "S", "vk_s"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "T", "vk_t"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "U", "vk_u"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "V", "vk_v"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "W", "vk_w"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "X", "vk_x"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Y", "vk_y"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Z", "vk_z"));

            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tPeriod, "oem_period"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tComma, "oem_comma"));

            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tLWin, "lwin"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tRWin, "rwin"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Apps / Menu", "apps"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Numpad 0", "numpad0"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Numpad 1", "numpad1"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Numpad 2", "numpad2"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Numpad 3", "numpad3"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Numpad 4", "numpad4"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Numpad 5", "numpad5"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Numpad 6", "numpad6"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Numpad 7", "numpad7"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Numpad 8", "numpad8"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Numpad 9", "numpad9"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tMultiply, "multiply"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tAdd, "add"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tSubtract, "subtract"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tDecimal, "decimal"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tDivide, "divide"));


            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "F1", "f1"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "F2", "f2"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "F3", "f3"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "F4", "f4"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "F5", "f5"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "F6", "f6"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "F7", "f7"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "F8", "f8"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "F9", "f9"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "F10", "f10"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "F11", "f11"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "F12", "f12"));


            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tNumlock, "numlock"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tLShift, "lshift"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tRShift, "rshift"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tLControl, "lcontrol"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tRControl, "rcontrol"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tLAlt, "lmenu"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tRAlt, "rmenu"));

            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tBrowserBack, "browser_back"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tBrowserForward, "browser_forward"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tBrowserRefresh, "browser_refresh"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tBrowserStop, "browser_stop"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tBrowserSearch, "browser_search"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tBrowserFavorites, "browser_favorites"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tBrowserHome, "browser_home"));

            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tVolumeMute, "volume_mute"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tVolumeUp, "volume_up"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tVolumeDown, "volume_down"));

            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tNextTrack, "media_next_track"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tPrevTrack, "media_prev_track"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tStop, "media_stop"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, tPlayPause, "media_play_pause"));

            allOutputs.Add(new KeymapOutput(KeymapOutputType.KEYBOARD, "Zoom", "zoom"));

            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, "A", "360.a"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, "B", "360.b"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, "X", "360.x"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, "Y", "360.y"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tLeft, "360.left", true, false, false, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tRight, "360.right", true, false, false, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tUp, "360.up", true, false, false, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tDown, "360.down", true, false, false, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tBack, "360.back"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tStart, "360.start"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tGuide, "360.guide"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tStickLPress, "360.stickpressl"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tStickRPress, "360.stickpressr"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tStickLeft, "360.stickl", false, false, true, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tStickLLight, "360.stickl-light", false, false, true, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tStickLLight43, "360.stickl-light-4:3", false, false, true, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tStickLUp, "360.sticklup", true, true, false, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tStickLDown, "360.stickldown", true, true, false, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tStickLLeft, "360.sticklleft", true, true, false, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tStickLRight, "360.sticklright", true, true, false, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tStickLCenter, "360.sticklcenter"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tStickRight, "360.stickr", false, false, true, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tStickRLight, "360.stickr-light", false, false, true, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tStickRLight43, "360.stickr-light-4:3", false, false, true, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tStickRUp, "360.stickrup", true, true, false, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tStickRDown, "360.stickrdown", true, true, false, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tStickRLeft, "360.stickrleft", true, true, false, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tStickRRight, "360.stickrright", true, true, false, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tStickRCenter, "360.stickrcenter"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tTriggerL, "360.triggerl", true, true, false, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tTriggerR, "360.triggerr", true, true, false, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tBumperL, "360.bumperl"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.XINPUT, tBumperR, "360.bumperr"));

            //allOutputs.Add(new KeymapOutput(KeymapOutputType.CURSOR, "Cursor", "cursor", false, false, true, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.CURSOR, "Cursor", "lightguncursor", false, false, true, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.CURSOR, "Cursor 4:3", "lightguncursor-4:3", false, false, true, false));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.CURSOR, "Cursor 16:9", "lightguncursor-16:9", false, false, true, false));
            //allOutputs.Add(new KeymapOutput(KeymapOutputType.CURSOR, tPressCursor, "cursorpress"));

            allOutputs.Add(new KeymapOutput(KeymapOutputType.WIIMOTE, tShortRumble, "rumbleshort"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.WIIMOTE, tLongRumble, "rumblelong"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.WIIMOTE, tRumbleHold, "rumblehold"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.WIIMOTE, tRumbleAlt, "rumblealt"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.WIIMOTE, "LED 1", "led1"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.WIIMOTE, "LED 2", "led2"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.WIIMOTE, "LED 3", "led3"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.WIIMOTE, "LED 4", "led4"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.WIIMOTE, tSound1, "sound1"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.WIIMOTE, tSound2, "sound2"));
            allOutputs.Add(new KeymapOutput(KeymapOutputType.WIIMOTE, tSoundLoop, "loop"));

            allOutputs.Add(new KeymapOutput(KeymapOutputType.DISABLE, tDisableKey, this.DisableKey));
        }


        public KeymapSettings getKeymapSettings()
        {
            return new KeymapSettings(Settings.Default.keymaps_config);
        }

        public List<Keymap> getAllKeymaps()
        {
            List<Keymap> list = new List<Keymap>();
            string[] files = Directory.GetFiles(Settings.Default.keymaps_path, "*.json");
            string defaultKeymapFilename = this.getKeymapSettings().getDefaultKeymap();

            Keymap defaultKeymap = new Keymap(null, defaultKeymapFilename);
            list.Add(defaultKeymap);

            foreach (string filepath in files)
            {
                string filename = Path.GetFileName(filepath);
                if (filename != Settings.Default.keymaps_config && filename != defaultKeymapFilename)
                {
                    list.Add(new Keymap(defaultKeymap, filename));
                }
            }
            return list;
        }

        public Keymap getKeymap(string filename)
        {
            List<Keymap> list = this.getAllKeymaps();

            foreach (Keymap keymap in list)
            {
                if (keymap.Filename == filename)
                {
                    return keymap;
                }
            }
            return null;
        }

        public Keymap getDefaultKeymap()
        {
            List<Keymap> list = this.getAllKeymaps();
            KeymapSettings settings = this.getKeymapSettings();

            foreach (Keymap keymap in list)
            {
                if (keymap.Filename == settings.getDefaultKeymap())
                {
                    return keymap;
                }
            }
            return null;
        }

        public Keymap getCalibrationKeymap()
        {
            List<Keymap> list = this.getAllKeymaps();
            KeymapSettings settings = this.getKeymapSettings();

            foreach (Keymap keymap in list)
            {
                if (keymap.Filename == settings.getCalibrationKeymap())
                {
                    return keymap;
                }
            }
            return null;
        }
		public Keymap getFinishCalibrationKeymap()
        {
            List<Keymap> list = this.getAllKeymaps();
            KeymapSettings settings = this.getKeymapSettings();

            foreach (Keymap keymap in list)
            {
                if (keymap.Filename == settings.getFinishCalibrationKeymap())
                {
                    return keymap;
                }
            }
            return null;
        }

        public List<KeymapInput> getAvailableInputs()
        {
            return allInputs;
        }

        public List<KeymapInput> getAvailableInputs(KeymapInputSource source, bool onscreen)
        {
            List<KeymapInput> list = new List<KeymapInput>();
            foreach (KeymapInput input in allInputs)
            {
                if ((input.Source == source) && ((onscreen && input.OnScreen) || (!onscreen && !input.OnScreen)))
                {
                    list.Add(input);
                }
            }
            return list;
        }

        public List<KeymapOutput> getAvailableOutputs()
        {
            return allOutputs;
        }

        public List<KeymapOutput> getAvailableOutputs(KeymapOutputType type)
        {
            if (type == KeymapOutputType.ALL)
            {
                return allOutputs;
            }
            List<KeymapOutput> list = new List<KeymapOutput>();
            foreach (KeymapOutput output in allOutputs)
            {
                if (output.Type == type)
                {
                    list.Add(output);
                }
            }
            return list;
        }

        public KeymapInput getInput(string key)
        {
            List<KeymapInput> list = this.allInputs;
            foreach (KeymapInput input in list)
            {
                if (input.Key.ToLower() == key.ToLower())
                {
                    return input;
                }
            }
            return null;
        }

        public KeymapOutput getOutput(string key)
        {
            List<KeymapOutput> list = this.allOutputs;
            foreach (KeymapOutput output in list)
            {
                if (output.Key.ToLower() == key.ToLower())
                {
                    return output;
                }
            }
            return null;
        }

        public KeymapOutput getDisableOutput()
        {
            return this.getAvailableOutputs(KeymapOutputType.DISABLE).First();
        }

        public bool deleteKeymap(Keymap keymap)
        {
            if (keymap.Filename == this.getKeymapSettings().getDefaultKeymap())
            {
                return false;
            }
            this.getKeymapSettings().removeFromLayoutChooser(keymap);
            this.getKeymapSettings().removeFromApplicationSearch(keymap);
            File.Delete(Settings.Default.keymaps_path + keymap.Filename);
            return true;
        }

        public Keymap createNewKeymap()
        {
            List<Keymap> list = new List<Keymap>();
            string[] files = Directory.GetFiles(Settings.Default.keymaps_path, "*.json");

            string suggestedFilename = "z_custom.json";

            bool recheck = false;

            int iterations = 0;

            do
            {
                recheck = false;
                foreach (string filepath in files)
                {
                    string filename = Path.GetFileName(filepath);
                    if (suggestedFilename == filename)
                    {
                        suggestedFilename = "z_custom_" + (++iterations) + ".json";
                        recheck = true;
                    }
                }
            } while (recheck);

            return new Keymap(this.getDefaultKeymap(), suggestedFilename);
        }



        public void CreateDefaultFiles()
        {
            this.createDefaultApplicationsJSON();
            this.createDefaultKeymapJSON();
            this.createCalibrationKeymapJSON();
            this.createFinishCalibrationKeymapJSON();
        }

        private static void MergeJSON(JObject receiver, JObject donor)
        {
            foreach (var property in donor)
            {
                JObject receiverValue = receiver[property.Key] as JObject;
                JObject donorValue = property.Value as JObject;
                if (receiverValue != null && donorValue != null)
                    MergeJSON(receiverValue, donorValue);
                else
                    receiver[property.Key] = property.Value;
            }
        }

        private JObject createDefaultApplicationsJSON()
        {
            JArray layouts = new JArray();
            layouts.Add(new JObject(
                new JProperty("Name", "Default"),
                new JProperty("Keymap", DEFAULT_JSON_FILENAME)
            ));

            JArray applications = new JArray();

            JObject applicationList =
                new JObject(
                    new JProperty("LayoutChooser", layouts),
                    new JProperty("Applications", applications),
                    new JProperty("Default", DEFAULT_JSON_FILENAME),
                    new JProperty("Calibration", CALIBRATION_JSON_FILENAME),
                    new JProperty("Calibration_Preview", FINISH_CALIBRATION_JSON_FILENAME)
                );

            JObject union = applicationList;

            if (File.Exists(Settings.Default.keymaps_path + Settings.Default.keymaps_config))
            {
                StreamReader reader = File.OpenText(Settings.Default.keymaps_path + Settings.Default.keymaps_config);
                try
                {
                    JObject existingConfig = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                    reader.Close();

                    MergeJSON(union, existingConfig);
                }
                catch (Exception e)
                {
                    throw new Exception(Settings.Default.keymaps_path + Settings.Default.keymaps_config + " is not valid JSON");
                }
            }

            File.WriteAllText(Settings.Default.keymaps_path + Settings.Default.keymaps_config, union.ToString());
            return union;
        }

        private JObject createDefaultKeymapJSON()
        {
            JObject buttons = new JObject();

            buttons.Add(new JProperty("Pointer", "360.stickl"));

            buttons.Add(new JProperty("A", "360.a"));

            buttons.Add(new JProperty("B", "360.b"));

            buttons.Add(new JProperty("Home", "360.Guide"));

            buttons.Add(new JProperty("Left", "360.Left"));
            buttons.Add(new JProperty("Right", "360.Right"));
            buttons.Add(new JProperty("Up", "360.Up"));
            buttons.Add(new JProperty("Down", "360.Down"));

            buttons.Add(new JProperty("Plus", "360.Start"));

            buttons.Add(new JProperty("Minus", "360.Back"));

            buttons.Add(new JProperty("One", "360.X"));

            buttons.Add(new JProperty("Two", "360.Y"));

            buttons.Add(new JProperty("AccelX+", "disable"));
            buttons.Add(new JProperty("AccelX-", "disable"));
            buttons.Add(new JProperty("AccelY+", "disable"));
            buttons.Add(new JProperty("AccelY-", "disable"));
            buttons.Add(new JProperty("AccelZ+", "disable"));
            buttons.Add(new JProperty("AccelZ-", "disable"));

            buttons.Add(new JProperty("Nunchuk.StickUp", "360.StickRUp"));
            buttons.Add(new JProperty("Nunchuk.StickDown", "360.StickRDown"));
            buttons.Add(new JProperty("Nunchuk.StickLeft", "360.StickRLeft"));
            buttons.Add(new JProperty("Nunchuk.StickRight", "360.StickRRight"));
            buttons.Add(new JProperty("Nunchuk.C", "360.TriggerL"));
            buttons.Add(new JProperty("Nunchuk.Z", "360.TriggerR"));

            buttons.Add(new JProperty("Classic.Left", "360.Left"));
            buttons.Add(new JProperty("Classic.Right", "360.Right"));
            buttons.Add(new JProperty("Classic.Up", "360.Up"));
            buttons.Add(new JProperty("Classic.Down", "360.Down"));
            buttons.Add(new JProperty("Classic.StickLUp", "360.StickLUp"));
            buttons.Add(new JProperty("Classic.StickLDown", "360.StickLDown"));
            buttons.Add(new JProperty("Classic.StickLLeft", "360.StickLLeft"));
            buttons.Add(new JProperty("Classic.StickLRight", "360.StickLRight"));
            buttons.Add(new JProperty("Classic.StickRUp", "360.StickRUp"));
            buttons.Add(new JProperty("Classic.StickRDown", "360.StickRDown"));
            buttons.Add(new JProperty("Classic.StickRLeft", "360.StickRLeft"));
            buttons.Add(new JProperty("Classic.StickRRight", "360.StickRRight"));
            buttons.Add(new JProperty("Classic.Minus", "360.Back"));
            buttons.Add(new JProperty("Classic.Plus", "360.Start"));
            buttons.Add(new JProperty("Classic.Home", "360.Guide"));
            buttons.Add(new JProperty("Classic.Y", "360.Y"));
            buttons.Add(new JProperty("Classic.X", "360.X"));
            buttons.Add(new JProperty("Classic.A", "360.A"));
            buttons.Add(new JProperty("Classic.B", "360.B"));
            buttons.Add(new JProperty("Classic.TriggerL", "360.TriggerL"));
            buttons.Add(new JProperty("Classic.TriggerR", "360.TriggerR"));
            buttons.Add(new JProperty("Classic.L", "360.L"));
            buttons.Add(new JProperty("Classic.R", "360.R"));
            buttons.Add(new JProperty("Classic.ZL", "360.BumperL"));
            buttons.Add(new JProperty("Classic.ZR", "360.BumperR"));

            JObject screen = new JObject();

            screen.Add(new JProperty("OnScreen", buttons));

            JObject union = new JObject();

            union.Add(new JProperty("Title", "Default"));

            union.Add(new JProperty("All", screen));

            if (File.Exists(Settings.Default.keymaps_path + Settings.Default.keymaps_config))
            {
                StreamReader reader = File.OpenText(Settings.Default.keymaps_path + DEFAULT_JSON_FILENAME);
                try
                {
                    JObject existingConfig = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                    reader.Close();

                    MergeJSON(union, existingConfig);
                }
                catch (Exception e)
                {
                    throw new Exception(Settings.Default.keymaps_path + DEFAULT_JSON_FILENAME + " is not valid JSON");
                }
            }
            File.WriteAllText(Settings.Default.keymaps_path + DEFAULT_JSON_FILENAME, union.ToString());
            return union;
        }

        private JObject createCalibrationKeymapJSON()
        {
            JObject buttons = new JObject();

            buttons.Add(new JProperty("Pointer", "disable"));

            buttons.Add(new JProperty("A", "disable"));

            buttons.Add(new JProperty("B", "disable"));

            buttons.Add(new JProperty("Home", "disable"));

            buttons.Add(new JProperty("Left", "disable"));
            buttons.Add(new JProperty("Right", "disable"));
            buttons.Add(new JProperty("Up", "disable"));
            buttons.Add(new JProperty("Down", "disable"));

            buttons.Add(new JProperty("Plus", "disable"));

            buttons.Add(new JProperty("Minus", "disable"));

            buttons.Add(new JProperty("One", "disable"));

            buttons.Add(new JProperty("Two", "disable"));

            buttons.Add(new JProperty("AccelX+", "disable"));
            buttons.Add(new JProperty("AccelX-", "disable"));
            buttons.Add(new JProperty("AccelY+", "disable"));
            buttons.Add(new JProperty("AccelY-", "disable"));
            buttons.Add(new JProperty("AccelZ+", "disable"));
            buttons.Add(new JProperty("AccelZ-", "disable"));

            buttons.Add(new JProperty("Nunchuk.StickUp", "disable"));
            buttons.Add(new JProperty("Nunchuk.StickDown", "disable"));
            buttons.Add(new JProperty("Nunchuk.StickLeft", "disable"));
            buttons.Add(new JProperty("Nunchuk.StickRight", "disable"));
            buttons.Add(new JProperty("Nunchuk.C", "disable"));
            buttons.Add(new JProperty("Nunchuk.Z", "disable"));

            buttons.Add(new JProperty("Classic.Left", "disable"));
            buttons.Add(new JProperty("Classic.Right", "disable"));
            buttons.Add(new JProperty("Classic.Up", "disable"));
            buttons.Add(new JProperty("Classic.Down", "disable"));
            buttons.Add(new JProperty("Classic.StickLUp", "disable"));
            buttons.Add(new JProperty("Classic.StickLDown", "disable"));
            buttons.Add(new JProperty("Classic.StickLLeft", "disable"));
            buttons.Add(new JProperty("Classic.StickLRight", "disable"));
            buttons.Add(new JProperty("Classic.StickRUp", "disable"));
            buttons.Add(new JProperty("Classic.StickRDown", "disable"));
            buttons.Add(new JProperty("Classic.StickRLeft", "disable"));
            buttons.Add(new JProperty("Classic.StickRRight", "disable"));
            buttons.Add(new JProperty("Classic.Minus", "disable"));
            buttons.Add(new JProperty("Classic.Plus", "disable"));
            buttons.Add(new JProperty("Classic.Home", "disable"));
            buttons.Add(new JProperty("Classic.Y", "disable"));
            buttons.Add(new JProperty("Classic.X", "disable"));
            buttons.Add(new JProperty("Classic.A", "disable"));
            buttons.Add(new JProperty("Classic.B", "disable"));
            buttons.Add(new JProperty("Classic.TriggerL", "disable"));
            buttons.Add(new JProperty("Classic.TriggerR", "disable"));
            buttons.Add(new JProperty("Classic.L", "disable"));
            buttons.Add(new JProperty("Classic.R", "disable"));
            buttons.Add(new JProperty("Classic.ZL", "disable"));
            buttons.Add(new JProperty("Classic.ZR", "disable"));

            JObject screen = new JObject();

            screen.Add(new JProperty("OnScreen", buttons));

            JObject union = new JObject();

            union.Add(new JProperty("Title", "Calibration"));

            union.Add(new JProperty("All", screen));

            File.WriteAllText(Settings.Default.keymaps_path + CALIBRATION_JSON_FILENAME, union.ToString()); //Prevent user from editing this
            return union;
        }
    private JObject createFinishCalibrationKeymapJSON()
        {
            JObject buttons = new JObject();

            buttons.Add(new JProperty("Pointer", "lightguncursor"));

            buttons.Add(new JProperty("A", "disable"));

            buttons.Add(new JProperty("B", "disable"));

            buttons.Add(new JProperty("Home", "disable"));

            buttons.Add(new JProperty("Left", "disable"));
            buttons.Add(new JProperty("Right", "disable"));
            buttons.Add(new JProperty("Up", "disable"));
            buttons.Add(new JProperty("Down", "disable"));

            buttons.Add(new JProperty("Plus", "disable"));

            buttons.Add(new JProperty("Minus", "disable"));

            buttons.Add(new JProperty("One", "disable"));

            buttons.Add(new JProperty("Two", "disable"));

            buttons.Add(new JProperty("AccelX+", "disable"));
            buttons.Add(new JProperty("AccelX-", "disable"));
            buttons.Add(new JProperty("AccelY+", "disable"));
            buttons.Add(new JProperty("AccelY-", "disable"));
            buttons.Add(new JProperty("AccelZ+", "disable"));
            buttons.Add(new JProperty("AccelZ-", "disable"));

            buttons.Add(new JProperty("Nunchuk.StickUp", "disable"));
            buttons.Add(new JProperty("Nunchuk.StickDown", "disable"));
            buttons.Add(new JProperty("Nunchuk.StickLeft", "disable"));
            buttons.Add(new JProperty("Nunchuk.StickRight", "disable"));
            buttons.Add(new JProperty("Nunchuk.C", "disable"));
            buttons.Add(new JProperty("Nunchuk.Z", "disable"));

            buttons.Add(new JProperty("Classic.Left", "disable"));
            buttons.Add(new JProperty("Classic.Right", "disable"));
            buttons.Add(new JProperty("Classic.Up", "disable"));
            buttons.Add(new JProperty("Classic.Down", "disable"));
            buttons.Add(new JProperty("Classic.StickLUp", "disable"));
            buttons.Add(new JProperty("Classic.StickLDown", "disable"));
            buttons.Add(new JProperty("Classic.StickLLeft", "disable"));
            buttons.Add(new JProperty("Classic.StickLRight", "disable"));
            buttons.Add(new JProperty("Classic.StickRUp", "disable"));
            buttons.Add(new JProperty("Classic.StickRDown", "disable"));
            buttons.Add(new JProperty("Classic.StickRLeft", "disable"));
            buttons.Add(new JProperty("Classic.StickRRight", "disable"));
            buttons.Add(new JProperty("Classic.Minus", "disable"));
            buttons.Add(new JProperty("Classic.Plus", "disable"));
            buttons.Add(new JProperty("Classic.Home", "disable"));
            buttons.Add(new JProperty("Classic.Y", "disable"));
            buttons.Add(new JProperty("Classic.X", "disable"));
            buttons.Add(new JProperty("Classic.A", "disable"));
            buttons.Add(new JProperty("Classic.B", "disable"));
            buttons.Add(new JProperty("Classic.TriggerL", "disable"));
            buttons.Add(new JProperty("Classic.TriggerR", "disable"));
            buttons.Add(new JProperty("Classic.L", "disable"));
            buttons.Add(new JProperty("Classic.R", "disable"));
            buttons.Add(new JProperty("Classic.ZL", "disable"));
            buttons.Add(new JProperty("Classic.ZR", "disable"));

            JObject screen = new JObject();

            screen.Add(new JProperty("OnScreen", buttons));

            JObject union = new JObject();

            union.Add(new JProperty("Title", "Calibration_Preview"));

            union.Add(new JProperty("All", screen));

            File.WriteAllText(Settings.Default.keymaps_path + FINISH_CALIBRATION_JSON_FILENAME, union.ToString()); //Prevent user from editing this
            return union;
        }
    }

    public enum KeymapInputSource
    {
        IR,
        WIIMOTE,
        NUNCHUK,
        CLASSIC
    }

    public class KeymapInput
    {
        public string Name { get; private set; }
        public string Key { get; private set; }
        public KeymapInputSource Source { get; private set; }
        public bool Button { get; private set; }
        public bool Continous { get; private set; }
        public bool Cursor { get; private set; }
        public bool OnScreen { get; private set; }

        public KeymapInput(KeymapInputSource source, string name, string key, bool onScreen = true)
            : this(source, name, key, true, false, false, onScreen)
        {

        }

        public KeymapInput(KeymapInputSource source, string name, string key, bool button, bool continous, bool cursor, bool onScreen = true)
        {
            this.Source = source;
            this.Name = name;
            this.Key = key;
            this.Button = button;
            this.Continous = continous;
            this.Cursor = cursor;
            this.OnScreen = onScreen;
        }

        public bool canHandle(KeymapOutput output)
        {
            return ((this.Button == output.Button || this.Continous == output.Continous) && (this.Button == output.Button || this.Cursor == output.Cursor)) || output.Type == KeymapOutputType.DISABLE;
        }
    }


    public enum KeymapOutputType
    {
        ALL, //Only used in search
        MOUSE,
        XINPUT,
        KEYBOARD,
        WIIMOTE,
        CURSOR,
        DISABLE
    }
    public class KeymapOutput
    {
        public string Name { get; private set; }
        public string Key { get; private set; }
        public KeymapOutputType Type { get; private set; }
        public bool Button { get; private set; }
        public bool Continous { get; private set; }
        public bool Cursor { get; private set; }
        public bool Stackable { get; private set; }

        public KeymapOutput(KeymapOutputType type, string name, string key)
            : this(type, name, key, true, false, false, true)
        {

        }

        public KeymapOutput(KeymapOutputType type, string name, string key, bool button, bool continous, bool cursor, bool stackable)
        {
            this.Type = type;
            this.Name = name;
            this.Key = key;
            this.Button = button;
            this.Continous = continous;
            this.Cursor = cursor;
        }

        public bool canStack(KeymapOutput other)
        {
            return this.Stackable && other.Stackable;
        }
    }


    public class KeymapOutputComparer : IComparer<KeymapOutput>
    {
        StringComparer comparer = StringComparer.CurrentCulture;

        public int Compare(KeymapOutput x, KeymapOutput y)
        {
            if (x.Type - y.Type == 0)
            {
                return comparer.Compare(x.Name, y.Name);
            }
            return x.Type - y.Type;
        }
    }
}
