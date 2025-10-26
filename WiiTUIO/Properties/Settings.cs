using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PointF = WiimoteLib.PointF;

namespace WiiTUIO.Properties
{
    class Settings
    {

        public event PropertyChangedEventHandler PropertyChanged;

        private bool _pairOnStart = false;
        public bool pairOnStart
        {
            get { return _pairOnStart; }
            set
            {
                _pairOnStart = value;
                OnPropertyChanged("pairOnStart");
            }
        }

        private bool _connectOnStart = true;
        public bool connectOnStart
        {
            get { return _connectOnStart; }
            set
            {
                _connectOnStart = value;
                OnPropertyChanged("connectOnStart");
            }
        }

        private bool _minimizeOnStart = false;
        public bool minimizeOnStart
        {
            get { return _minimizeOnStart; }
            set
            {
                _minimizeOnStart = value;
                OnPropertyChanged("minimizeOnStart");
            }
        }

        private bool _minimizeToTray = false;
        public bool minimizeToTray
        {
            get { return _minimizeToTray; }
            set
            {
                _minimizeToTray = value;
                OnPropertyChanged("minimizeToTray");
            }
        }

        private bool _notifications_enabled = true;
        public bool notifications_enabled
        {
            get { return _notifications_enabled; }
            set
            {
                _notifications_enabled = value;
                OnPropertyChanged("notifications_enabled");
            }
        }

        private bool _pairedOnce = false;
        public bool pairedOnce
        {
            get { return _pairedOnce; }
            set
            {
                _pairedOnce = value;
                OnPropertyChanged("pairedOnce");
            }
        }

        private string _primaryMonitor = "";
        public string primaryMonitor
        {
            get { return _primaryMonitor; }
            set
            {
                _primaryMonitor = value;
                OnPropertyChanged("primaryMonitor");
            }
        }

        private bool _completelyDisconnect = false;
        public bool completelyDisconnect
        {
            get { return _completelyDisconnect; }
            set
            {
                _completelyDisconnect = value;
                OnPropertyChanged("completelyDisconnect");
            }
        }

        private int _autoDisconnectTimeout = 300000;
        public int autoDisconnectTimeout
        {
            get { return _autoDisconnectTimeout; }
            set
            {
                _autoDisconnectTimeout = value;
                OnPropertyChanged("autoDisconnectTimeout");
            }
        }

        private double _defaultContinousScale = 1.0;
        public double defaultContinousScale
        {
            get { return _defaultContinousScale; }
            set
            {
                _defaultContinousScale = value;
                OnPropertyChanged("defaultContinousScale");
            }
        }

        private double _defaultContinousPressThreshold = 0.4;
        public double defaultContinousPressThreshold
        {
            get { return _defaultContinousPressThreshold; }
            set
            {
                _defaultContinousPressThreshold = value;
                OnPropertyChanged("defaultContinousPressThreshold");
            }
        }

        private double _defaultContinousDeadzone = 0.01;
        public double defaultContinousDeadzone
        {
            get { return _defaultContinousDeadzone; }
            set
            {
                _defaultContinousDeadzone = value;
                OnPropertyChanged("defaultContinousDeadzone");
            }
        }

        private bool _disconnectWiimotesOnDolphin = false;
        public bool disconnectWiimotesOnDolphin
        {
            get { return _disconnectWiimotesOnDolphin; }
            set
            {
                _disconnectWiimotesOnDolphin = value;
                OnPropertyChanged("disconnectWiimotesOnDolphin");
            }
        }

        private string _dolphin_path = "";
        public string dolphin_path
        {
            get { return _dolphin_path; }
            set
            {
                _dolphin_path = value;
                OnPropertyChanged("dolphin_path");
            }
        }

        private string _keymaps_path = @"Keymaps\";
        public string keymaps_path
        {
            get { return _keymaps_path; }
            set
            {
                _keymaps_path = value;
                OnPropertyChanged("keymaps_path");
            }
        }

        private bool _noTopmost = false;
        public bool noTopmost
        {
            get { return _noTopmost; }
            set
            {
                _noTopmost = value;
                OnPropertyChanged("noTopmost");
            }
        }

        private string _keymaps_config = @"Keymaps.json";
        public string keymaps_config
        {
            get { return _keymaps_config; }
            set
            {
                _keymaps_config = value;
                OnPropertyChanged("keymaps_config");
            }
        }

        private double _pointer_sensorBarPosCompensation = 0.30;
        public double pointer_sensorBarPosCompensation
        {
            get { return _pointer_sensorBarPosCompensation; }
            set
            {
                _pointer_sensorBarPosCompensation = value;
                OnPropertyChanged("pointer_sensorBarPosCompensation");
            }
        }

        private double _pointer_cursorSize = 0.03;
        public double pointer_cursorSize
        {
            get { return _pointer_cursorSize; }
            set
            {
                _pointer_cursorSize = value;
                OnPropertyChanged("pointer_cursorSize");
            }
        }

        private double _pointer_marginsTopBottom = 0.5;
        public double pointer_marginsTopBottom
        {
            get { return _pointer_marginsTopBottom; }
            set
            {
                _pointer_marginsTopBottom = value;
                OnPropertyChanged("pointer_marginsTopBottom");
            }
        }

        private double _pointer_marginsLeftRight = 0.4;
        public double pointer_marginsLeftRight
        {
            get { return _pointer_marginsLeftRight; }
            set
            {
                _pointer_marginsLeftRight = value;
                OnPropertyChanged("pointer_marginsLeftRight");
            }
        }

        private string _pointer_sensorBarPos = "center";
        public string pointer_sensorBarPos
        {
            get { return _pointer_sensorBarPos; }
            set
            {
                _pointer_sensorBarPos = value;
                OnPropertyChanged("pointer_sensorBarPos");
            }
        }

        private string _pointer_4IRMode = "none";
        public string pointer_4IRMode
        {
            get { return _pointer_4IRMode; }
            set
            {
                _pointer_4IRMode = value;
                OnPropertyChanged("pointer_4IRMode");
            }
        }

        private int _pointer_FPS = 100;
        public int pointer_FPS
        {
            get { return _pointer_FPS; }
            set
            {
                _pointer_FPS = value;
                OnPropertyChanged("pointer_FPS");
            }
        }

        // Should delta acceleration be enabled.
        private bool _test_deltaAccel = true;
        public bool test_deltaAccel
        {
            get { return _test_deltaAccel; }
            set
            {
                _test_deltaAccel = value;
                OnPropertyChanged("test_deltaAccel");
            }
        }

        // Maximum multiplier to use for delta acceleration.
        private double _test_deltaAccelMulti = 4.0;
        public double test_deltaAccelMulti
        {
            get { return _test_deltaAccelMulti; }
            set
            {
                if (value >= 1.0 && value <= 20.0)
                {
                    _test_deltaAccelMulti = value;
                    OnPropertyChanged("test_deltaAccelMulti");
                }
            }
        }

        // Minimum delta travel before delta acceleration is applied.
        private double _test_deltaAccelMinTravel = 0.02;
        public double test_deltaAccelMinTravel
        {
            get { return _test_deltaAccelMinTravel; }
            set
            {
                if (value >= 0.01 && value <= 0.5)
                {
                    _test_deltaAccelMinTravel = value;
                    OnPropertyChanged("test_deltaAccelMinTravel");
                }
            }
        }

        // Maximum duration that delta acceleration should be applied.
        // Value is in seconds.
        private double _test_deltaAccelEasingDuration = 0.2;
        public double test_deltaAccelEasingDuration
        {
            get { return _test_deltaAccelEasingDuration; }
            set
            {
                if (value >= 0.00 && value <= 10.0)
                {
                    _test_deltaAccelEasingDuration = value;
                    OnPropertyChanged("test_deltaAccelEasingDuration");
                }
            }

        }

        // Maximum duration that easing is applied to upper acceleration
        // region.
        private double _test_regionEasingXDuration = 0.1;
        public double test_regionEasingXDuration
        {
            get { return _test_regionEasingXDuration; }
            set
            {
                if (value >= 0.00 && value <= 10.0)
                {
                    _test_regionEasingXDuration = value;
                    OnPropertyChanged("test_regionEasingXDuration");
                }
            }
        }

        // Maximum delta travel before maximum delta acceleration is applied.
        private double _test_deltaAccelMaxTravel = 0.425;
        public double test_deltaAccelMaxTravel
        {
            get { return _test_deltaAccelMaxTravel; }
            set
            {
                if (value >= 0.01 && value <= 0.5)
                {
                    _test_deltaAccelMaxTravel = value;
                    OnPropertyChanged("test_deltaAccelMaxTravel");
                }
            }
        }

        private double _test_regionEasingXOffset = 0.8;
        public double test_regionEasingXOffset
        {
            get { return _test_regionEasingXOffset; }
            set
            {
                if (value >= 0.0 && value <= 1.0)
                {
                    _test_regionEasingXOffset = value;
                    OnPropertyChanged("test_regionEasingXOffset");
                }
            }
        }

        // Weight multiplier used in smoothing routine.
        private double _test_smoothingWeight = 0.25;
        public double test_smoothingWeight
        {
            get { return _test_smoothingWeight; }
            set
            {
                if (value >= 0.0 && value <= 1.0)
                {
                    _test_smoothingWeight = value;
                    OnPropertyChanged("test_smoothingWeight");
                }
            }
        }

        // Initial mouse offset for fpsmouse calculations.
        private double _test_fpsmouseOffset = 0.8;
        public double test_fpsmouseOffset
        {
            get { return _test_fpsmouseOffset; }
            set
            {
                if (value >= 0.0 && value <= 1.0)
                {
                    _test_fpsmouseOffset = value;
                    OnPropertyChanged("test_fpsmouseOffset");
                }
            }
        }

        private double _CalibrationMarginX = 0.05625;
        public double CalibrationMarginX
        {
            get => _CalibrationMarginX;
            set
            {
                if (_CalibrationMarginX == value) return;
                _CalibrationMarginX = value;
                OnPropertyChanged("CalibrationMarginX");
            }
        }

        private double _CalibrationMarginY = 0.1;
        public double CalibrationMarginY
        {
            get => _CalibrationMarginY;
            set
            {
                if (_CalibrationMarginY == value) return;
                _CalibrationMarginY = value;
                OnPropertyChanged("CalibrationMarginY");
            }
        }

        private double _test_lightgun_oneeuro_mincutoff = 2.0;
        public double test_lightgun_oneeuro_mincutoff
        {
            get => _test_lightgun_oneeuro_mincutoff;
            set
            {
                if (value >= 0.0 && value <= 10.0)
                {
                    _test_lightgun_oneeuro_mincutoff = value;
                    OnPropertyChanged("test_lightgun_oneeuro_mincutoff");
                }
            }
        }

        private double _test_lightgun_oneeuro_beta = 0.92;
        public double test_lightgun_oneeuro_beta
        {
            get => _test_lightgun_oneeuro_beta;
            set
            {
                if (value >= 0.0 && value <= 1.0)
                {
                    _test_lightgun_oneeuro_beta = value;
                    OnPropertyChanged("test_lightgun_oneeuro_beta");
                }
            }
        }

        private int _pointer_positionSmoothing = 2;
        public int pointer_positionSmoothing
        {
            get { return _pointer_positionSmoothing; }
            set
            {
                _pointer_positionSmoothing = value;
                OnPropertyChanged("pointer_positionSmoothing");
            }
        }

        private double _pointer_positionRadius = 0.002;
        public double pointer_positionRadius
        {
            get { return _pointer_positionRadius; }
            set
            {
                _pointer_positionRadius = value;
                OnPropertyChanged("pointer_positionRadius");
            }
        }

        private double _fpsmouse_deadzone = 0.021;
        public double fpsmouse_deadzone
        {
            get { return _fpsmouse_deadzone; }
            set
            {
                _fpsmouse_deadzone = value;
                OnPropertyChanged("fpsmouse_deadzone");
            }
        }

        private int _fpsmouse_speed = 35;
        public int fpsmouse_speed
        {
            get { return _fpsmouse_speed; }
            set
            {
                _fpsmouse_speed = value;
                OnPropertyChanged("fpsmouse_speed");
            }
        }

        private double _shake_threshold = 0.2;
        public double shake_threshold
        {
            get { return _shake_threshold; }
            set
            {
                _shake_threshold = value;
                OnPropertyChanged("shake_threshold");
            }
        }

        private int _shake_count = 2;
        public int shake_count
        {
            get { return _shake_count; }
            set
            {
                _shake_count = value;
                OnPropertyChanged("shake_count");
            }
        }

        private int _shake_maxTimeInBetween = 500;
        public int shake_maxTimeInBetween
        {
            get { return _shake_maxTimeInBetween; }
            set
            {
                _shake_maxTimeInBetween = value;
                OnPropertyChanged("shake_maxTimeInBetween");
            }
        }

        private int _shake_pressedTime = 200;
        public int shake_pressedTime
        {
            get { return _shake_pressedTime; }
            set
            {
                _shake_pressedTime = value;
                OnPropertyChanged("shake_pressedTime");
            }
        }

        private int _xinput_rumbleThreshold_big = 200;
        public int xinput_rumbleThreshold_big
        {
            get { return _xinput_rumbleThreshold_big; }
            set
            {
                _xinput_rumbleThreshold_big = value;
                OnPropertyChanged("xinput_rumbleThreshold_big");
            }
        }

        private int _xinput_rumbleThreshold_small = 200;
        public int xinput_rumbleThreshold_small
        {
            get { return _xinput_rumbleThreshold_small; }
            set
            {
                _xinput_rumbleThreshold_small = value;
                OnPropertyChanged("xinput_rumbleThreshold_small");
            }
        }

        private int _wiimode_rumbleTime_short = 200;
        public int wiimode_rumbleTime_short
        {
            get { return _wiimode_rumbleTime_short; }
            set
            {
                _wiimode_rumbleTime_short = value;
                OnPropertyChanged("wiimode_rumbleTime_short");
            }
        }

        private int _wiimode_rumbleTime_long = 500;
        public int wiimode_rumbleTime_long
        {
            get { return _wiimode_rumbleTime_long; }
            set
            {
                _wiimode_rumbleTime_long = value;
                OnPropertyChanged("wiimode_rumbleTime_long");
            }
        }

        private int _wiimode_rumbleTime_alternatingOn = 100;
        public int wiimode_rumbleTime_alternatingOn
        {
            get { return _wiimode_rumbleTime_alternatingOn; }
            set
            {
                _wiimode_rumbleTime_alternatingOn = value;
                OnPropertyChanged("wiimode_rumbleTime_alternatingOn");
            }
        }

        private int _wiimode_rumbleTime_alternatingOff = 100;
        public int wiimode_rumbleTime_alternatingOff
        {
            get { return _wiimode_rumbleTime_alternatingOff; }
            set
            {
                _wiimode_rumbleTime_alternatingOff = value;
                OnPropertyChanged("wiimode_rumbleTime_alternatingOff");
            }
        }

        private int _wiimode_loopSoundTime = 200;
        public int wiimode_loopSoundTime
        {
            get { return _wiimode_loopSoundTime; }
            set
            {
                _wiimode_loopSoundTime = value;
                OnPropertyChanged("wiimode_loopSoundTime");
            }
        }

        private int[] _color_ID1 = { 128, 255, 0 };
        public int[] Color_ID1
        {
            get { return _color_ID1; }
            set
            {
                if (value.Length == 3)
                {
                    _color_ID1[0] = Math.Max(0, Math.Min(255, value[0]));
                    _color_ID1[1] = Math.Max(0, Math.Min(255, value[1]));
                    _color_ID1[2] = Math.Max(0, Math.Min(255, value[2]));
                    OnPropertyChanged("Color_ID");
                }
            }
        }

        private int[] _color_ID2 = { 197, 0, 255 };
        public int[] Color_ID2
        {
            get { return _color_ID2; }
            set
            {
                if (value.Length == 3)
                {
                    _color_ID2[0] = Math.Max(0, Math.Min(255, value[0]));
                    _color_ID2[1] = Math.Max(0, Math.Min(255, value[1]));
                    _color_ID2[2] = Math.Max(0, Math.Min(255, value[2]));
                    OnPropertyChanged("Color_ID");
                }
            }
        }

        private int[] _color_ID3 = { 0, 220, 255 };
        public int[] Color_ID3
        {
            get { return _color_ID3; }
            set
            {
                if (value.Length == 3)
                {
                    _color_ID3[0] = Math.Max(0, Math.Min(255, value[0]));
                    _color_ID3[1] = Math.Max(0, Math.Min(255, value[1]));
                    _color_ID3[2] = Math.Max(0, Math.Min(255, value[2]));
                    OnPropertyChanged("Color_ID");
                }
            }
        }

        private int[] _color_ID4 = { 255, 255, 0 };
        public int[] Color_ID4
        {
            get { return _color_ID4; }
            set
            {
                if (value.Length == 3)
                {
                    _color_ID4[0] = Math.Max(0, Math.Min(255, value[0]));
                    _color_ID4[1] = Math.Max(0, Math.Min(255, value[1]));
                    _color_ID4[2] = Math.Max(0, Math.Min(255, value[2]));
                    OnPropertyChanged("Color_ID");
                }
            }
        }

        private static string SETTINGS_FILENAME = System.AppDomain.CurrentDomain.BaseDirectory + "settings.json";

        private static Settings defaultInstance;

        public static Settings Default
        {
            get
            {
                if (defaultInstance == null)
                {
                    defaultInstance = Load();
                }
                return defaultInstance;
            }
        }

        private static Settings Load()
        {
            Settings result;
            try
            {
                result = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(SETTINGS_FILENAME));
            }
            catch //If anything goes wrong, just load default values
            {
                result = new Settings();
            }

            return result;
        }

        public void Save()
        {
            File.WriteAllText(SETTINGS_FILENAME, JsonConvert.SerializeObject(Default, Formatting.Indented));
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
		private string _DefaultLanguage = ""; // Valor inicial vacío
        public string DefaultLanguage
        {
            get { return _DefaultLanguage; }
            set
            {
                _DefaultLanguage = value;
                OnPropertyChanged("DefaultLanguage");
            }
        }
        private bool _Debug = false;
        public bool Debug
        {
            get { return _Debug; }
            set
            {
                _Debug = value;
                OnPropertyChanged("Debug");
            }
        }

        private int _ShootsPerTarget = 3;
        public int ShootsPerTarget
        {
            get { return _ShootsPerTarget; }
            set
            {
                _ShootsPerTarget = value;
                OnPropertyChanged("ShootsPerTarget");
            }
        }

    }

}
