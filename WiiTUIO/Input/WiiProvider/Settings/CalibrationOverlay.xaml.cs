using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Timers;
using WiiTUIO.DeviceUtils;
using WiiTUIO.Properties;
using PointF = WiimoteLib.PointF;
using System.Diagnostics;
using WiimoteLib;
using System.Windows.Shapes; // Required for Line and Polygon
using System.ComponentModel; // Required for PropertyChangedEventArgs in SettingsChanged
using System.Windows.Media.Media3D;
using System.Globalization;
using WiiTUIO.Filters;
using static WiiTUIO.Resources.Resources; // Assumed to contain localized strings
using System.Windows.Documents;
using System.Linq; // Added for .Any() and .FirstOrDefault()
using System.Collections.Generic;

namespace WiiTUIO.Provider
{
    /// <summary>
    /// Interaction logic for CalibrationOverlay.xaml
    /// </summary>
    public partial class CalibrationOverlay : Window
    {
        private WiiKeyMapper keyMapper;
        private static CalibrationOverlay defaultInstance;
        private bool wasDebugActiveBeforeCalibration;
        private bool wasNotificationsActiveBeforeCalibration;

        private System.Windows.Forms.Screen primaryScreen;
        private IntPtr previousForegroundWindow = IntPtr.Zero;

        private bool hidden = true;

        private int step = 0; // Current calibration step

        // Properties to backup current values before calibration
        private float topBackup;
        private float bottomBackup;
        private float leftBackup;
        private float rightBackup;
        private float tlBackup;
        private float trBackup;
        private float centerXBackup;
        private float centerYBackup;
        private float offsetYBottomBackup;
        private float offsetYTopBackup;

        private double marginXBackup;
        private double marginYBackup;

        
        // Constant for the side length of the triangle
        private const double TRIANGLE_SIDE_LENGTH = 20.0;

        private double captWidth = 0.0;
        private double captHeight = 0.0;
        private double captRight = 0.0;
        private double captBottom = 0.0;
        private double captLeft = 0.0;
        private double captTop = 0.0;
        private List<PointF> lastCapturedRawLeds;

        private int SHOTS_PER_TARGET = Settings.Default.ShootsPerTarget;
        private int currentShotCount = 0;

        private struct ShotData
        {
            public double RelativeX;
            public double RelativeY;
            public float PitchOffsetY;
            public double Width; 
            public double Height; 
        }

        private List<ShotData> currentTargetShots = new List<ShotData>();

        public event Action OnCalibrationFinished;

        public static CalibrationOverlay Current
        {
            get
            {
                if (defaultInstance == null)
                {
                    defaultInstance = new CalibrationOverlay();
                }
                return defaultInstance;
            }
        }

        public CalibrationOverlay()
        {
            InitializeComponent();

            primaryScreen = DeviceUtil.GetScreen(Settings.Default.primaryMonitor);

            Settings.Default.PropertyChanged += SettingsChanged;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

            this.CalibrationCanvas.Visibility = Visibility.Hidden;

            // Compensate for DPI settings
            Loaded += (o, e) =>
            {
                this.updateWindowToScreen(primaryScreen);

                // Prevent OverlayWindow from appearing in the alt+tab menu.
                UIHelpers.HideFromAltTab(this);
                // ADDED CALL: Update lines and triangles when window loads
                UpdateCalibrationLinesAndTrianglesVisibility();
            };
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            this.primaryScreen = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                this.updateWindowToScreen(primaryScreen);
            }));
        }

        private void updateWindowToScreen(System.Windows.Forms.Screen screen)
        {
            PresentationSource source = PresentationSource.FromVisual(this);
            Matrix transformMatrix = source.CompositionTarget.TransformToDevice;

            this.Width = screen.Bounds.Width * transformMatrix.M22;
            this.Height = screen.Bounds.Height * transformMatrix.M11;
            UIHelpers.SetWindowPos((new WindowInteropHelper(this)).Handle, IntPtr.Zero, screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Width, screen.Bounds.Height, UIHelpers.SetWindowPosFlags.SWP_NOACTIVATE | UIHelpers.SetWindowPosFlags.SWP_NOZORDER);
            this.CalibrationCanvas.Width = this.Width;
            this.CalibrationCanvas.Height = this.Height;
            UIHelpers.TopmostFix(this);
            // ADDED CALL: Update lines and triangles after canvas size is updated
            UpdateCalibrationLinesAndTrianglesVisibility();
        }

        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "primaryMonitor")
            {
                primaryScreen = DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
                Dispatcher.BeginInvoke(new Action(delegate ()
                {
                    this.updateWindowToScreen(primaryScreen);
                }));
            }
            // ADDED CALL: If 4IR mode changes, we also need to update line and triangle visibility
            else if (e.PropertyName == "pointer_4IRMode")
            {
                UpdateCalibrationLinesAndTrianglesVisibility();
            }
        }

        // NEWLY ADDED METHOD: To update the visibility and position of calibration lines and triangles
        private void UpdateCalibrationLinesAndTrianglesVisibility(SolidColorBrush brush = null)
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                // Hide all lines and triangles initially
                VerticalLineLeft.Visibility = Visibility.Hidden;
                VerticalLineRight.Visibility = Visibility.Hidden;
                HorizontalLineCenter.Visibility = Visibility.Hidden;
                VerticalLineCenter.Visibility = Visibility.Hidden;

                TriangleLeftTop.Visibility = Visibility.Hidden;
                TriangleLeftBottom.Visibility = Visibility.Hidden;
                TriangleRightTop.Visibility = Visibility.Hidden;
                TriangleRightBottom.Visibility = Visibility.Hidden;
                TriangleCenterTop.Visibility = Visibility.Hidden;
                TriangleCenterBottom.Visibility = Visibility.Hidden;
                TriangleCenterLeft.Visibility = Visibility.Hidden;
                TriangleCenterRight.Visibility = Visibility.Hidden;

                // Hide all grid lines by default
                GridLineV1.Visibility = Visibility.Hidden;
                GridLineV2.Visibility = Visibility.Hidden;
                GridLineV3.Visibility = Visibility.Hidden;
                GridLineV4.Visibility = Visibility.Hidden;
                GridLineV5.Visibility = Visibility.Hidden;
                GridLineH1.Visibility = Visibility.Hidden;
                GridLineH2.Visibility = Visibility.Hidden;
                GridLineH3.Visibility = Visibility.Hidden;
                GridLineH4.Visibility = Visibility.Hidden;
                GridLineH5.Visibility = Visibility.Hidden;


                SolidColorBrush currentBrush = new SolidColorBrush(Colors.Green); // Default green color

                // If keyMapper is available, use its color for consistency
                if (keyMapper != null)
                {
                    Color pointColor = IDColor.getColor(keyMapper.WiimoteID);
                    pointColor.R = (byte)(pointColor.R * 0.8);
                    pointColor.G = (byte)(pointColor.G * 0.8);
                    pointColor.B = (byte)(pointColor.B * 0.8);
                    currentBrush = new SolidColorBrush(pointColor);
                }

                // Calculate a lighter green color for the grid (even lighter)
                Color lighterGreen = Color.FromArgb(
                    128,
                    (byte)Math.Min(255, currentBrush.Color.R + 100), // Increased brightness
                    (byte)Math.Min(255, currentBrush.Color.G + 100), // Increased brightness
                    (byte)Math.Min(255, currentBrush.Color.B + 100)  // Increased brightness
                );
                SolidColorBrush lighterBrush = new SolidColorBrush(lighterGreen);


                // Calculate the height of an equilateral triangle (distance from vertex to base)
                double triangleHeight = TRIANGLE_SIDE_LENGTH * Math.Sqrt(3) / 2;
                // Half the base of the triangle
                double halfBase = TRIANGLE_SIDE_LENGTH / 2;

                double centerX = this.ActualWidth / 2; // Defined here for use in both modes
                double centerY = this.ActualHeight / 2; // Defined here for use in both modes


                if (Settings.Default.pointer_4IRMode == "none" || Settings.Default.pointer_4IRMode == "square")
                {
                    // Logic for "none" or "square" mode (existing vertical lines)
                    double squareSide = this.ActualHeight; // Assuming the "square" is based on height
                    // Vertical lines extend across the entire height
                    double leftLineX = centerX - (squareSide / 2);
                    double rightLineX = centerX + (squareSide / 2);

                    VerticalLineLeft.X1 = leftLineX;
                    VerticalLineLeft.Y1 = 0;
                    VerticalLineLeft.X2 = leftLineX;
                    VerticalLineLeft.Y2 = this.ActualHeight;
                    VerticalLineLeft.Stroke = currentBrush;
                    VerticalLineLeft.Visibility = Visibility.Visible;

                    VerticalLineRight.X1 = rightLineX;
                    VerticalLineRight.Y1 = 0;
                    VerticalLineRight.X2 = rightLineX;
                    VerticalLineRight.Y2 = this.ActualHeight;
                    VerticalLineRight.Stroke = currentBrush;
                    VerticalLineRight.Visibility = Visibility.Visible;

                    // Triangles for vertical lines (none/square)
                    // Top Left Triangle (base at Y=0, vertex at leftLineX, points downwards)
                    TriangleLeftTop.Points = new PointCollection
                    {
                        new System.Windows.Point(leftLineX, triangleHeight),
                        new System.Windows.Point(leftLineX - halfBase, 0),
                        new System.Windows.Point(leftLineX + halfBase, 0)
                    };
                    TriangleLeftTop.Fill = currentBrush;
                    TriangleLeftTop.Visibility = Visibility.Visible;

                    // Bottom Left Triangle (base at Y=ActualHeight, vertex at leftLineX, points upwards)
                    TriangleLeftBottom.Points = new PointCollection
                    {
                        new System.Windows.Point(leftLineX, this.ActualHeight - triangleHeight),
                        new System.Windows.Point(leftLineX - halfBase, this.ActualHeight),
                        new System.Windows.Point(leftLineX + halfBase, this.ActualHeight)
                    };
                    TriangleLeftBottom.Fill = currentBrush;
                    TriangleLeftBottom.Visibility = Visibility.Visible;

                    // Top Right Triangle (base at Y=0, vertex at rightLineX, points downwards)
                    TriangleRightTop.Points = new PointCollection
                    {
                        new System.Windows.Point(rightLineX, triangleHeight),
                        new System.Windows.Point(rightLineX + halfBase, 0),
                        new System.Windows.Point(rightLineX - halfBase, 0)
                    };
                    TriangleRightTop.Fill = currentBrush;
                    TriangleRightTop.Visibility = Visibility.Visible;

                    // Bottom Right Triangle (base at Y=ActualHeight, vertex at rightLineX, points upwards)
                    TriangleRightBottom.Points = new PointCollection
                    {
                        new System.Windows.Point(rightLineX, this.ActualHeight - triangleHeight),
                        new System.Windows.Point(rightLineX + halfBase, this.ActualHeight),
                        new System.Windows.Point(rightLineX - halfBase, this.ActualHeight)
                    };
                    TriangleRightBottom.Fill = currentBrush;
                    TriangleRightBottom.Visibility = Visibility.Visible;

                    // --- Logic for the grid ---
                    // The grid will have 5 vertical and 5 horizontal lines, creating 4x4 sections.
                    // The first vertical and horizontal lines will be centered.
                    double gridSpacingX = this.ActualWidth / 6; // For 5 vertical lines (6 sections)
                    double gridSpacingY = this.ActualHeight / 6; // For 5 horizontal lines (6 sections)

                    // Define the dash array for dashed lines
                    DoubleCollection dashArray = new DoubleCollection { 2, 2 }; // 2 units on, 2 units off

                    // Vertical grid lines
                    // The central line (GridLineV3) is already at centerX
                    GridLineV1.X1 = centerX - 2 * gridSpacingX; GridLineV1.Y1 = 0; GridLineV1.X2 = centerX - 2 * gridSpacingX; GridLineV1.Y2 = this.ActualHeight; GridLineV1.Stroke = lighterBrush; GridLineV1.StrokeDashArray = dashArray; GridLineV1.Visibility = Visibility.Visible;
                    GridLineV2.X1 = centerX - gridSpacingX; GridLineV2.Y1 = 0; GridLineV2.X2 = centerX - gridSpacingX; GridLineV2.Y2 = this.ActualHeight; GridLineV2.Stroke = lighterBrush; GridLineV2.StrokeDashArray = dashArray; GridLineV2.Visibility = Visibility.Visible;
                    GridLineV3.X1 = centerX; GridLineV3.Y1 = 0; GridLineV3.X2 = centerX; GridLineV3.Y2 = this.ActualHeight; GridLineV3.Stroke = lighterBrush; GridLineV3.StrokeDashArray = dashArray; GridLineV3.Visibility = Visibility.Visible; // Central vertical line
                    GridLineV4.X1 = centerX + gridSpacingX; GridLineV4.Y1 = 0; GridLineV4.X2 = centerX + gridSpacingX; GridLineV4.Y2 = this.ActualHeight; GridLineV4.Stroke = lighterBrush; GridLineV4.StrokeDashArray = dashArray; GridLineV4.Visibility = Visibility.Visible;
                    GridLineV5.X1 = centerX + 2 * gridSpacingX; GridLineV5.Y1 = 0; GridLineV5.X2 = centerX + 2 * gridSpacingX; GridLineV5.Y2 = this.ActualHeight; GridLineV5.Stroke = lighterBrush; GridLineV5.StrokeDashArray = dashArray; GridLineV5.Visibility = Visibility.Visible;

                    // Horizontal grid lines
                    // The central line (GridLineH3) is already at centerY
                    GridLineH1.X1 = 0; GridLineH1.Y1 = centerY - 2 * gridSpacingY; GridLineH1.X2 = this.ActualWidth; GridLineH1.Y2 = centerY - 2 * gridSpacingY; GridLineH1.Stroke = lighterBrush; GridLineH1.StrokeDashArray = dashArray; GridLineH1.Visibility = Visibility.Visible;
                    GridLineH2.X1 = 0; GridLineH2.Y1 = centerY - gridSpacingY; GridLineH2.X2 = this.ActualWidth; GridLineH2.Y2 = centerY - gridSpacingY; GridLineH2.Stroke = lighterBrush; GridLineH2.StrokeDashArray = dashArray; GridLineH2.Visibility = Visibility.Visible;
                    GridLineH3.X1 = 0; GridLineH3.Y1 = centerY; GridLineH3.X2 = this.ActualWidth; GridLineH3.Y2 = centerY; GridLineH3.Stroke = lighterBrush; GridLineH3.StrokeDashArray = dashArray; GridLineH3.Visibility = Visibility.Visible; // Central horizontal line
                    GridLineH4.X1 = 0; GridLineH4.Y1 = centerY + gridSpacingY; GridLineH4.X2 = this.ActualWidth; GridLineH4.Y2 = centerY + gridSpacingY; GridLineH4.Stroke = lighterBrush; GridLineH4.StrokeDashArray = dashArray; GridLineH4.Visibility = Visibility.Visible;
                    GridLineH5.X1 = 0; GridLineH5.Y1 = centerY + 2 * gridSpacingY; GridLineH5.X2 = this.ActualWidth; GridLineH5.Y2 = centerY + 2 * gridSpacingY; GridLineH5.Stroke = lighterBrush; GridLineH5.StrokeDashArray = dashArray; GridLineH5.Visibility = Visibility.Visible;

                }
                else if (Settings.Default.pointer_4IRMode == "diamond")
                {
                    // Logic for "diamond" mode (cross lines)
                    // double centerX = this.ActualWidth / 2; // Already defined above
                    // double centerY = this.ActualHeight / 2; // Already defined above

                    // Horizontal line from right center to left
                    HorizontalLineCenter.X1 = this.ActualWidth;
                    HorizontalLineCenter.Y1 = centerY;
                    HorizontalLineCenter.X2 = 0;
                    HorizontalLineCenter.Y2 = centerY;
                    HorizontalLineCenter.Stroke = currentBrush;
                    HorizontalLineCenter.Visibility = Visibility.Visible;

                    // Vertical line from top center to bottom
                    VerticalLineCenter.X1 = centerX;
                    VerticalLineCenter.Y1 = 0;
                    VerticalLineCenter.X2 = centerX;
                    VerticalLineCenter.Y2 = this.ActualHeight;
                    VerticalLineCenter.Stroke = currentBrush;
                    VerticalLineCenter.Visibility = Visibility.Visible;

                    // Triangles for central lines (diamond)
                    // Top Center Triangle (base at X=centerX, Y=0, points downwards)
                    TriangleCenterTop.Points = new PointCollection
                    {
                        new System.Windows.Point(centerX, triangleHeight),
                        new System.Windows.Point(centerX - halfBase, 0),
                        new System.Windows.Point(centerX + halfBase, 0)
                    };
                    TriangleCenterTop.Fill = currentBrush;
                    TriangleCenterTop.Visibility = Visibility.Visible;

                    // Bottom Center Triangle (base at X=centerX, Y=ActualHeight, points upwards)
                    TriangleCenterBottom.Points = new PointCollection
                    {
                        new System.Windows.Point(centerX, this.ActualHeight - triangleHeight),
                        new System.Windows.Point(centerX - halfBase, this.ActualHeight),
                        new System.Windows.Point(centerX + halfBase, this.ActualHeight)
                    };
                    TriangleCenterBottom.Fill = currentBrush;
                    TriangleCenterBottom.Visibility = Visibility.Visible;

                    // Left Center Triangle (base at Y=centerY, X=0, points right)
                    TriangleCenterLeft.Points = new PointCollection
                    {
                        new System.Windows.Point(triangleHeight, centerY),
                        new System.Windows.Point(0, centerY - halfBase),
                        new System.Windows.Point(0, centerY + halfBase)
                    };
                    TriangleCenterLeft.Fill = currentBrush;
                    TriangleCenterLeft.Visibility = Visibility.Visible;

                    // Right Center Triangle (base at Y=centerY, X=ActualWidth, points left)
                    TriangleCenterRight.Points = new PointCollection
                    {
                        new System.Windows.Point(this.ActualWidth - triangleHeight, centerY),
                        new System.Windows.Point(this.ActualWidth, centerY - halfBase),
                        new System.Windows.Point(this.ActualWidth, centerY + halfBase)
                    };
                    TriangleCenterRight.Fill = currentBrush;
                    TriangleCenterRight.Visibility = Visibility.Visible;
                }
            }), null);
        }


        public void StartCalibration(WiiKeyMapper keyMapper)
        {
            if (this.hidden)
            {
                this.hidden = false;
                wasDebugActiveBeforeCalibration = Settings.Default.Debug;
                wasNotificationsActiveBeforeCalibration = Settings.Default.notifications_enabled;

                Settings.Default.Debug = true;
                Settings.Default.notifications_enabled = false;

                this.keyMapper = keyMapper;
                this.keyMapper.SwitchToCalibration();
                this.keyMapper.OnButtonDown += keyMapper_OnButtonDown;
                this.keyMapper.OnButtonUp += keyMapper_OnButtonUp;

                previousForegroundWindow = UIHelpers.GetForegroundWindow();
                if (previousForegroundWindow == null)
                {
                    previousForegroundWindow = IntPtr.Zero;
                }

                Dispatcher.BeginInvoke(new Action(delegate ()
                {
                    this.Activate();

                    Color pointColor = IDColor.getColor(keyMapper.WiimoteID);
                    pointColor.R = (byte)(pointColor.R * 0.8);
                    pointColor.G = (byte)(pointColor.G * 0.8);
                    pointColor.B = (byte)(pointColor.B * 0.8);
                    SolidColorBrush brush = new SolidColorBrush(pointColor);

                    this.wiimoteNo.Text = "Wiimote " + keyMapper.WiimoteID + " ";
                    this.wiimoteNo.Foreground = brush;

                    this.TextBorder.SetValue(Canvas.LeftProperty, 0.5 * this.ActualWidth - (this.TextBorder.ActualWidth / 2));
                    this.TextBorder.SetValue(Canvas.TopProperty, 0.25 * this.ActualHeight - (this.TextBorder.ActualHeight / 2));

                    this.CalibrationCanvas.Opacity = 0.0;
                    this.CalibrationCanvas.Visibility = Visibility.Visible;

                    this.elipse.Stroke = this.lineX.Stroke = this.lineY.Stroke = new SolidColorBrush(Colors.Black);
                    this.elipse.Fill = new SolidColorBrush(Colors.White);
                    this.elipse.Fill.Opacity = 0.9;

                    // Set initial stroke for lines
                    VerticalLineLeft.Stroke = brush;
                    VerticalLineRight.Stroke = brush;
                    HorizontalLineCenter.Stroke = brush; // For diamond mode
                    VerticalLineCenter.Stroke = brush;   // For diamond mode

                    DoubleAnimation animation = UIHelpers.createDoubleAnimation(1.0, 200, false);
                    animation.FillBehavior = FillBehavior.HoldEnd;
                    animation.Completed += delegate (object sender, EventArgs pEvent)
                    {
                        // Animation completed, ready for the first step
                    };
                    this.CalibrationCanvas.BeginAnimation(FrameworkElement.OpacityProperty, animation, HandoffBehavior.SnapshotAndReplace);

                    // Call here to ensure lines and triangles update with correct color and visibility after keyMapper is set
                    UpdateCalibrationLinesAndTrianglesVisibility(brush);
                }), null);

                // --- BACKUP CURRENT VALUES AND PREPARE FOR EACH MODE ---
                
                topBackup = this.keyMapper.settings.Top;
                bottomBackup = this.keyMapper.settings.Bottom;
                leftBackup = this.keyMapper.settings.Left;
                rightBackup = this.keyMapper.settings.Right;
                centerXBackup = this.keyMapper.settings.CenterX;
                centerYBackup = this.keyMapper.settings.CenterY;
                offsetYTopBackup = this.keyMapper.settings.OffsetYTop;
                offsetYBottomBackup = this.keyMapper.settings.OffsetYBottom;
                tlBackup = this.keyMapper.settings.TLled;
                trBackup = this.keyMapper.settings.TRled;

                // Capture backup of margins here, as they are used in "none" and restored in "square" if canceled.
                marginXBackup = Settings.Default.CalibrationMarginX;
                marginYBackup = Settings.Default.CalibrationMarginY;


                // Default values
                this.keyMapper.settings.Top = 0.0f;
                this.keyMapper.settings.Bottom = 1.0f;
                this.keyMapper.settings.Left = 0.0f;
                this.keyMapper.settings.Right = 1.0f;
                this.keyMapper.settings.OffsetYTop = 0.0f;
                this.keyMapper.settings.OffsetYBottom = 1.0f;
                this.keyMapper.settings.CenterX = 0.5f;
                this.keyMapper.settings.CenterY = 0.5f;
                this.keyMapper.settings.TLled = 0.23f;
                this.keyMapper.settings.TRled = 0.77f;
                
                

                Dispatcher.BeginInvoke(new Action(delegate ()
                {
                    this.CalibrationPoint.Visibility = Visibility.Hidden; // Hide target until first press
                    this.wiimoteNo.Text = "Wiimote " + keyMapper.WiimoteID + ":"; // Keep the wiimote ID visible
                    this.insText2.Text = AimTargets; // Display initial generic instruction
                    this.TextBorder.UpdateLayout();
                    this.TextBorder.SetValue(Canvas.LeftProperty, 0.5 * this.ActualWidth - (this.TextBorder.ActualWidth / 2));
                    this.TextBorder.SetValue(Canvas.TopProperty, 0.25 * this.ActualHeight - (this.TextBorder.ActualHeight / 2));
                }), null);

                step = -1; // A new state indicating we are waiting for the first button press.

            }
        }

        void OverlayWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (!this.hidden)
            {
                if (e.Key == Key.Escape)
                {
                    HideOverlay();
                }
            }
        }

        private void HideOverlay()
        {
            if (!this.hidden)
            {
                DebugVisualizer.HideAll();
                this.hidden = true;

                //this.timerElapsed = false;

                this.keyMapper.OnButtonUp -= keyMapper_OnButtonUp;
                this.keyMapper.OnButtonDown -= keyMapper_OnButtonDown;
                this.keyMapper.SwitchToFallback();

                this.currentShotCount = 0;
                this.currentTargetShots.Clear();

                this.keyMapper.OnButtonUp -= keyMapper_OnButtonUp;
                this.keyMapper.OnButtonDown -= keyMapper_OnButtonDown;
                this.keyMapper.SwitchToFallback();

                //buttonTimer.Elapsed -= buttonTimer_Elapsed;

                Dispatcher.BeginInvoke(new Action(delegate ()
                {
                    if (previousForegroundWindow != IntPtr.Zero)
                    {
                        UIHelpers.SetForegroundWindow(previousForegroundWindow);
                    }
                    DoubleAnimation animation = UIHelpers.createDoubleAnimation(0.0, 200, false);
                    animation.FillBehavior = FillBehavior.HoldEnd;
                    animation.Completed += delegate (object sender, EventArgs pEvent)
                    {
                        this.CalibrationCanvas.Visibility = Visibility.Hidden;
                        // Ensure lines and triangles are hidden when overlay is hidden
                        UpdateCalibrationLinesAndTrianglesVisibility();
                    };
                    this.CalibrationCanvas.BeginAnimation(FrameworkElement.OpacityProperty, animation, HandoffBehavior.SnapshotAndReplace);
                }), null);
                step = 0; // Reset step counter when hiding
            }
        }

        private void finishedCalibration()
        {
            
            
            Settings.Default.Debug = wasDebugActiveBeforeCalibration;
            Settings.Default.notifications_enabled = wasNotificationsActiveBeforeCalibration;
            Settings.Default.Save();
            
            // None and diamond modes don't need Settings.Default.Save() here
            // as their relevant properties are saved in WiimoteSettings.SaveCalibrationData()

            this.keyMapper.settings.SaveCalibrationData(); // Saves Wiimote calibration

            this.HideOverlay();
            // Ensure lines and triangles are hidden after calibration finishes
            UpdateCalibrationLinesAndTrianglesVisibility();
        }

        public void CancelCalibration()
        {

            Settings.Default.Debug = wasDebugActiveBeforeCalibration;
            Settings.Default.notifications_enabled = wasNotificationsActiveBeforeCalibration;

            // Restore backup values based on mode
            this.keyMapper.settings.Top = topBackup;
            this.keyMapper.settings.Bottom = bottomBackup;
            this.keyMapper.settings.Left = leftBackup;
            this.keyMapper.settings.Right = rightBackup;

            this.keyMapper.settings.CenterX = centerXBackup;
            this.keyMapper.settings.CenterY = centerYBackup;
            this.keyMapper.settings.TLled = tlBackup;
            this.keyMapper.settings.TRled = trBackup;

            this.keyMapper.settings.OffsetYTop = offsetYTopBackup;
            this.keyMapper.settings.OffsetYBottom = offsetYBottomBackup;

            // Make sure to also restore calibration margins for square mode
            // as they are reset to 0 when calibration starts in square.
            Settings.Default.CalibrationMarginX = marginXBackup;
            Settings.Default.CalibrationMarginY = marginYBackup;

            Settings.Default.Save();


            this.keyMapper.settings.SaveCalibrationData(); // Saves restored values


            this.currentShotCount = 0;
            this.currentTargetShots.Clear();

            this.HideOverlay();
            // Ensure lines and triangles are hidden after calibration is canceled
            UpdateCalibrationLinesAndTrianglesVisibility();
        }

        private void keyMapper_OnButtonUp(WiiButtonEvent e)
        {
            e.Button = e.Button.Replace("OffScreen.", "");
            if (e.Button.ToLower().Equals("a") || e.Button.ToLower().Equals("b"))
            {
                if (step == -1)
                {
                    // Logic moved from StartCalibration to here. This shows the FIRST target.
                    Dispatcher.BeginInvoke(new Action(delegate ()
                        {
                            this.movePoint(0.5, 0.5); // Center
                            this.CalibrationPoint.Visibility = Visibility.Hidden;
                            this.insText2.Text = AimCenter;
                            this.TextBorder.UpdateLayout();
                            this.TextBorder.SetValue(Canvas.LeftProperty, 0.5 * this.ActualWidth - (this.TextBorder.ActualWidth / 2));
                            this.TextBorder.SetValue(Canvas.TopProperty, 0.25 * this.ActualHeight - (this.TextBorder.ActualHeight / 2));
                        }), null);
                        step = 0; // Step 0 for center
                    return;
                }
            }
        }

        private void keyMapper_OnButtonDown(WiiButtonEvent e)
        {
            e.Button = e.Button.Replace("OffScreen.", "");
            // Calibration confirmation or reset logic
            bool isConfirmStep = (step == 5);

            if (isConfirmStep)
            {
                if (e.Button.ToLower().Equals("a"))
                {
                    finishedCalibration();
                }
                else if (e.Button.ToLower().Equals("minus"))
                {
                    // Restart calibration based on mode
                    this.keyMapper.SwitchToCalibration();
                    Dispatcher.BeginInvoke(new Action(delegate ()
                        {
                            this.movePoint(0.5, 0.5); // Return to center
                            this.insText2.Text = AimCenter;
                            this.TextBorder.UpdateLayout();
                            this.TextBorder.SetValue(Canvas.LeftProperty, 0.5 * this.ActualWidth - (this.TextBorder.ActualWidth / 2));
                            this.TextBorder.SetValue(Canvas.TopProperty, 0.25 * this.ActualHeight - (this.TextBorder.ActualHeight / 2));
                        }), null);
                        step = 0;
                    this.currentShotCount = 0;
                    this.currentTargetShots.Clear();
                }
            }
            else if (step >= 0 && (e.Button.ToLower().Equals("a") || e.Button.ToLower().Equals("b")))
            {
                IRState irState = keyMapper.CurrentWiimoteState.IRState;
                AccelState accelState = keyMapper.CurrentWiimoteState.AccelState;

                // Contamos cuántos sensores están activos
                int foundSensors = irState.IRSensors.Count(sensor => sensor.Found);

                if ((Settings.Default.pointer_4IRMode != "none" && foundSensors < 3) || Settings.Default.pointer_4IRMode == "none" && foundSensors <2)
                {
                    // No hay suficientes sensores, mostrar error y no contar el disparo
                    Dispatcher.BeginInvoke(new Action(delegate ()
                    {
                        this.wiimoteNo.Text = null;
                        this.insText2.Text = NoSensors; 

                        this.TextBorder.UpdateLayout();
                        this.TextBorder.SetValue(Canvas.LeftProperty, 0.5 * this.ActualWidth - (this.TextBorder.ActualWidth / 2));
                        this.TextBorder.SetValue(Canvas.TopProperty, 0.25 * this.ActualHeight - (this.TextBorder.ActualHeight / 2));
                    }), null);
                    return; 
                }

                // --- Capturar datos del disparo ---
                double Pitch = Math.Atan2(accelState.Values.Y, accelState.Values.Z);
                const float k_pitchCorrectionFactor = 0.02f;
                float pitchOffsetY = k_pitchCorrectionFactor * (float)Math.Tan(Pitch);

                // Añadir datos del disparo a la lista (incluyendo Width y Height)
                currentTargetShots.Add(new ShotData
                {
                    RelativeX = this.keyMapper.cursorPos.RelativeX,
                    RelativeY = this.keyMapper.cursorPos.RelativeY,
                    PitchOffsetY = pitchOffsetY,
                    Width = this.keyMapper.cursorPos.Width, 
                    Height = this.keyMapper.cursorPos.Height 
                });

                currentShotCount++;

                // --- Comprobar si hemos terminado con este objetivo ---
                if (currentShotCount < SHOTS_PER_TARGET)
                {
                    // Aún se necesitan más disparos para este objetivo
                    Dispatcher.BeginInvoke(new Action(delegate ()
                    {
                        this.wiimoteNo.Text = null;
                        
                        this.insText2.Text = $"¨{shoot} {currentShotCount} {of} {SHOTS_PER_TARGET}";

                        this.TextBorder.UpdateLayout();
                        this.TextBorder.SetValue(Canvas.LeftProperty, 0.5 * this.ActualWidth - (this.TextBorder.ActualWidth / 2));
                        this.TextBorder.SetValue(Canvas.TopProperty, 0.25 * this.ActualHeight - (this.TextBorder.ActualHeight / 2));
                    }), null);
                }
                else
                {
                    
                    // Llamamos al nuevo método para procesar las medias y guardar los parámetros
                    ProcessAverageAndAdvance();

                    // Reiniciamos contadores para el PRÓXIMO objetivo
                    currentShotCount = 0;
                    currentTargetShots.Clear();
                }
            }
            
        }

        private void ProcessAverageAndAdvance()
        {
            // --- 1. CALCULAR PROMEDIOS ---
            if (currentTargetShots.Count == 0) return; // Comprobación de seguridad

            // Usamos LINQ para calcular el promedio de todos los disparos
            double avgRelativeX = currentTargetShots.Average(s => s.RelativeX);
            double avgRelativeY = currentTargetShots.Average(s => s.RelativeY);
            float avgPitchOffsetY = currentTargetShots.Average(s => s.PitchOffsetY);
            double avgWidth = currentTargetShots.Average(s => s.Width); 
            double avgHeight = currentTargetShots.Average(s => s.Height); 


            // --- 2. APLICAR DATOS DE CALIBRACIÓN 

            // Capturamos el ancho/alto en el paso 0 (usando promedios), 
            // ya que se usa en los cálculos del paso 4
            if (step == 0)
            {
                captWidth = (float)avgWidth;  // Usamos el promedio
                captHeight = (float)avgHeight; // Usamos el promedio

                // Guardamos los LEDs raw (si aún es necesario)
                var irState = keyMapper.CurrentWiimoteState.IRState;
                lastCapturedRawLeds = irState.IRSensors
                                     .Where(s => s.Found)
                                     .Select(s => new PointF { X = s.RawPosition.X, Y = s.RawPosition.Y })
                                     .ToList();
            }


            switch (step)
            {
                case 0: // Center Capture 
                    
                    // Usamos los promedios
                    this.keyMapper.settings.CenterX = (float)avgRelativeX;
                    this.keyMapper.settings.CenterY = (float)avgRelativeY - avgPitchOffsetY;

                    Console.WriteLine($"DEBUG: Mostrando Width = {captWidth}");
                    Console.WriteLine($"DEBUG: Mostrando Height = {captHeight}");
                    Console.WriteLine($"DEBUG: Mostrando CenterX = {this.keyMapper.settings.CenterX}");
                    Console.WriteLine($"DEBUG: Mostrando CenterY = {this.keyMapper.settings.CenterY}");
                    
                    break;
                case 1:
                    if (Settings.Default.pointer_4IRMode == "square" || Settings.Default.pointer_4IRMode == "none") //BOTTOM-RIGHT
                    {
                        captRight = 1.0 - avgRelativeX;
                        captBottom = avgRelativeY - avgPitchOffsetY;
                        Console.WriteLine($"DEBUG: Mostrando Right = {captRight}");
                        Console.WriteLine($"DEBUG: Mostrando Bottom = {captBottom}");
                    }
                    else if (Settings.Default.pointer_4IRMode == "diamond") // TOP
                    {
                        captTop = avgRelativeY - avgPitchOffsetY;
                    }
                    break;
                case 2:
                    if (Settings.Default.pointer_4IRMode == "square" || Settings.Default.pointer_4IRMode == "none") //TOP-LEFT
                    {
                        captLeft = 1.0 - avgRelativeX;
                        captTop = avgRelativeY - avgPitchOffsetY;
                    }
                    else if (Settings.Default.pointer_4IRMode == "diamond") //BOTTOM
                    {
                        captBottom = avgRelativeY - avgPitchOffsetY;
                    }
                    break;
                case 3:
                    if (Settings.Default.pointer_4IRMode == "diamond") // LEFT
                    {
                        captLeft = 1.0 - avgRelativeX;
                    }
                    else if (Settings.Default.pointer_4IRMode == "square" || Settings.Default.pointer_4IRMode == "none") //TOP-RIGHT
                    {
                        // La lógica de promediar los promedios se mantiene
                        captRight = ((captRight + (1.0 - avgRelativeX))) / 2;
                        captTop = ((captTop + avgRelativeY - avgPitchOffsetY)) / 2;
                    }
                    break;
                case 4:
                    if (Settings.Default.pointer_4IRMode == "diamond") // RIGHT
                    {
                        captRight = 1.0 - avgRelativeX;
                    }
                    else if (Settings.Default.pointer_4IRMode == "square" || Settings.Default.pointer_4IRMode == "none") //Bottom-Left
                    {
                        captLeft = ((captLeft + (1.0 - avgRelativeX))) / 2;
                        captBottom = ((captBottom + avgRelativeY - avgPitchOffsetY)) / 2;
                    }

                    // --- Cálculos finales (esta lógica es idéntica a la anterior, pero usa valores promediados) ---
                    double scaleX = ((1.0 - marginXBackup) - marginXBackup) / (captRight - captLeft);
                    double scaleY = ((1.0 - marginYBackup) - marginYBackup) / (captBottom - captTop);

                    double NormalizedScreenWidth = captWidth * Math.Abs(scaleX);
                    double NormalizedScreenHeight = captHeight * Math.Abs(scaleY);

                    var TLled = this.keyMapper.settings.CenterX - (NormalizedScreenWidth / 2.0);
                    var TRled = this.keyMapper.settings.CenterX + (NormalizedScreenWidth / 2.0);
                    var OffsetYTop = this.keyMapper.settings.CenterY - (NormalizedScreenHeight / 2.0);
                    var OffsetYBottom = this.keyMapper.settings.CenterY + (NormalizedScreenHeight / 2.0);

                    // ... (todos los Console.WriteLine de debug) ...
                    Console.WriteLine($"DEBUG: Ancho Normalizado en Pantalla = {NormalizedScreenWidth}");
                    Console.WriteLine($"DEBUG: Alto Normalizado en Pantalla = {NormalizedScreenHeight}");
                    Console.WriteLine($"DEBUG: Escala X Calculada = {scaleX}");
                    Console.WriteLine($"DEBUG: Escala Y Calculada = {scaleY}");
                    Console.WriteLine($"DEBUG: Mostrando Left = {captLeft}");
                    Console.WriteLine($"DEBUG: Mostrando Top = {captTop}");

                    Console.WriteLine($"DEBUG: Mostrando TLLed = {TLled}");
                    Console.WriteLine($"DEBUG: Mostrando TRLed = {TRled}");

                    Console.WriteLine($"DEBUG: Mostrando OffsetYTop = {OffsetYTop}");
                    Console.WriteLine($"DEBUG: Mostrando OffsetYBottom = {OffsetYBottom}");
                    if (Settings.Default.pointer_4IRMode == "diamond") // RIGHT
                    {
                        this.keyMapper.settings.Top = (float)OffsetYTop;
                        this.keyMapper.settings.Bottom = (float)OffsetYBottom;
                        this.keyMapper.settings.Left = (float)TLled;
                        this.keyMapper.settings.Right = (float)TRled;
                    }
                    else if (Settings.Default.pointer_4IRMode == "square")
                    {
                        this.keyMapper.settings.TLled = (float)TLled;
                        this.keyMapper.settings.TRled = (float)TRled;
                        this.keyMapper.settings.OffsetYTop = (float)OffsetYTop;
                        this.keyMapper.settings.OffsetYBottom = (float)OffsetYBottom;
                    }
                    else if (Settings.Default.pointer_4IRMode == "none")
                    {
                        this.keyMapper.settings.TLled = (float)(this.keyMapper.settings.CenterX - (captWidth / 2.0));
                        this.keyMapper.settings.TRled = (float)(this.keyMapper.settings.CenterX + (captWidth / 2.0));
                        this.keyMapper.settings.OffsetYTop = (float)OffsetYTop;
                        this.keyMapper.settings.OffsetYBottom = (float)OffsetYBottom;
                        this.keyMapper.settings.Top = (float)captTop;
                        this.keyMapper.settings.Bottom = (float)captBottom;
                        this.keyMapper.settings.Left = 1.0f - (float)captLeft;
                        this.keyMapper.settings.Right = 1.0f - (float)captRight;
                        //this.keyMapper.settings.CenterX = 0.5f;
                        //this.keyMapper.settings.CenterY = 0.5f;
                    }
                    this.keyMapper.loadKeyMap("Calibration_Preview.json");
                    break;
                default: break;
            }

            // --- 3. AVANZAR AL SIGUIENTE PASO 
            // Una vez procesados los datos, movemos el objetivo y cambiamos el paso
            switch (step)
            {
                case 0: // Acabamos de procesar el Centro
                    if (Settings.Default.pointer_4IRMode == "square" || Settings.Default.pointer_4IRMode == "none")
                    {
                        Dispatcher.BeginInvoke(new Action(delegate ()
                        {
                            this.movePoint(1 - marginXBackup, 1 - marginYBackup); // Bottom Right Corner
                            this.CalibrationPoint.Visibility = Visibility.Visible;
                            this.insText2.Text = AimBottomRight;
                            this.TextBorder.UpdateLayout();
                            this.TextBorder.SetValue(Canvas.LeftProperty, 0.5 * this.ActualWidth - (this.TextBorder.ActualWidth / 2));
                            this.TextBorder.SetValue(Canvas.TopProperty, 0.25 * this.ActualHeight - (this.TextBorder.ActualHeight / 2));
                        }), null);
                        step = 1;
                    }
                    else if (Settings.Default.pointer_4IRMode == "diamond")
                    {
                        Dispatcher.BeginInvoke(new Action(delegate ()
                        {
                            this.movePoint(0.5, marginYBackup); // Top-Center
                            this.CalibrationPoint.Visibility = Visibility.Visible;
                            this.insText2.Text = AimTopCenter;
                            this.TextBorder.UpdateLayout();
                            this.TextBorder.SetValue(Canvas.LeftProperty, 0.5 * this.ActualWidth - (this.TextBorder.ActualWidth / 2));
                            this.TextBorder.SetValue(Canvas.TopProperty, 0.25 * this.ActualHeight - (this.TextBorder.ActualHeight / 2));
                        }), null);
                        step = 1;
                    }
                    break;
                case 1: // Acabamos de procesar BR o Top-Center
                    if (Settings.Default.pointer_4IRMode == "square" || Settings.Default.pointer_4IRMode == "none")
                    {
                        Dispatcher.BeginInvoke(new Action(delegate ()
                        {
                            this.movePoint(marginXBackup, marginYBackup);
                            this.CalibrationPoint.Visibility = Visibility.Visible;
                            this.insText2.Text = AimTopLeft;
                            this.TextBorder.UpdateLayout();
                            this.TextBorder.SetValue(Canvas.LeftProperty, 0.5 * this.ActualWidth - (this.TextBorder.ActualWidth / 2));
                            this.TextBorder.SetValue(Canvas.TopProperty, 0.25 * this.ActualHeight - (this.TextBorder.ActualHeight / 2));
                        }), null);
                        step = 2;
                    }
                    else if (Settings.Default.pointer_4IRMode == "diamond")
                    {
                        Dispatcher.BeginInvoke(new Action(delegate ()
                        {
                            this.movePoint(0.5, 1 - marginYBackup); // Bottom-Center
                            this.CalibrationPoint.Visibility = Visibility.Visible;
                            this.insText2.Text = AimBottomCenter;
                            this.TextBorder.UpdateLayout();
                            this.TextBorder.SetValue(Canvas.LeftProperty, 0.5 * this.ActualWidth - (this.TextBorder.ActualWidth / 2));
                            this.TextBorder.SetValue(Canvas.TopProperty, 0.25 * this.ActualHeight - (this.TextBorder.ActualHeight / 2));
                        }), null);
                        step = 2;
                    }
                    break;
                case 2: // Acabamos de procesar TL o Bottom-Center
                    
                    if (Settings.Default.pointer_4IRMode == "square" || Settings.Default.pointer_4IRMode == "none")
                    {
                        Dispatcher.BeginInvoke(new Action(delegate ()
                        {
                            this.movePoint(1 - marginXBackup, marginYBackup); // Top-Right
                            this.CalibrationPoint.Visibility = Visibility.Visible;
                            this.insText2.Text = AimTopRight;
                            this.TextBorder.UpdateLayout();
                            this.TextBorder.SetValue(Canvas.LeftProperty, 0.5 * this.ActualWidth - (this.TextBorder.ActualWidth / 2));
                            this.TextBorder.SetValue(Canvas.TopProperty, 0.25 * this.ActualHeight - (this.TextBorder.ActualHeight / 2));
                        }), null);
                        step = 3;
                    }
                    else if (Settings.Default.pointer_4IRMode == "diamond")
                    {
                        Dispatcher.BeginInvoke(new Action(delegate ()
                        {
                            this.movePoint(marginXBackup, 0.5); // Left-Center
                            this.CalibrationPoint.Visibility = Visibility.Visible;
                            this.insText2.Text = AimLeftCenter;
                            this.TextBorder.UpdateLayout();
                            this.TextBorder.SetValue(Canvas.LeftProperty, 0.5 * this.ActualWidth - (this.TextBorder.ActualWidth / 2));
                            this.TextBorder.SetValue(Canvas.TopProperty, 0.25 * this.ActualHeight - (this.TextBorder.ActualHeight / 2));
                        }), null);
                        step = 3;
                    }
                    break;
                case 3: // Acabamos de procesar TR o Left-Center
                    if (Settings.Default.pointer_4IRMode == "diamond")
                    {
                        Dispatcher.BeginInvoke(new Action(delegate ()
                        {
                            this.movePoint(1 - marginXBackup, 0.5); // Right-Center
                            this.CalibrationPoint.Visibility = Visibility.Visible;
                            this.insText2.Text = AimRightCenter;
                            this.TextBorder.UpdateLayout();
                            this.TextBorder.SetValue(Canvas.LeftProperty, 0.5 * this.ActualWidth - (this.TextBorder.ActualWidth / 2));
                            this.TextBorder.SetValue(Canvas.TopProperty, 0.25 * this.ActualHeight - (this.TextBorder.ActualHeight / 2));
                        }), null);
                        step = 4;
                    }
                    else if (Settings.Default.pointer_4IRMode == "square" || Settings.Default.pointer_4IRMode == "none")
                    {
                        Dispatcher.BeginInvoke(new Action(delegate ()
                        {
                            this.movePoint(marginXBackup, 1 - marginYBackup); // Bottom-Left
                            this.CalibrationPoint.Visibility = Visibility.Visible;
                            this.insText2.Text = AimBottomLeft;
                            this.TextBorder.UpdateLayout();
                            this.TextBorder.SetValue(Canvas.LeftProperty, 0.5 * this.ActualWidth - (this.TextBorder.ActualWidth / 2));
                            this.TextBorder.SetValue(Canvas.TopProperty, 0.25 * this.ActualHeight - (this.TextBorder.ActualHeight / 2));
                        }), null);
                        step = 4;
                    }
                    break;
                case 4: // Acabamos de procesar BL o Right-Center (ÚLTIMO PUNTO)
                        Dispatcher.BeginInvoke(new Action(delegate ()
                        {
                            this.CalibrationPoint.Visibility = Visibility.Hidden; // Hide target
                            this.wiimoteNo.Text = null;
                            this.insText2.Text = AimConfirm;
                            this.TextBorder.UpdateLayout();
                            this.TextBorder.SetValue(Canvas.LeftProperty, 0.5 * this.ActualWidth - (this.TextBorder.ActualWidth / 2));
                            this.TextBorder.SetValue(Canvas.TopProperty, 0.25 * this.ActualHeight - (this.TextBorder.ActualHeight / 2));
                        }), null);
                        step = 5; // Go directly to unified confirmation step
                    break;
                default: break;
            }
        }
        private System.Windows.Point movePoint(double fNormalX, double fNormalY)
        {
            System.Windows.Point tPoint = new System.Windows.Point(fNormalX * this.ActualWidth, fNormalY * this.ActualHeight);

            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                this.CalibrationPoint.Visibility = Visibility.Visible;

                this.CalibrationPoint.SetValue(Canvas.LeftProperty, tPoint.X - (this.CalibrationPoint.ActualWidth / 2));
                this.CalibrationPoint.SetValue(Canvas.TopProperty, tPoint.Y - (this.CalibrationPoint.ActualHeight / 2));

            }), null);

            return tPoint;
        }

        public bool OverlayIsOn()
        {
            return !this.hidden;
        }


    }
}
