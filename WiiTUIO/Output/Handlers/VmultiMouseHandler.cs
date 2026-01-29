using System;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using VMultiDllWrapper;
using WiiTUIO.Filters;
using WiiTUIO.Properties;
using WiiTUIO.Provider;

namespace WiiTUIO.Output.Handlers
{
    public class VmultiMouseHandler : IButtonHandler, IStickHandler, ICursorHandler
    {
        private VMulti vmulti;
        private MouseReport report;
        private MouseReport lastReport = new MouseReport();

        // Remainder values used for partial mouse distance calculations.
        private double remainderX = 0.0;
        private double remainderY = 0.0;
        // PointerX and PointerY values from previous Wiimote poll.
        private double previousPointerX = 0.5;
        private double previousPointerY = 0.5;

        double previousPointerRadial = 0.0;
        double accelCurrentMultiRadial = 0.0;
        double accelEasingMultiRadial = 0.0;
        double accelTravelRadial = 0.0;
        Stopwatch deltaEasingTimeRadial = new Stopwatch();
        double totalTravelRadial = 0.0;

        
        // Add period of mouse movement when remote is out of IR range.
        private Stopwatch outOfReachElapsed;
        private bool outOfReachStatus = true;

        // Add dead period when remote is initially moved into IR range.
        private Stopwatch initialInReachElapsed;
        private bool initialInReachStatus = false;

        // Add small easing region in final acceleration region
        // for X axis
        private Stopwatch regionEasingX;

        private double mouseOffset = Settings.Default.test_fpsmouseOffset;

        private CursorPositionHelper cursorPositionHelper;
        private OneEuroFilter testLightFilterX = new OneEuroFilter(Settings.Default.test_lightgun_oneeuro_mincutoff,
            Settings.Default.test_lightgun_oneeuro_beta, 1.0);
        private OneEuroFilter testLightFilterY = new OneEuroFilter(Settings.Default.test_lightgun_oneeuro_mincutoff,
            Settings.Default.test_lightgun_oneeuro_beta, 1.0);

        // Measured in milliseconds
        public const int OUTOFREACH_ELAPSED_TIME = 1000;
        public const int INITIAL_INREACH_ELAPSED_TIME = 125;

        private bool initialMouseMove = false;
        private double mouseOffsetX = 0.0;
        private double mouseOffsetY = 0.0;
        private double unitX = 0.0;
        private double unitY = 0.0;
        private Point previousLightCursorCoorPoint = new Point(0.5, 0.5);
        private Point previousLightOutputCursorPoint = new Point(0.5, 0.5);
        private long previousLightTime = 0;
        private bool wasInReach;

        private float scalingFactorX;
        private float scalingFactorY;
        private int deltaX;
        private int deltaY;

        public VmultiMouseHandler(VMulti Vmulti)
        {
            this.vmulti = Vmulti;
            this.report = new MouseReport();
            cursorPositionHelper = new CursorPositionHelper();
            cursorPositionHelper = new CursorPositionHelper();
            this.outOfReachElapsed = new Stopwatch();
            this.initialInReachElapsed = new Stopwatch();
            this.regionEasingX = new Stopwatch();
            System.Drawing.Rectangle screenBounds = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor).Bounds;
            scalingFactorX = 32767f / screenBounds.Width;
            scalingFactorY = 32767f / screenBounds.Height;

            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            System.Drawing.Rectangle screenBounds = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor).Bounds;
            scalingFactorX = 32767f / screenBounds.Width;
            scalingFactorY = 32767f / screenBounds.Height;
        }

        public bool reset()
        {
            report = new MouseReport();

            // Create empty smoothing filters on profile reset
            testLightFilterX = new OneEuroFilter(Settings.Default.test_lightgun_oneeuro_mincutoff,
                Settings.Default.test_lightgun_oneeuro_beta, 1.0);
            testLightFilterY = new OneEuroFilter(Settings.Default.test_lightgun_oneeuro_mincutoff,
                Settings.Default.test_lightgun_oneeuro_beta, 1.0);

            previousLightCursorCoorPoint = new Point(0.5, 0.5);
            previousLightOutputCursorPoint = new Point(0.5, 0.5);
            previousLightTime = Stopwatch.GetTimestamp();

            return true;
        }

        public bool setButtonDown(string key)
        {
            if (Enum.IsDefined(typeof(VmultiMouseCode), key.ToUpper()))
            {
                VmultiMouseCode mouseCode = (VmultiMouseCode)Enum.Parse(typeof(VmultiMouseCode), key, true);
                VMultiDllWrapper.MouseButton mouseButton;
                switch (mouseCode)
                {
                    case VmultiMouseCode.MOUSELEFT:
                        mouseButton = VMultiDllWrapper.MouseButton.LeftButton;
                        report.ButtonDown(mouseButton);
                        break;
                    case VmultiMouseCode.MOUSEMIDDLE:
                        mouseButton = VMultiDllWrapper.MouseButton.MiddleButton;
                        report.ButtonDown(mouseButton);
                        break;
                    case VmultiMouseCode.MOUSERIGHT:
                        mouseButton = VMultiDllWrapper.MouseButton.RightButton;
                        report.ButtonDown(mouseButton);
                        break;
                    case VmultiMouseCode.MOUSEWHEELDOWN:
                        report.VerticalScroll(-1);
                        break;
                    case VmultiMouseCode.MOUSEWHEELUP:
                        report.VerticalScroll(1);
                        break;
                    case VmultiMouseCode.MOUSEXBUTTON1:
                        mouseButton = VMultiDllWrapper.MouseButton.X1Button;
                        report.ButtonDown(mouseButton);
                        break;
                    case VmultiMouseCode.MOUSEXBUTTON2:
                        mouseButton = VMultiDllWrapper.MouseButton.X2Button;
                        report.ButtonDown(mouseButton);
                        break;
                    default:
                        return false;
                }
                return true;
            }
            return false;
        }

        public bool setButtonUp(string key)
        {
            if (Enum.IsDefined(typeof(VmultiMouseCode), key.ToUpper()))
            {
                VmultiMouseCode mouseCode = (VmultiMouseCode)Enum.Parse(typeof(VmultiMouseCode), key, true);
                VMultiDllWrapper.MouseButton mouseButton;
                switch (mouseCode)
                {
                    case VmultiMouseCode.MOUSELEFT:
                        mouseButton = VMultiDllWrapper.MouseButton.LeftButton;
                        report.ButtonUp(mouseButton);
                        break;
                    case VmultiMouseCode.MOUSEMIDDLE:
                        mouseButton = VMultiDllWrapper.MouseButton.MiddleButton;
                        report.ButtonUp(mouseButton);
                        break;
                    case VmultiMouseCode.MOUSERIGHT:
                        mouseButton = VMultiDllWrapper.MouseButton.RightButton;
                        report.ButtonUp(mouseButton);
                        break;
                    case VmultiMouseCode.MOUSEXBUTTON1:
                        mouseButton = VMultiDllWrapper.MouseButton.X1Button;
                        report.ButtonUp(mouseButton);
                        break;
                    case VmultiMouseCode.MOUSEXBUTTON2:
                        mouseButton = VMultiDllWrapper.MouseButton.X2Button;
                        report.ButtonUp(mouseButton);
                        break;
                    default:
                        return false;
                }
                return true;
            }
            return false;
        }

        public bool setPosition(string key, CursorPos cursorPos)
        {
            key = key.ToLower();

            if (key.Equals("mouse"))
            {
                if (!cursorPos.OutOfReach)
                {
                    Point smoothedPos = cursorPositionHelper.getRelativePosition(new Point(cursorPos.X, cursorPos.Y));
                    report.SetPosition((ushort)(32767 * smoothedPos.X), (ushort)(32767 * smoothedPos.Y));
                    return true;
                }
            }
            else if (key.Equals("lightgunmouse") || key.Equals("lightgunmouse-4:3") || key.Equals("lightgunmouse-16:9"))
            {
                long currentTime = Stopwatch.GetTimestamp();
                long timeElapsed = currentTime - previousLightTime;
                double elapsedMs = timeElapsed * (1.0 / Stopwatch.Frequency);
                previousLightTime = currentTime;

                const double LIGHT_FUZZ = 0.003;
                const bool useFuzz = false;
                if (!cursorPos.OutOfReach)
                {
                    Point smoothedPos = new Point();

                    bool moveCursor = true;

                    

                    smoothedPos.X = testLightFilterX.Filter(cursorPos.LightbarX * 1.001, 1.0 / elapsedMs);
                    smoothedPos.Y = testLightFilterY.Filter(cursorPos.LightbarY * 1.001, 1.0 / elapsedMs);

                    if (useFuzz)
                    {
                        double diffX = smoothedPos.X - previousLightOutputCursorPoint.X;
                        double diffY = smoothedPos.Y - previousLightOutputCursorPoint.Y;
                        double magSqu = (diffX * diffX) + (diffY * diffY);
                        double deltaSqu = LIGHT_FUZZ * LIGHT_FUZZ;

                        //bool fuzzReached = (diffX > (LIGHT_FUZZ * ratioX)) ||
                        //    (diffY > (LIGHT_FUZZ * ratioY));
                        bool fuzzReached = magSqu >= deltaSqu;
                        moveCursor = !wasInReach || fuzzReached;
                    }

                    // Filter does not go back to absolute zero for reasons. Check
                    // for low number and reset to zero
                    if (Math.Abs(smoothedPos.X) < 0.0001) smoothedPos.X = 0.0;
                    if (Math.Abs(smoothedPos.Y) < 0.0001) smoothedPos.Y = 0.0;

                    // Clamp values
                    smoothedPos.X = Math.Min(1.0, Math.Max(0.0, smoothedPos.X));
                    smoothedPos.Y = Math.Min(1.0, Math.Max(0.0, smoothedPos.Y));

                    if (moveCursor)
                    {
                        report.SetPosition((ushort)(32767 * smoothedPos.X), (ushort)(32767 * smoothedPos.Y));

                        // Save current IR position
                        previousLightCursorCoorPoint = new Point(cursorPos.LightbarX, cursorPos.LightbarY);
                        if (useFuzz)
                        {
                            previousLightOutputCursorPoint = new Point(smoothedPos.X, smoothedPos.Y);
                        }
                    }

                    wasInReach = true;
                }
                else
                {
                    // Save last known position to smoothing buffer
                    testLightFilterX.Filter(previousLightCursorCoorPoint.X * 1.001, 1.0 / elapsedMs);
                    testLightFilterY.Filter(previousLightCursorCoorPoint.Y * 1.001, 1.0 / elapsedMs);

                    wasInReach = false;
                }

                return true;
            }

            return false;
        }
        public bool setValue(string key, double value)
        {
            key = key.ToLower();
            switch (key)
            {
                case "mousey+":
                    deltaY = (int)(-30 * value + 0.5);
                    break;
                case "mousey-":
                    deltaY = (int)(30 * value + 0.5);
                    break;
                case "mousex+":
                    deltaX = (int)(30 * value + 0.5);
                    break;
                case "mousex-":
                    deltaX = (int)(-30 * value + 0.5);
                    break;
                default:
                    return false;
            }
            return true;
        }

        public bool connect()
        {
            return true;
        }

        public bool disconnect()
        {
            vmulti.updateMouse(new MouseReport());
            vmulti.Dispose();
            return true;
        }

        public bool startUpdate()
        {
            return true;
        }

        public bool endUpdate()
        {
            if (report.MouseX == lastReport.MouseX && report.MouseY == lastReport.MouseY)
                report.SetPosition(0, 0);

            if (deltaX != 0 || deltaY != 0)
            {
                if (report.MouseX == 0 && report.MouseY == 0)
                {
                    report.MouseX = (ushort)Math.Max(0, Math.Min(32767, lastReport.MouseX + (deltaX * scalingFactorX)));
                    report.MouseY = (ushort)Math.Max(0, Math.Min(32767, lastReport.MouseY + (deltaY * scalingFactorY)));
                }
                else
                {
                    report.MouseX = (ushort)Math.Max(0, Math.Min(32767, report.MouseX + (deltaX * scalingFactorX)));
                    report.MouseY = (ushort)Math.Max(0, Math.Min(32767, report.MouseY + (deltaY * scalingFactorY)));
                }
            }

            if ((report.MouseX != 0 && report.MouseY != 0) || (deltaX != 0 || deltaY != 0))
            {
                lastReport = new MouseReport
                {
                    MouseX = report.MouseX,
                    MouseY = report.MouseY,
                };

                deltaX = 0;
                deltaY = 0;
            }

            return vmulti.updateMouse(report);
        }
    }

    public enum VmultiMouseCode
    {
        MOUSELEFT,
        MOUSEMIDDLE,
        MOUSERIGHT,
        MOUSEWHEELUP,
        MOUSEWHEELDOWN,
        MOUSEXBUTTON1,
        MOUSEXBUTTON2,
    }
}
