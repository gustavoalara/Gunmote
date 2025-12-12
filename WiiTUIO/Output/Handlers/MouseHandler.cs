using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using WiiTUIO.Filters;
using WiiTUIO.Properties;
using WiiTUIO.Provider;
using WindowsInput;
using WindowsInput.Native;

namespace WiiTUIO.Output.Handlers
{
    public class MouseHandler : IButtonHandler, IStickHandler, ICursorHandler
    {
        private InputSimulator inputSimulator;

        private bool mouseLeftDown = false;
        private bool mouseMiddleDown = false;
        private bool mouseRightDown = false;

        private bool mouseXButton1Down = false;
        private bool mouseXButton2Down = false;

        // Remainder values used for partial mouse distance calculations.
        private double remainderX = 0.0;
        private double remainderY = 0.0;
        // PointerX and PointerY values from previous Wiimote poll.
        private double previousPointerX = 0.5;
        private double previousPointerY = 0.5;

        // Check elapsed time that delta acceleration is applied.
        //private Stopwatch deltaEasingTimeX;
        //private Stopwatch deltaEasingTimeY;

        double previousPointerRadial = 0.0;
        double accelCurrentMultiRadial = 0.0;
        double accelEasingMultiRadial = 0.0;
        double accelTravelRadial = 0.0;
        Stopwatch deltaEasingTimeRadial = new Stopwatch();
        double totalTravelRadial = 0.0;

        // Keep track of current acceleration multiplier in directions.
        //private double accelHelperX = 0.0;
        //private double accelHelperY = 0.0;
        //// Keep track of travel value that caused acceleration
        //private double accelTravelX = 0.0;
        //private double accelTravelY = 0.0;

        // Add period of mouse movement when remote is out of IR range.
        private Stopwatch outOfReachElapsed;
        private bool outOfReachStatus = true;

        // Add dead period when remote is initially moved into IR range.
        private Stopwatch initialInReachElapsed;
        private bool initialInReachStatus = false;

        // Add small easing region in final acceleration region
        // for X axis
        private Stopwatch regionEasingX;
        //private bool enableRegionEasing = true;
        private Stopwatch shitTestDuration = new Stopwatch();

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
        //private double cosAngle = 0.0;
        //private double sinAngle = 0.0;
        private double unitX = 0.0;
        private double unitY = 0.0;
        private Point previousLightCursorCoorPoint = new Point(0.5, 0.5);
        private Point previousLightOutputCursorPoint = new Point(0.5, 0.5);
        private long previousLightTime = 0;
        private bool wasInReach;

        public MouseHandler()
        {
            this.inputSimulator = new InputSimulator();
            cursorPositionHelper = new CursorPositionHelper();
            this.inputSimulator = new InputSimulator();
            cursorPositionHelper = new CursorPositionHelper();
            //this.deltaEasingTimeX = new Stopwatch();
            //this.deltaEasingTimeY = new Stopwatch();
            this.outOfReachElapsed = new Stopwatch();
            this.initialInReachElapsed = new Stopwatch();
            this.regionEasingX = new Stopwatch();
        }
        
        public bool reset()
        {
            if (mouseLeftDown)
            {
                setButtonUp("mouseleft");
            }
            if (mouseRightDown)
            {
                setButtonUp("mouseright");
            }
            if (mouseMiddleDown)
            {
                setButtonUp("mousemiddle");
            }
            if (mouseXButton1Down)
            {
                setButtonUp("mousexbutton1");
            }
            if (mouseXButton2Down)
            {
                setButtonUp("mousexbutton2");
            }

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
            if (Enum.IsDefined(typeof(MouseCode), key.ToUpper()))
            {
                MouseCode mouseCode = (MouseCode)Enum.Parse(typeof(MouseCode), key, true);
                switch (mouseCode)
                {
                    case MouseCode.MOUSELEFT:
                        this.inputSimulator.Mouse.LeftButtonDown();
                        mouseLeftDown = true;
                        break;
                    case MouseCode.MOUSEMIDDLE:
                        this.inputSimulator.Mouse.MiddleButtonDown();
                        mouseMiddleDown = true;
                        break;
                    case MouseCode.MOUSERIGHT:
                        this.inputSimulator.Mouse.RightButtonDown();
                        mouseRightDown = true;
                        break;
                    case MouseCode.MOUSEWHEELDOWN:
                        this.inputSimulator.Mouse.VerticalScroll(-1);
                        break;
                    case MouseCode.MOUSEWHEELUP:
                        this.inputSimulator.Mouse.VerticalScroll(1);
                        break;
                    case MouseCode.MOUSEXBUTTON1:
                        this.inputSimulator.Mouse.XButtonDown(1);
                        mouseXButton1Down = true;
                        break;
                    case MouseCode.MOUSEXBUTTON2:
                        this.inputSimulator.Mouse.XButtonDown(2);
                        mouseXButton2Down = true;
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
            if (Enum.IsDefined(typeof(MouseCode), key.ToUpper()))
            {
                MouseCode mouseCode = (MouseCode)Enum.Parse(typeof(MouseCode), key, true);
                switch (mouseCode)
                {
                    case MouseCode.MOUSELEFT:
                        this.inputSimulator.Mouse.LeftButtonUp();
                        mouseLeftDown = false;
                        break;
                    case MouseCode.MOUSEMIDDLE:
                        this.inputSimulator.Mouse.MiddleButtonUp();
                        mouseMiddleDown = false;
                        break;
                    case MouseCode.MOUSERIGHT:
                        this.inputSimulator.Mouse.RightButtonUp();
                        mouseRightDown = false;
                        break;
                    case MouseCode.MOUSEXBUTTON1:
                        this.inputSimulator.Mouse.XButtonUp(1);
                        mouseXButton1Down = false;
                        break;
                    case MouseCode.MOUSEXBUTTON2:
                        this.inputSimulator.Mouse.XButtonUp(2);
                        mouseXButton2Down = false;
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
                    this.inputSimulator.Mouse.MoveMouseTo((65535 * smoothedPos.X), (65535 * smoothedPos.Y));
                    return true;
                }
            }

            else if (key.Equals("fpsmouse"))
            {
                bool shouldMoveFPSCursor = !outOfReachStatus;
                initialMouseMove = false;

                if (!cursorPos.OutOfReach)
                {
                    // If remote in IR range, uncheck out of reach status.
                    outOfReachStatus = false;
                    if (outOfReachElapsed.IsRunning)
                    {
                        outOfReachElapsed.Stop();
                    }

                    // Check if remote has been moved into IR range.
                    if (initialInReachStatus)
                    {
                        if (!initialInReachElapsed.IsRunning)
                        {
                            // Start timer if not running.
                            initialInReachElapsed.Restart();
                        }

                        if (initialInReachElapsed.IsRunning)
                        {
                            if (initialInReachElapsed.ElapsedMilliseconds < INITIAL_INREACH_ELAPSED_TIME)
                            {
                                shouldMoveFPSCursor = false;
                            }
                            else
                            {
                                shouldMoveFPSCursor = true;
                                initialInReachStatus = false;
                                initialMouseMove = true;
                                initialInReachElapsed.Stop();
                            }
                        }
                    }
                    else
                    {
                        shouldMoveFPSCursor = true;
                    }
                }
                else if (!outOfReachStatus)
                {
                    if (!outOfReachElapsed.IsRunning)
                    {
                        // Start timer if not running.
                        outOfReachElapsed.Restart();
                    }

                    if (outOfReachElapsed.IsRunning)
                    {
                        // Check if time has passed. If so, mark out of reach
                        // status bool shouldMoveFPSCursor = !outOfReachStatus;and stop timer.
                        long elapsed = outOfReachElapsed.ElapsedMilliseconds;
                        if (elapsed >= OUTOFREACH_ELAPSED_TIME)
                        {
                            outOfReachStatus = true;
                            outOfReachElapsed.Stop();
                        }
                        else
                        {
                            // Time has not elapsed. Keep out of reach status
                            // at false.
                            outOfReachStatus = false;
                        }
                    }

                }

                //Debug.WriteLine("DUDE BRO: " + Convert.ToString(initialMouseMove));

                // Checks show that remote should be considered in IR range.
                if (shouldMoveFPSCursor)
                //if (!outOfReachStatus)
                {
                    //Point smoothedPos = cursorPositionHelper.getSmoothedPosition(new Point(cursorPos.RelativeX, cursorPos.RelativeY));
                    // Use proper IR values for full IR range and to take
                    // margins into account.
                    Point smoothedPos = cursorPositionHelper.getFPSRelativePosition(new Point(cursorPos.MarginX, cursorPos.MarginY));

                    /* TODO: Consider sensor bar position?
                    if (Settings.Default.pointer_sensorBarPos == "top")
                    {
                        smoothedPos.Y = smoothedPos.Y - Settings.Default.pointer_sensorBarPosCompensation;
                    }
                    else if (Settings.Default.pointer_sensorBarPos == "bottom")
                    {
                        smoothedPos.Y = smoothedPos.Y + Settings.Default.pointer_sensorBarPosCompensation;
                    }
                    */
                    //double deadzone = Settings.Default.fpsmouse_deadzone; // TODO: Move to settings
                    double deadzone = Settings.Default.fpsmouse_deadzone;

                    // Make dead zone region circular instead of a square.
                    double tempdeadangle = Math.Atan2(-(smoothedPos.Y - 0.5), smoothedPos.X - 0.5);
                    unitX = Math.Abs(Math.Cos(tempdeadangle));
                    unitY = Math.Abs(Math.Sin(tempdeadangle));
                    double deadzoneX = deadzone * unitX;
                    double deadzoneY = deadzone * 1.0 * unitY;

                    double fps_mouse_speed = Settings.Default.fpsmouse_speed;

                    double testAccelMulti = Settings.Default.test_deltaAccelMulti;
                    double testAccelMinTravel = Settings.Default.test_deltaAccelMinTravel;
                    double testAccelMaxTravel = Settings.Default.test_deltaAccelMaxTravel > testAccelMinTravel ? Settings.Default.test_deltaAccelMaxTravel : 0.5;
                    double testAccelEasingDuration = Settings.Default.test_deltaAccelEasingDuration;
                    bool testDeltaAccel = Settings.Default.test_deltaAccel;

                    bool enableRegionEasing = Settings.Default.test_regionEasingXDuration > 0.0;
                    double easingOffset = Settings.Default.test_regionEasingXOffset;

                    double shiftX = 0.0;
                    double shiftY = 0.0;

                    // Find X distance away from assigned dead zone
                    if (Math.Abs(smoothedPos.X - 0.5) > deadzoneX)
                    {
                        if (smoothedPos.X >= 0.5)
                        {
                            shiftX = (smoothedPos.X - 0.5 - deadzoneX) / (1.0 - 0.5 - deadzoneX);
                        }
                        else
                        {
                            shiftX = (smoothedPos.X - (0.5 - deadzoneX)) / (1.0 - 0.5 - deadzoneX);
                        }
                    }

                    // Find Y distance away from assigned dead zone
                    if (Math.Abs(smoothedPos.Y - 0.5) > deadzoneY)
                    {
                        if (smoothedPos.Y >= 0.5)
                        {
                            shiftY = (smoothedPos.Y - 0.5 - deadzoneY) / (1.0 - 0.5 - deadzoneY);
                        }
                        else
                        {
                            shiftY = (smoothedPos.Y - (0.5 - deadzoneY)) / (1.0 - 0.5 - deadzoneY);
                        }
                    }

                    //double shiftX = Math.Abs(smoothedPos.X - 0.5) > deadzone ? (smoothedPos.X - 0.5) : 0;
                    //double shiftY = Math.Abs(smoothedPos.Y - 0.5) > deadzone ? (smoothedPos.Y - 0.5) : 0;

                    // Need sign components for later calculations
                    double signshiftX = (shiftX >= 0) ? 1.0 : -1.0;
                    double signshiftY = (shiftY >= 0) ? 1.0 : -1.0;

                    double sideX = shiftX; double sideY = shiftY;
                    double capX = unitX * 1.0; double capY = unitY * 1.0;

                    double absSideX = Math.Abs(sideX); double absSideY = Math.Abs(sideY);
                    if (absSideX > capX) capX = absSideX;
                    if (absSideY > capY) capY = absSideY;
                    double tempRatioX = capX > 0.0 ? sideX / capX : 0.0;
                    double tempRatioY = capY > 0.0 ? sideY / capY : 0.0;

                    // Need absolute values for later calculations
                    double absX = Math.Abs(tempRatioX);
                    double absY = Math.Abs(tempRatioY);

                    if (absX <= 0.75 && regionEasingX.IsRunning)
                    {
                        // No longer in easing region
                        regionEasingX.Stop();
                    }
                    else if (absX > 0.75 && regionEasingX.IsRunning &&
                        (previousPointerX >= 0.5) != (smoothedPos.X >= 0.5))
                    {
                        // Direction changed quickly. Restart easing timer.
                        regionEasingX.Restart();
                    }

                    // Use three types of acceleration depending on distance
                    // away from dead zone. Will need to change later.
                    if (absX <= 0.4)
                    {
                        //shiftX = 0.395 * absX;
                        //shiftX = 0.5 * absX;
                        //shiftX = 0.6 * absX;
                        shiftX = 0.65 * absX;
                    }
                    else if (absX <= 0.75)
                    {
                        //shiftX = 1.0 * absX - 0.242;
                        //shiftX = 1.0 * absX - 0.2;
                        //shiftX = 1.0 * absX - 0.16;
                        shiftX = 1.0 * absX - 0.14;
                    }
                    else
                    {
                        double tempAbsx = absX;

                        if (enableRegionEasing)
                        {
                            double easingDuration = Settings.Default.test_regionEasingXDuration;

                            double easingElapsed = 0.0;
                            double elapsedDiff = 1.0;
                            if (regionEasingX.IsRunning)
                            {
                                easingElapsed = regionEasingX.ElapsedMilliseconds;
                            }
                            else
                            {
                                regionEasingX.Restart();
                            }

                            double adjustedEasingDuration = ((4.0 * absX) - 3.0) * easingDuration;
                            //double adjustedEasingDuration = easingDuration;
                            if (easingDuration > 0.0 && adjustedEasingDuration > 0.0 &&
                                (easingElapsed * 0.001) < adjustedEasingDuration)
                            {
                                //elapsedDiff = (easingElapsed * 0.001) / easingDuration;
                                //double minAbsRegionX = 0.75;
                                double minAbsRegionX = absX + (easingOffset * (absX - 0.75));
                                elapsedDiff = (easingElapsed * 0.001) / adjustedEasingDuration;
                                //elapsedDiff = (absX - 0.75) * elapsedDiff  + 0.75;
                                elapsedDiff = (absX - minAbsRegionX) * elapsedDiff + minAbsRegionX;
                            }
                            else
                            {
                                elapsedDiff = absX;
                            }

                            tempAbsx = elapsedDiff;
                        }

                        //shiftX = 1.968 * tempAbsx - 0.968;
                        //shiftX = 1.80 * tempAbsx - 0.80;
                        //shiftX = 1.64 * tempAbsx - 0.64;
                        shiftX = 1.56 * tempAbsx - 0.56;
                    }

                    shiftX *= capX;

                    // Use three types of acceleration depending on distance
                    // away from dead zone. Will need to change later.
                    if (absY <= 0.4)
                    {
                        //shiftY = 0.395 * absY;
                        //shiftY = 0.5 * absY;
                        shiftY = 0.65 * absY;
                    }
                    else if (absY <= 0.75)
                    {
                        //shiftY = 1.0 * absY - 0.242;
                        //shiftY = 1.0 * absY - 0.2;
                        shiftY = 1.0 * absY - 0.14;
                    }
                    else
                    {
                        //shiftY = 1.968 * absY - 0.968;
                        //shiftY = 1.80 * absY - 0.80;
                        shiftY = 1.56 * absY - 0.56;
                    }

                    shiftY *= capY;

                    double minfactor = Math.Max(1.0, 1.2); // default 1.0
                    double minTravelStop = Math.Max(0.05, testAccelMinTravel);

                    // Calculate delta acceleration slope and offset.
                    double accelSlope = (testAccelMulti - minfactor) / (testAccelMaxTravel - testAccelMinTravel);
                    double accelOffset = minfactor - (accelSlope * testAccelMinTravel);

                    double hyp = Math.Sqrt((smoothedPos.X * smoothedPos.X) + (smoothedPos.Y * smoothedPos.Y));

                    if (testDeltaAccel)
                    {
                        if (hyp > 0.0 &&
                            Math.Abs(hyp - previousPointerRadial) >= testAccelMinTravel &&
                            (hyp - previousPointerRadial >= 0.0))
                        {
                            double tempTravel = Math.Abs(hyp - previousPointerRadial);
                            double tempDist = tempTravel;

                            if (totalTravelRadial == 0.0)
                            {
                                totalTravelRadial = tempTravel;
                                accelEasingMultiRadial = (accelSlope * tempDist + accelOffset);
                            }
                            else
                            {
                                totalTravelRadial += tempDist;
                                double tempEasingDist = totalTravelRadial;
                                //tempDist = tempEasingDist;
                                //tempTravel = tempDist;
                                accelEasingMultiRadial = (accelSlope * tempEasingDist + accelOffset);
                            }


                            accelCurrentMultiRadial = (accelSlope * tempDist + accelOffset);
                            shiftX = shiftX * accelCurrentMultiRadial;
                            shiftY = shiftY * accelCurrentMultiRadial;
                            accelTravelRadial = tempTravel;

                            deltaEasingTimeRadial.Restart();

                            previousPointerRadial = hyp;
                            previousPointerX = smoothedPos.X;
                            previousPointerY = smoothedPos.Y;
                        }
                        else if (hyp > 0.0 && accelCurrentMultiRadial > 0.0 &&
                            Math.Abs(previousPointerRadial - hyp) < minTravelStop &&
                            !(
                            (previousPointerX >= 0.5) != (smoothedPos.X >= 0.5) &&
                            (previousPointerY >= 0.5) != (smoothedPos.Y >= 0.5))
                            )
                        {
                            double timeElapsed = deltaEasingTimeRadial.ElapsedMilliseconds;
                            //currentTime = Stopwatch.GetTimestamp();
                            //double timeElapsed = (currentTime - previousTime) * (1.0 / Stopwatch.Frequency) * 1000.0;
                            double elapsedDiff = 1.0;
                            double tempAccel = accelCurrentMultiRadial;
                            double tempTravel = accelTravelRadial;

                            if (hyp - previousPointerRadial <= 0.0)
                            {
                                double tempmix2 = Math.Abs(hyp - previousPointerRadial);
                                tempmix2 = Math.Min(tempmix2, minTravelStop);
                                double tempmixslope = (testAccelMinTravel - tempTravel) / minTravelStop;
                                double tempshitintercept = tempTravel;
                                double finalmanham = (tempmixslope * tempmix2 + tempshitintercept);

                                tempTravel = finalmanham;
                                tempAccel = (accelSlope * (tempTravel) + accelOffset);
                            }

                            double elapsedDuration = testAccelEasingDuration * (accelEasingMultiRadial / testAccelMulti);
                            //Trace.WriteLine($"TIME ELAPSED: {timeElapsed} {tempAccel} {elapsedDuration}");
                            if (elapsedDuration > 0.0 && (timeElapsed * 0.001) < elapsedDuration)
                            {
                                elapsedDiff = ((timeElapsed * 0.001) / elapsedDuration);
                                elapsedDiff = (1.0 - tempAccel) * (elapsedDiff * elapsedDiff * elapsedDiff) + tempAccel;
                                shiftX = elapsedDiff * shiftX;
                                shiftY = elapsedDiff * shiftY;

                                //Trace.WriteLine($"CONITNUING {elapsedDiff}");
                            }
                            else
                            {
                                // Easing time has ended. Reset values.
                                previousPointerRadial = hyp;
                                accelCurrentMultiRadial = 0.0;
                                accelTravelRadial = 0.0;
                                deltaEasingTimeRadial.Reset();
                                accelEasingMultiRadial = 0.0;
                                totalTravelRadial = 0.0;
                                //previousTime = currentTime;
                                previousPointerX = smoothedPos.X;
                                previousPointerY = smoothedPos.Y;
                                //inDuration = false;

                                //Trace.WriteLine($"DURATION ENDED");
                            }
                        }
                        else
                        {
                            previousPointerRadial = hyp;
                            accelCurrentMultiRadial = 0.0;
                            accelTravelRadial = 0.0;
                            accelEasingMultiRadial = 0.0;
                            totalTravelRadial = 0.0;
                            deltaEasingTimeRadial.Reset();
                            previousPointerX = smoothedPos.X;
                            previousPointerY = smoothedPos.Y;
                        }
                    }
                    else
                    {
                        previousPointerRadial = hyp;
                        previousPointerX = smoothedPos.X;
                        previousPointerY = smoothedPos.Y;
                        accelCurrentMultiRadial = 0.0;
                        accelTravelRadial = 0.0;
                        accelEasingMultiRadial = 0.0;
                        totalTravelRadial = 0.0;
                        {
                            deltaEasingTimeRadial.Reset();
                        }
                    }

                    // If deltaX >= 0.1 and displacement is increasing then
                    // use acceleration multiplier.
                    //if (absX > 0.0 && testDeltaAccel && !initialMouseMove &&
                    //    Math.Abs(previousPointerX - smoothedPos.X) >= testAccelMinTravel &&
                    //   (smoothedPos.X - previousPointerX >= 0.0) == (smoothedPos.X >= 0.5))
                    //{
                    //    double tempTravel = Math.Min(Math.Abs(previousPointerX - smoothedPos.X), testAccelMaxTravel);
                    //    if (accelHelperX > 1.0)
                    //    {
                    //        // Already in acceleration mode. Add accel
                    //        // dead zone to travel.
                    //        //tempTravel = Math.Min(tempTravel + testAccelMinTravel, testAccelMaxTravel);
                    //    }

                    //    //double tempDist = Math.Min(tempTravel / 0.5, 1.0);
                    //    double tempDist = Math.Min(tempTravel, testAccelMaxTravel);

                    //    /*double currentAccelMultiTemp = (accelSlope * tempDist + accelOffset);
                    //    double getMultiDiff = (currentAccelMultiTemp - 1.0) / (testAccelMulti - 1.0);
                    //    //currentAccelMultiTemp = -(testAccelMulti - 1.0) / (getMultiDiff * (getMultiDiff - 2.0)) + 1.0;
                    //    currentAccelMultiTemp = (testAccelMulti - 1.0) / Math.Sin(getMultiDiff * (Math.PI / 2.0)) + 1.0;

                    //    shiftX = shiftX * currentAccelMultiTemp;
                    //    previousPointerX = smoothedPos.X;
                    //    accelHelperX = currentAccelMultiTemp;
                    //    accelTravelX = tempTravel;
                    //    deltaEasingTimeX.Restart();
                    //    */

                    //    shiftX = shiftX * (accelSlope * tempDist + accelOffset);
                    //    previousPointerX = smoothedPos.X;
                    //    accelHelperX = (accelSlope * tempDist + accelOffset);
                    //    accelTravelX = tempTravel;
                    //    deltaEasingTimeX.Restart();
                    //}
                    //else if (absX > 0.0 && testDeltaAccel && !initialMouseMove && testAccelEasingDuration > 0.00 &&
                    //        accelHelperX > 0.0 &&
                    //        Math.Abs(smoothedPos.X - previousPointerX) < testAccelMinTravel &&
                    //        (previousPointerX >= 0.5) == (smoothedPos.X >= 0.5))
                    //{
                    //    double timeElapsed = deltaEasingTimeX.ElapsedMilliseconds;
                    //    double elapsedDiff = 1.0;
                    //    double tempAccel = accelHelperX;
                    //    double tempTravel = accelTravelX;

                    //    if ((smoothedPos.X - previousPointerX >= 0.0) != (smoothedPos.X >= 0.5))
                    //    {
                    //        // Travelling towards dead zone. Decrease acceleration and duration.
                    //        double minstop2 = Math.Min(testAccelMinTravel, tempTravel);
                    //        double tempmix2 = Math.Abs(smoothedPos.X - previousPointerX);
                    //        tempmix2 = Math.Min(tempmix2, minstop2);

                    //        double tempmixslope = (testAccelMinTravel - tempTravel) / (minstop2);
                    //        double tempshitintercept = tempTravel;

                    //        double finalmanham = (tempmixslope * tempmix2 + tempshitintercept);
                    //        //tempAccel = finalmanham;
                    //        tempTravel = finalmanham;
                    //        tempAccel = (accelSlope * tempTravel + accelOffset);

                    //        /*tempTravel = Math.Min(Math.Abs(previousPointerX - smoothedPos.X), testAccelMaxTravel);
                    //        tempTravel = Math.Max(Math.Min((accelTravelX - tempTravel), testAccelMaxTravel), testAccelMinTravel);
                    //        tempAccel = (accelSlope * tempTravel + accelOffset);
                    //        */
                    //    }

                    //    double elapsedDuration = testAccelEasingDuration * (tempAccel / testAccelMulti);

                    //    /*double getMultiDiff = (tempAccel - 1.0) / (testAccelMulti - 1.0);
                    //    //double tempinner = getMultiDiff * (getMultiDiff - 2.0);
                    //    //timeElapsed = (-testAccelEasingDuration * tempinner + 0.0);
                    //    //double currentAccelMultiTemp = -(testAccelMulti - 1.0) * tempinner + 1.0;
                    //    double tempinner = Math.Sin(getMultiDiff * (Math.PI / 2.0));
                    //    timeElapsed = testAccelEasingDuration * tempinner + 0.0;
                    //    double currentAccelMultiTemp = (testAccelMulti - 1.0) * tempinner + 1.0;
                    //    tempAccel = currentAccelMultiTemp;
                    //    */

                    //    if (elapsedDuration > 0.0 && (timeElapsed * 0.001) < elapsedDuration)
                    //    {
                    //        elapsedDiff = ((timeElapsed * 0.001) / elapsedDuration);
                    //        elapsedDiff = (1.0 - tempAccel) * (elapsedDiff * elapsedDiff * elapsedDiff) + tempAccel;
                    //        shiftX = elapsedDiff * shiftX;
                    //    }
                    //    else
                    //    {
                    //        // Easing time has ended. Reset values.
                    //        previousPointerX = smoothedPos.X;
                    //        accelHelperX = 0.0;
                    //        accelTravelX = 0.0;
                    //        deltaEasingTimeX.Stop();
                    //        //regionEasingX.Stop();
                    //    }
                    //}
                    //else
                    //{
                    //    // Don't apply acceleration. Reset values.
                    //    previousPointerX = smoothedPos.X;
                    //    accelHelperX = 0.0;
                    //    accelTravelX = 0.0;
                    //    if (deltaEasingTimeX.IsRunning)
                    //    {
                    //        deltaEasingTimeX.Stop();
                    //    }
                    //}

                    //// If deltaY >= 0.1 and displacement is increasing then
                    //// use acceleration multiplier.
                    //if (absY > 0.0 && testDeltaAccel && !initialMouseMove && Math.Abs(previousPointerY - smoothedPos.Y) >= testAccelMinTravel &&
                    //   (smoothedPos.Y - previousPointerY >= 0.0) == (smoothedPos.Y >= 0.5))
                    //{
                    //    double tempTravel = Math.Min(Math.Abs(previousPointerY - smoothedPos.Y), testAccelMaxTravel);
                    //    if (accelHelperY > 1.0)
                    //    {
                    //        // Already in acceleration mode. Add accel
                    //        // dead zone to travel.
                    //        //tempTravel = Math.Min(tempTravel + testAccelMinTravel, testAccelMaxTravel);
                    //    }

                    //    //double tempDist = Math.Min(tempTravel / 0.5, 1.0);
                    //    double tempDist = Math.Min(tempTravel, testAccelMaxTravel);

                    //    /*double currentAccelMultiTemp = (accelSlope * tempDist + accelOffset);
                    //    double getMultiDiff = (currentAccelMultiTemp - 1.0) / (testAccelMulti - 1.0);
                    //    //currentAccelMultiTemp = -(testAccelMulti - 1.0) / (getMultiDiff * (getMultiDiff - 2.0)) + 1.0;
                    //    currentAccelMultiTemp = (testAccelMulti - 1.0) / Math.Sin(getMultiDiff * (Math.PI / 2.0)) + 1.0;

                    //    shiftY = shiftY * currentAccelMultiTemp;
                    //    previousPointerY = smoothedPos.Y;
                    //    accelHelperY = currentAccelMultiTemp;
                    //    accelTravelY = tempTravel;
                    //    deltaEasingTimeY.Restart();
                    //    */

                    //    shiftY = shiftY * (accelSlope * tempDist + accelOffset);
                    //    previousPointerY = smoothedPos.Y;
                    //    accelHelperY = (accelSlope * tempDist + accelOffset);
                    //    accelTravelY = tempTravel;
                    //    deltaEasingTimeY.Restart();
                    //}
                    //else if (absY > 0.0 && testDeltaAccel && !initialMouseMove && testAccelEasingDuration > 0.00 &&
                    //        accelHelperY > 0.0 &&
                    //        Math.Abs(smoothedPos.Y - previousPointerY) < testAccelMinTravel &&
                    //        (previousPointerY >= 0.5) == (smoothedPos.Y >= 0.5))
                    //{
                    //    double timeElapsed = deltaEasingTimeY.ElapsedMilliseconds;
                    //    double elapsedDiff = 1.0;
                    //    double tempAccel = accelHelperY;
                    //    double tempTravel = accelTravelY;

                    //    if ((smoothedPos.Y - previousPointerY >= 0.0) != (smoothedPos.Y >= 0.5))
                    //    {
                    //        // Travelling towards dead zone. Decrease acceleration and duration.
                    //        double minstop2 = Math.Min(testAccelMinTravel, tempTravel);
                    //        double tempmix2 = Math.Abs(smoothedPos.Y - previousPointerY);
                    //        tempmix2 = Math.Min(tempmix2, minstop2);

                    //        double tempmixslope = (testAccelMinTravel - tempTravel) / (minstop2);
                    //        double tempshitintercept = tempTravel;

                    //        double finalmanham = (tempmixslope * tempmix2 + tempshitintercept);
                    //        //tempAccel = finalmanham;
                    //        tempTravel = finalmanham;
                    //        tempAccel = (accelSlope * tempTravel + accelOffset);

                    //        /*tempTravel = Math.Min(Math.Abs(previousPointerY - smoothedPos.Y), testAccelMaxTravel);
                    //        tempTravel = Math.Max(Math.Min((accelTravelY - tempTravel), testAccelMaxTravel), testAccelMinTravel);
                    //        tempAccel = (accelSlope * tempTravel + accelOffset);
                    //        */
                    //    }

                    //    double elapsedDuration = testAccelEasingDuration * (tempAccel / testAccelMulti);

                    //    /*double getMultiDiff = (tempAccel - 1.0) / (testAccelMulti - 1.0);
                    //    //double tempinner = getMultiDiff * (getMultiDiff - 2.0);
                    //    //timeElapsed = -testAccelEasingDuration * tempinner + 0.0;
                    //    //double currentAccelMultiTemp = -(testAccelMulti - 1.0) * tempinner + 1.0;
                    //    double tempinner = Math.Sin(getMultiDiff * (Math.PI / 2.0));
                    //    timeElapsed = testAccelEasingDuration * tempinner + 0.0;
                    //    double currentAccelMultiTemp = (testAccelMulti - 1.0) * tempinner + 1.0;
                    //    tempAccel = currentAccelMultiTemp;
                    //    */

                    //    if (elapsedDuration > 0.0 && (timeElapsed * 0.001) < elapsedDuration)
                    //    {
                    //        elapsedDiff = ((timeElapsed * 0.001) / elapsedDuration);
                    //        elapsedDiff = (1.0 - tempAccel) * (elapsedDiff * elapsedDiff * elapsedDiff) + tempAccel;
                    //        shiftY = elapsedDiff * shiftY;
                    //    }
                    //    else
                    //    {
                    //        // Easing time has ended. Reset values.
                    //        previousPointerY = smoothedPos.Y;
                    //        accelHelperY = 0.0;
                    //        accelTravelY = 0.0;
                    //        deltaEasingTimeY.Stop();
                    //    }
                    //}
                    //else
                    //{
                    //    // Don't apply acceleration. Reset values.
                    //    previousPointerY = smoothedPos.Y;
                    //    accelHelperY = 0.0;
                    //    accelTravelY = 0.0;
                    //    if (deltaEasingTimeY.IsRunning)
                    //    {
                    //        deltaEasingTimeY.Stop();
                    //    }
                    //}

                    // Add sign bit
                    shiftX = signshiftX * shiftX;
                    shiftY = signshiftY * shiftY;

                    //double currentoffset = 0.037;
                    //double currentoffset = 0.0426;
                    //const double currentoffset = 0.09236;
                    //const double currentoffset = 0.25;
                    mouseOffsetX = mouseOffset * unitX;
                    mouseOffsetY = mouseOffset * unitY;
                    // Find initial relative mouse speed
                    double mouseX = 0.0;
                    double mouseY = 0.0;
                    if (absX > 0.0)
                    {
                        mouseX = (fps_mouse_speed - mouseOffsetX) * shiftX + (mouseOffsetX * signshiftX);
                    }
                    else
                    {
                        remainderX = 0.0;
                    }

                    if (absY > 0.0)
                    {
                        mouseY = (fps_mouse_speed - mouseOffsetY) * shiftY + (mouseOffsetY * signshiftY);
                    }
                    else
                    {
                        remainderY = 0.0;
                    }

                    // Only apply remainder if both current displacement and
                    // remainder follow the same direction.
                    if ((remainderX > 0) == (mouseX > 0))
                    {
                        mouseX += remainderX;
                    }

                    // Only apply remainder if both current displacement and
                    // remainder follow the same direction.
                    if ((remainderY > 0) == (mouseY > 0))
                    {
                        mouseY += remainderY;
                    }

                    // Make sure relative mouse movement does not exceed 127 pixels.
                    if (Math.Abs(mouseX) > 127)
                    {
                        mouseX = (mouseX < 0) ? -127 : 127;
                    }

                    // Make sure relative mouse movement does not exceed 127 pixels.
                    if (Math.Abs(mouseY) > 127)
                    {
                        mouseY = (mouseY < 0) ? -127 : 127;
                    }

                    // Reset remainder values
                    remainderX = 0.0;
                    remainderY = 0.0;

                    // Round mouseX distance to zero and save remainder.
                    // Prefer over rounding to nearest.
                    if (mouseX > 0.0)
                    {
                        double temp = Math.Floor(mouseX);
                        remainderX = mouseX - temp;
                        mouseX = temp;
                    }
                    else if (mouseX < 0.0)
                    {
                        double temp = Math.Ceiling(mouseX);
                        remainderX = mouseX - temp;
                        mouseX = temp;
                    }

                    // Round mouseY distance to zero and save remainder.
                    // Prefer over rounding to nearest.
                    if (mouseY > 0.0)
                    {
                        double temp = Math.Floor(mouseY);
                        remainderY = mouseY - temp;
                        mouseY = temp;
                    }
                    else if (mouseY < 0.0)
                    {
                        double temp = Math.Ceiling(mouseY);
                        remainderY = mouseY - temp;
                        mouseY = temp;
                    }

                    //this.inputSimulator.Mouse.MoveMouseBy((int)(Settings.Default.fpsmouse_speed * shiftX), (int)(Settings.Default.fpsmouse_speed * shiftY));
                    // Need to double check if sync would happen if (0,0).
                    if (mouseX != 0.0 || mouseY != 0.0)
                    {
                        this.inputSimulator.Mouse.MoveMouseBy((int)mouseX, (int)mouseY);
                    }

                    return true;
                }
                else
                {
                    // Consider outside of IR range. Reset some values.
                    remainderX = 0.0;
                    remainderY = 0.0;

                    //accelHelperX = 0.0;
                    //accelHelperY = 0.0;
                    //accelTravelX = 0.0;
                    //accelTravelY = 0.0;

                    previousPointerX = 0.5;
                    previousPointerY = 0.5;

                    previousPointerRadial = 0.0;
                    accelCurrentMultiRadial = 0.0;
                    accelTravelRadial = 0.0;
                    accelEasingMultiRadial = 0.0;
                    totalTravelRadial = 0.0;
                    //{
                    //    deltaEasingTimeRadial.Reset();
                    //}

                    //if (deltaEasingTimeX.IsRunning)
                    //{
                    //    deltaEasingTimeX.Stop();
                    //}

                    //if (deltaEasingTimeY.IsRunning)
                    //{
                    //    deltaEasingTimeY.Stop();
                    //}

                    if (deltaEasingTimeRadial.IsRunning)
                    {
                        deltaEasingTimeRadial.Reset();
                    }

                    initialInReachStatus = true;

                    if (regionEasingX.IsRunning)
                    {
                        regionEasingX.Reset();
                    }
                }
            }

            else if (key.Equals("lightgunmouse") || key.Equals("lightgunmouse-4:3"))
            {
                //if (!shitTestDuration.IsRunning)
                //{
                //    shitTestDuration.Start();
                //}

                long currentTime = Stopwatch.GetTimestamp();
                long timeElapsed = currentTime - previousLightTime;
                double elapsedMs = timeElapsed * (1.0 / Stopwatch.Frequency);
                //Trace.WriteLine($"ELAPSED DUR: {elapsed}");
                previousLightTime = currentTime;

                const double LIGHT_FUZZ = 0.003;
                const bool useFuzz = false;
                if (!cursorPos.OutOfReach)
                {
                    //Point smoothedPos = cursorPositionHelper.GetLightbarRelativePosition(new Point(cursorPos.LightbarX, cursorPos.LightbarY));
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

                    //Trace.WriteLine($"{cursorPos.LightbarX} {cursorPos.LightbarY} {diffX} {diffY} {moveCursor}");
                    //if (moveCursor)
                    //{
                    //    // Use current IR coordinates
                    //    // Adjust sensitivity to work around rounding in filter method
                    //    smoothedPos.X = testLightFilterX.Filter(cursorPos.LightbarX * 1.001, 1.0 / elapsedMs);
                    //    smoothedPos.Y = testLightFilterY.Filter(cursorPos.LightbarY * 1.001, 1.0 / elapsedMs);
                    //}
                    //else
                    //{
                    //    // Use previously saved cursor coordinates.
                    //    // Adjust sensitivity to work around rounding in filter method
                    //    smoothedPos.X = testLightFilterX.Filter(cursorPos.LightbarX * 1.001, 1.0 / elapsedMs);
                    //    smoothedPos.Y = testLightFilterX.Filter(cursorPos.LightbarX * 1.001, 1.0 / elapsedMs);

                    //    //Trace.WriteLine($"SMOOTHED {smoothedPos.X} {smoothedPos.Y}");
                    //    //Trace.WriteLine($"PREV {cursorPos.LightbarX} {cursorPos.LightbarY}");
                    //}

                    // Filter does not go back to absolute zero for reasons. Check
                    // for low number and reset to zero
                    if (Math.Abs(smoothedPos.X) < 0.0001) smoothedPos.X = 0.0;
                    if (Math.Abs(smoothedPos.Y) < 0.0001) smoothedPos.Y = 0.0;

                    // Clamp values
                    smoothedPos.X = Math.Min(1.0, Math.Max(0.0, smoothedPos.X));
                    smoothedPos.Y = Math.Min(1.0, Math.Max(0.0, smoothedPos.Y));

                    //Trace.WriteLine($"{cursorPos.LightbarX} | {smoothedPos.X} | {shitTestDuration.ElapsedMilliseconds}");
                    //Trace.WriteLine($"{cursorPos.LightbarY} | {smoothedPos.Y} | {shitTestDuration.ElapsedMilliseconds}");

                    //this.inputSimulator.Mouse.MoveMouseToPositionOnVirtualDesktop((65535 * smoothedPos.X), (65535 * smoothedPos.Y));
                    //this.inputSimulator.Mouse.MoveMouseToPositionOnVirtualDesktop((65535 * smoothedPos.X), (65535 * smoothedPos.Y));
                    if (moveCursor)
                    {
                        //Trace.WriteLine("MOVE CURSOR");

                        this.inputSimulator.Mouse.MoveMouseTo((65535 * smoothedPos.X), (65535 * smoothedPos.Y));

                        // Save current IR position
                        previousLightCursorCoorPoint = new Point(cursorPos.LightbarX, cursorPos.LightbarY);
                        if (useFuzz)
                        {
                            previousLightOutputCursorPoint = new Point(smoothedPos.X, smoothedPos.Y);
                        }
                    }

                    //shitTestDuration.Restart();

                    wasInReach = true;
                }
                else
                {
                    //testLightFilterX.Filter(0.5, 1.0 / 0.008);
                    //testLightFilterY.Filter(0.5, 1.0 / 0.008);
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
                    this.inputSimulator.Mouse.MoveMouseBy(0, (int)(-30 * value + 0.5));
                    break;
                case "mousey-":
                    this.inputSimulator.Mouse.MoveMouseBy(0, (int)(30 * value + 0.5));
                    break;
                case "mousex+":
                    this.inputSimulator.Mouse.MoveMouseBy((int)(30 * value + 0.5), 0);
                    break;
                case "mousex-":
                    this.inputSimulator.Mouse.MoveMouseBy((int)(-30 * value + 0.5), 0);
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
            return true;
        }

        public bool startUpdate()
        {
            return true;
        }

        public bool endUpdate()
        {
            return true;
        }
    }

    public enum MouseCode
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
