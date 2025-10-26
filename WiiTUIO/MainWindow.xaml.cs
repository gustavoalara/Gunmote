using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Threading;
using System.Threading.Tasks;

using WiiTUIO.Provider;
using WiiTUIO.Properties;
using System.Windows.Input;
using WiiTUIO.Output;
using Microsoft.Win32;
using System.Diagnostics; // Needed for Process.Start
using Newtonsoft.Json;
using MahApps.Metro.Controls;
using System.Windows.Interop;
using System.Net; // Needed for HttpWebRequest
using Newtonsoft.Json.Linq; // Needed for JObject
using WiiTUIO.DeviceUtils;
using WiiCPP;
using WiiTUIO.Output.Handlers.Xinput;
using WiiTUIO.ArcadeHook;
using System.IO.Pipes;
using System.Globalization;
using System.Reflection; // Needed for Assembly.GetExecutingAssembly
using MahApps.Metro.Controls.Dialogs;

using static WiiTUIO.Resources.Resources; // Importa la clase Resources para acceso directo a las cadenas

namespace WiiTUIO
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, WiiCPP.WiiPairListener
    {
        // Define aquí la URL de la API de releases de tu repositorio de GitHub
        private const string GitHubApiReleasesUrl = "https://api.github.com/repos/gustavoalara/Gunmote/releases/latest";
        private const string GitHubReleasesPageUrl = "https://github.com/gustavoalara/Gunmote/releases";
        
        private LanguageSelectorUC languagePanel;

        private bool wiiPairRunning = false;

        private bool minimizedOnce = false;

        private Thread wiiPairThread;

        private bool providerHandlerConnected = false;

        private bool tryingToConnect = false;

        private bool startupPair = false;

        private ArcadeHookMain arcadeHook;

        private Thread arcadeHookThread;

        private Mutex statusStackMutex = new Mutex();

        private SystemProcessMonitor processMonitor;

        private CommandListener commandListener;

        private IntPtr previousForegroundWindow = IntPtr.Zero;

        /// <summary>
        /// A reference to the WiiProvider we want to use to get/forward input.
        /// </summary>
        private IProvider pWiiProvider = null;

        WiiCPP.WiiPair wiiPair = null;

        System.Windows.Threading.Dispatcher overlayDispatcher = null;
        Thread overlayUIThread = null;

        /// <summary>
        /// Boolean to tell if we are connected to the mote and network.
        /// </summary>
        private bool bConnected = false;

        private static MainWindow defaultInstance;

        public static MainWindow Current
        {
            get
            {
                return defaultInstance;
            }
        }

        /// <summary>
        /// Construct a new Window.
        /// </summary>
        public MainWindow()
        {
            // Localization

            if (!string.IsNullOrEmpty(Settings.Default.DefaultLanguage))
            {
                try
                {
                    // Aplica el idioma guardado a la cultura del hilo de la UI.
                    var culture = new CultureInfo(Settings.Default.DefaultLanguage);
                    Thread.CurrentThread.CurrentUICulture = culture;
                    Thread.CurrentThread.CurrentCulture = culture;
                }
                catch (CultureNotFoundException)
                {
                    // Si el idioma guardado es inválido por alguna razón,
                    // simplemente continúa con el idioma por defecto del sistema.
                    Settings.Default.DefaultLanguage = "";
                    Settings.Default.Save();
                }
            }



            //Set highest priority on main process.
            Process currentProcess = Process.GetCurrentProcess();
            currentProcess.PriorityClass = ProcessPriorityClass.High;
            Thread.CurrentThread.Priority = ThreadPriority.Normal;

            if (Settings.Default.minimizeOnStart)
            {
                this.ShowActivated = false;
                this.WindowState = System.Windows.WindowState.Minimized;
            }

            //Settings.Default.primaryMonitor = "";

            defaultInstance = this;

            // Load from the XAML.
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            KeymapDatabase.Current.CreateDefaultFiles();

            base.OnInitialized(e);

            KeymapConfigWindow.Instance.Visibility = System.Windows.Visibility.Collapsed;

            this.mainPanel.Visibility = Visibility.Visible;
            this.canvasSettings.Visibility = Visibility.Collapsed;
            this.canvasAbout.Visibility = Visibility.Collapsed;
            this.spPairing.Visibility = Visibility.Collapsed;
            this.tbPair2.Visibility = Visibility.Visible;
            this.tbPairDone.Visibility = Visibility.Collapsed;
            this.spErrorMsg.Visibility = Visibility.Collapsed;
            this.spInfoMsg.Visibility = Visibility.Collapsed;
            this.canvasLanguage.Visibility = Visibility.Collapsed;
            this.animateExpand(this.mainPanel);

            overlayUIThread = new Thread(() =>
            {
                previousForegroundWindow = UIHelpers.GetForegroundWindow();
                OverlayWindow.Current.Show();
                CalibrationOverlay.Current.Show();

                System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new Action(delegate ()
                {
                    D3DCursorWindow.Current.Start((new WindowInteropHelper(OverlayWindow.Current)).Handle);
                }));

                if (previousForegroundWindow != IntPtr.Zero && Settings.Default.minimizeOnStart)
                    UIHelpers.SetForegroundWindow(previousForegroundWindow);

                // Grab dispatcher for current thread
                overlayDispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                System.Windows.Threading.Dispatcher.Run();
                Console.WriteLine("Overlay UI Thread Ended");
            });
            overlayUIThread.SetApartmentState(ApartmentState.STA);
            overlayUIThread.IsBackground = true;
            overlayUIThread.Priority = ThreadPriority.Highest;
            overlayUIThread.Start();

            Application.Current.Exit += appWillExit;
            Application.Current.SessionEnding += windowsShutdownEvent;

            wiiPair = new WiiCPP.WiiPair();
            wiiPair.addListener(this);

            Settings.Default.PropertyChanged += Settings_PropertyChanged;

            // Create the providers.
            this.createProvider();
            //this.createProviderHandler();

            if (Settings.Default.pairOnStart)
            {
                this.startupPair = true;
                this.runWiiPair();
            }
            else //if (Settings.Default.connectOnStart)
            {
                this.connectProvider();
            }

            AppSettingsUC settingspanel = new AppSettingsUC();
            settingspanel.OnClose += SettingsPanel_OnClose;

            this.canvasSettings.Children.Add(settingspanel);

            AboutUC aboutpanel = new AboutUC();
            aboutpanel.OnClose += AboutPanel_OnClose;

            this.canvasAbout.Children.Add(aboutpanel);

            languagePanel = new LanguageSelectorUC();
            languagePanel.OnClose += LanguagePanel_OnClose;
            this.canvasLanguage.Children.Add(languagePanel);

            Loaded += MainWindow_Loaded;

            checkNewVersion();

            if (Settings.Default.disconnectWiimotesOnDolphin)
            {
                this.processMonitor = SystemProcessMonitor.Default;
                this.processMonitor.ProcessChanged += processChanged;
                this.processMonitor.Start();
            }

            StartArcadeHook();

            this.commandListener = CommandListener.Default;

            AudioUtil.IsValid("sound1");
            AudioUtil.IsValid("sound2");
        }

        private void processChanged(ProcessChangedEvent obj)
        {
            if ((Settings.Default.dolphin_path == "" && obj.Process.ProcessName == "Dolphin"))
            {
                Console.WriteLine("Dolphin detected. Disconnecting provider. Hiding overlay window.");
                this.disconnectDolphin();
                D3DCursorWindow.Current.RefreshCursors();
            }
            else if (obj.Process.ProcessName == "Dolphin" && (Settings.Default.dolphin_path.IndexOfAny(Path.GetInvalidPathChars()) == -1))
            {
                if (obj.Process.MainModule?.FileName == Path.GetFullPath(Settings.Default.dolphin_path))
                {
                    Console.WriteLine("Dolphin detected. Disconnecting provider. Hiding overlay window.");
                    this.disconnectDolphin();
                    D3DCursorWindow.Current.RefreshCursors();
                }
            }
            else
            {
                this.connectProvider();
            }
        }

        private HttpWebRequest wrGETURL;

        /// <summary>
        /// Checks for a new version of the application by querying the GitHub API.
        /// </summary>
        private void checkNewVersion()
        {
            try
            {
                // Get the current application version
                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                // Or if the version is in Settings.Default.AppVersion:
                // Version currentVersion = new Version(Settings.Default.AppVersion);

                // Construct the GitHub API URL to get the latest release
                // We don't need to pass the current version in the URL for GitHub API /latest
                string sURL = GitHubApiReleasesUrl;

                wrGETURL = (HttpWebRequest)WebRequest.Create(sURL);
                wrGETURL.Method = "GET";
                wrGETURL.UserAgent = "GunmoteAppUpdater"; // GitHub API requires a User-Agent
                wrGETURL.Accept = "application/vnd.github.v3+json"; // Optional, but good practice

                wrGETURL.BeginGetResponse(new AsyncCallback(checkNewVersionResponse), currentVersion);
            }
            catch (Exception e)
            {
                // Error handling: log or display a message if the check fails
                ShowMessage(string.Format(UpdateCheck_InitialCheckError, e.Message), MessageType.Error);
            }
        }

        /// <summary>
        /// Handles the response from the GitHub API for version checking.
        /// </summary>
        /// <param name="result">The asynchronous result.</param>
        private void checkNewVersionResponse(IAsyncResult result)
        {
            Version currentVersion = result.AsyncState as Version; // Retrieve the current version

            try
            {
                HttpWebResponse response = (HttpWebResponse)wrGETURL.EndGetResponse(result);
                Stream objStream = response.GetResponseStream();
                StreamReader objReader = new StreamReader(objStream);

                string jsonResponse = objReader.ReadToEnd();

                // Use JObject to parse the JSON response from GitHub
                JObject githubRelease = JObject.Parse(jsonResponse);

                // Get the latest version tag (tag_name)
                string latestVersionTag = githubRelease.Value<string>("tag_name");
                // The tag_name often includes a 'v' prefix, e.g., 'v1.2.3'.
                // We need to clean it to compare numeric versions.
                if (latestVersionTag != null && latestVersionTag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                {
                    latestVersionTag = latestVersionTag.Substring(1); // Remove the initial 'v'
                }

                // Get the assets array to find the executable
                JArray assets = githubRelease.Value<JArray>("assets");
                string installerDownloadUrl = null;

                if (assets != null)
                {
                    // Look for an asset with a common executable extension
                    // You might need to refine this based on your actual release asset names
                    foreach (JObject asset in assets)
                    {
                        string assetName = asset.Value<string>("name");
                        string downloadUrl = asset.Value<string>("browser_download_url");

                        if (assetName != null && downloadUrl != null &&
                            (assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                             assetName.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))) // Consider .msi if you use installers
                        {
                            installerDownloadUrl = downloadUrl;
                            break; // Found the first executable, take it
                        }
                    }
                }

                Version latestVersion = new Version(latestVersionTag);

                // Compare versions
                if (latestVersion > currentVersion)
                {
                    Dispatcher.Invoke(() => {
                        MessageBoxResult dialogResult = MessageBox.Show(
                            string.Format(UpdateCheck_NewVersionAvailable_Message, latestVersion.ToString()),
                            UpdateCheck_NewVersionAvailable_Title,
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information
                        );

                        if (dialogResult == MessageBoxResult.Yes)
                        {
                            if (!string.IsNullOrEmpty(installerDownloadUrl))
                            {
                                ShowMessage(string.Format(UpdateCheck_DownloadingUpdate, Path.GetFileName(installerDownloadUrl)), MessageType.Info);
                                // Show download progress UI elements
                                if (downloadProgressPanel != null)
                                {
                                    downloadProgressPanel.Visibility = Visibility.Visible;
                                }

                                // Start download on a new thread to avoid blocking UI
                                Task.Run(() => DownloadAndInstallUpdate(installerDownloadUrl));
                            }
                            else
                            {
                                ShowMessage(UpdateCheck_NoInstallerFound, MessageType.Error);
                                // Fallback to opening the releases page if no installer URL is found
                                Process.Start(new ProcessStartInfo(GitHubReleasesPageUrl) { UseShellExecute = true });
                            }
                        }
                    });
                }
                else
                {
                    // La versión actual es la más reciente o superior
                    ShowMessage(UpdateCheck_LatestVersion, MessageType.Info);
                }
            }
            catch (WebException wex)
            {
                // Specific handling for network errors (e.g., 404 if the repo doesn't exist, or no connection)
                if (wex.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)wex.Response)
                    {
                        using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            string errorText = reader.ReadToEnd();
                            ShowMessage(string.Format(UpdateCheck_ApiError, errorResponse.StatusCode, errorText), MessageType.Error);
                        }
                    }
                }
                else
                {
                    ShowMessage(string.Format(UpdateCheck_ConnectionError, wex.Message), MessageType.Error);
                }
            }
            catch (JsonReaderException jrex)
            {
                ShowMessage(string.Format(UpdateCheck_JsonError, jrex.Message), MessageType.Error);
            }
            catch (Exception e)
            {
                // Handling of other unexpected errors
                ShowMessage(string.Format(UpdateCheck_UnexpectedError, e.Message), MessageType.Error);
            }
        }

        /// <summary>
        /// Downloads the installer and launches it.
        /// </summary>
        /// <param name="downloadUrl">The URL of the installer to download.</param>
        private void DownloadAndInstallUpdate(string downloadUrl)
        {
            try
            {
                string fileName = Path.GetFileName(downloadUrl);
                string downloadPath = Path.Combine(Path.GetTempPath(), fileName); // Download to temp directory

                using (WebClient client = new WebClient())
                {
                    // Add User-Agent header for GitHub downloads
                    client.Headers.Add("User-Agent", "GunmoteAppUpdater");

                    // Hook up progress and completion events
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressChanged);
                    client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(DownloadFileCompleted);

                    // Start the asynchronous download
                    client.DownloadFileAsync(new Uri(downloadUrl), downloadPath, downloadPath); // UserState will be downloadPath
                }
            }
            catch (Exception e)
            {
                ShowMessage(string.Format(UpdateCheck_DownloadError, e.Message), MessageType.Error);
                // Hide download progress UI elements in case of immediate error
                Dispatcher.Invoke(() => {
                    if (downloadProgressPanel != null)
                    {
                        downloadProgressPanel.Visibility = Visibility.Collapsed;
                    }
                });
            }
        }

        /// <summary>
        /// Handles the progress of the file download.
        /// </summary>
        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // Update your ProgressBar
                if (downloadProgressBar != null)
                {
                    downloadProgressBar.Value = e.ProgressPercentage;
                }
                if (downloadProgressText != null)
                {
                    downloadProgressText.Text = $"{e.ProgressPercentage}%";
                }
                ShowMessage($"Descargando actualizador: {e.ProgressPercentage}%", MessageType.Info);
            });
        }

        /// <summary>
        /// Handles the completion of the file download.
        /// </summary>
        private void DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // Hide download progress UI elements
                if (downloadProgressPanel != null)
                {
                    downloadProgressPanel.Visibility = Visibility.Collapsed;
                }
            });

            if (e.Cancelled)
            {
                ShowMessage(UpdateCheck_DownloadCancelled, MessageType.Warning);
                return;
            }

            if (e.Error != null)
            {
                ShowMessage(string.Format(UpdateCheck_DownloadError, e.Error.Message), MessageType.Error);
                return;
            }

            // The userState contains the downloadPath as set in DownloadFileAsync
            string downloadPath = e.UserState as string;

            if (string.IsNullOrEmpty(downloadPath))
            {
                ShowMessage(UpdateCheck_InstallationError + " (ruta de descarga no encontrada)", MessageType.Error);
                return;
            }

            try
            {
                // Show confirmation dialog before closing and launching installer
                Dispatcher.Invoke(() =>
                {
                    MessageBoxResult dialogResult = MessageBox.Show(
                        UpdateCheck_ConfirmInstallationMessage, // New localized string
                        UpdateCheck_ConfirmInstallationTitle, // New localized string
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (dialogResult == MessageBoxResult.Yes)
                    {
                        ShowMessage(string.Format(UpdateCheck_DownloadComplete, downloadPath), MessageType.Info);

                        // Launch the installer
                        Process.Start(new ProcessStartInfo(downloadPath) { UseShellExecute = true });

                        // Close the current application on the UI thread
                        Application.Current.Shutdown();
                    }
                    else
                    {
                        ShowMessage(UpdateCheck_InstallationCancelled, MessageType.Info); // New localized string
                    }
                });
            }
            catch (Exception ex)
            {
                ShowMessage(string.Format(UpdateCheck_InstallationError, ex.Message), MessageType.Error);
            }
        }


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.minimizeToTray)
            {
                MinimizeToTray.Enable(this, Settings.Default.minimizeOnStart);
            }

            KeymapConfigWindow.Instance.Owner = this;
        }

        private void windowsShutdownEvent(object sender, SessionEndingCancelEventArgs e)
        {
            Settings.Default.Save();
        }

        private void AboutPanel_OnClose()
        {
            this.showMain();
        }

        private void SettingsPanel_OnClose()
        {
            Settings.Default.Save();
            this.showMain();
        }

        void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

            if (e.PropertyName == "minimizeToTray")
            {
                if (Settings.Default.minimizeToTray)
                {
                    MinimizeToTray.Enable(this, false);
                }
                else
                {
                    MinimizeToTray.Disable(this);
                }
            }

        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

        }

        private void appWillExit(object sender, ExitEventArgs e)
        {
            if (overlayDispatcher != null)
            {
                overlayDispatcher.InvokeShutdown();
                overlayDispatcher = null;
            }

            overlayUIThread.Join();
            overlayUIThread = null;

            this.stopWiiPair();
            this.disconnectProvider();
            this.StopArcadeHook();

            ViGEmBusClientSingleton.Disconnect();
            //this.disconnectProviderHandler();
        }


        /// <summary>
        /// This is called when the wii remote is connected
        /// </summary>
        /// <param name="obj"></param>
        private void pWiiProvider_OnConnect(int ID, int totalWiimotes)
        {
            // Dispatch it.
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                this.bConnected = true;


                if (totalWiimotes == 1)
                {
                    this.connectedCount.Content = OneWiimoteConnected;
                }
                else
                {
                    this.connectedCount.Content = totalWiimotes + WiimotesConnected;
                }
                statusStackMutex.WaitOne();
                WiimoteStatusUC uc = new WiimoteStatusUC(ID);
                uc.Visibility = Visibility.Collapsed;
                this.statusStack.Children.Add(uc);
                this.animateExpand(uc);
                statusStackMutex.ReleaseMutex();

                //connectProviderHandler();

            }), null);


        }

        /// <summary>
        /// This is called when the wii remote is disconnected
        /// </summary>
        /// <param name="obj"></param>
        private void pWiiProvider_OnDisconnect(int ID, int totalWiimotes)
        {
            // Dispatch it.
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                if (totalWiimotes == 1)
                {
                    this.connectedCount.Content = OneWiimoteConnected;
                }
                else
                {
                    this.connectedCount.Content = totalWiimotes + WiimotesConnected;
                }
                statusStackMutex.WaitOne();
                foreach (UIElement child in this.statusStack.Children)
                {
                    WiimoteStatusUC uc = (WiimoteStatusUC)child;
                    if (uc.ID == ID)
                    {
                        this.animateCollapse(uc, true);
                        //this.statusStack.Children.Remove(child);
                        break;
                    }
                }
                statusStackMutex.ReleaseMutex();
                if (totalWiimotes == 0)
                {
                    this.bConnected = false;

                    //disconnectProviderHandler();
                }

            }), null);
        }


        private Mutex pCommunicationMutex = new Mutex();

        /// <summary>
        /// This is called when the battery state changes.
        /// </summary>
        /// <param name="obj"></param>
        private void pWiiProvider_OnStatusUpdate(WiimoteStatus status)
        {
            // Dispatch it.
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                statusStackMutex.WaitOne();
                foreach (UIElement child in this.statusStack.Children)
                {
                    WiimoteStatusUC uc = (WiimoteStatusUC)child;
                    if (uc.ID == status.ID)
                    {
                        uc.updateStatus(status);
                    }
                }
                statusStackMutex.ReleaseMutex();
            }), null);
        }


        #region Messages - Err/Inf

        public enum MessageType { Info, Warning, Error }; // Added Warning type

        /// <summary>
        /// Displays a message to the user.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="eType">The type of message (Info, Warning, Error).</param>
        public void ShowMessage(string message, MessageType eType)
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                MessageBoxImage icon = MessageBoxImage.Information;
                switch (eType)
                {
                    case MessageType.Error:
                        icon = MessageBoxImage.Error;
                        this.tbErrorMsg.Text = message;
                        this.animateExpand(this.spErrorMsg);
                        break;
                    case MessageType.Info:
                        icon = MessageBoxImage.Information;
                        this.tbInfoMsg.Text = message;
                        this.animateExpand(this.spInfoMsg);
                        break;
                    case MessageType.Warning: // Handle Warning type
                        icon = MessageBoxImage.Warning;
                        this.tbInfoMsg.Text = message; // Or a separate warning message box
                        this.animateExpand(this.spInfoMsg);
                        break;
                }
                // If you want to use a standard MessageBox for all messages, uncomment this:
                // MessageBox.Show(message, "Gunmote", MessageBoxButton.OK, icon);
            }), null);
        }

        #endregion


        private void animateExpand(FrameworkElement elem)
        {
            UIHelpers.animateExpand(elem);
        }

        private void animateCollapse(FrameworkElement elem, bool remove)
        {
            UIHelpers.animateCollapse(elem, remove);
        }

        private void showConfig()
        {
            if (this.mainPanel.IsVisible)
            {
                animateCollapse(this.mainPanel, false);
            }
            if (this.canvasAbout.IsVisible)
            {
                animateCollapse(this.canvasAbout, false);
            }
            if (!this.canvasSettings.IsVisible)
            {
                animateExpand(this.canvasSettings);
            }
            //this.mainPanel.Visibility = Visibility.Collapsed;
            //this.canvasAbout.Visibility = Visibility.Collapsed;
            //this.canvasSettings.Visibility = Visibility.Visible;
        }

        private void showMain()
        {
            if (this.canvasSettings.IsVisible)
            {
                animateCollapse(this.canvasSettings, false);
            }
            if (this.canvasAbout.IsVisible)
            {
                animateCollapse(this.canvasAbout, false);
            }
            if (!this.mainPanel.IsVisible)
            {
                animateExpand(this.mainPanel);
            }
            if (this.canvasLanguage.IsVisible)
            {
                animateCollapse(this.canvasLanguage, false);
            }
            //this.canvasSettings.Visibility = Visibility.Collapsed;
            //this.canvasAbout.Visibility = Visibility.Collapsed;
            //this.mainPanel.Visibility = Visibility.Visible;
        }

        private void showAbout()
        {
            if (this.canvasSettings.IsVisible)
            {
                animateCollapse(this.canvasSettings, false);
            }
            if (this.mainPanel.IsVisible)
            {
                animateCollapse(this.mainPanel, false);
            }
            if (!this.canvasAbout.IsVisible)
            {
                animateExpand(this.canvasAbout);
            }
            //this.mainPanel.Visibility = Visibility.Collapsed;
            //this.canvasAbout.Visibility = Visibility.Visible;
            //this.canvasSettings.Visibility = Visibility.Collapsed;
        }

        #region WiiProvider
        /// <summary>
        /// Try to create the WiiProvider (this involves connecting to the Wiimote).
        /// </summary>
        private void connectProvider()
        {
            if (!this.tryingToConnect)
            {
                Launcher.Launch("Driver", "devcon", " enable \"BTHENUM*_VID*57e*_PID&0306*\"", new Action(delegate ()
                {
                    Launcher.Launch("Driver", "devcon", " enable \"BTHENUM*_VID*57e*_PID&0330*\"", null);
                }));

                this.startProvider();

            }
        }

        /// <summary>
        /// Try to create the WiiProvider (this involves connecting to the Wiimote).
        /// </summary>
        private bool startProvider()
        {
            try
            {
                this.pWiiProvider.start();
                this.tryingToConnect = true;
                return true;
            }
            catch (Exception pError)
            {
                // Tear down.
                try
                {
                    this.pWiiProvider.stop();
                    this.tryingToConnect = false;
                    if (Settings.Default.completelyDisconnect)
                    {
                        completelyDisconnectAll();
                    }
                }
                catch { }

                // Report the error.
                Console.WriteLine(pError.Message);
                ShowMessage(pError.Message, MessageType.Error);
                //MessageBox.Show(pError.Message, "WiiTUIO", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Try to create the WiiProvider (this involves connecting to the Wiimote).
        /// </summary>
        private bool createProvider()
        {
            try
            {
                // Connect a Wiimote, hook events then start.
                this.pWiiProvider = new MultiWiiPointerProvider();
                this.pWiiProvider.OnStatusUpdate += new Action<WiimoteStatus>(pWiiProvider_OnStatusUpdate);
                this.pWiiProvider.OnConnect += new Action<int, int>(pWiiProvider_OnConnect);
                this.pWiiProvider.OnDisconnect += new Action<int, int>(pWiiProvider_OnDisconnect);
                return true;
            }
            catch (Exception pError)
            {
                // Tear down.
                try
                {

                }
                catch { }
                Console.WriteLine(pError.Message);
                // Report the error.cr
                ShowMessage(pError.Message, MessageType.Error);
                //MessageBox.Show(pError.Message, "WiiTUIO", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Tear down the provider connections.
        /// </summary>
        private void disconnectProvider()
        {
            this.tryingToConnect = false;
            // Disconnect the Wiimote.
            if (this.pWiiProvider != null)
            {
                this.pWiiProvider.stop();
            }

            //this.pWiiProvider = null;
            if (Settings.Default.completelyDisconnect)
            {
                completelyDisconnectAll();
            }
        }

        private void disconnectDolphin()
        {
            this.tryingToConnect = false;
            // Disconnect the Wiimote.
            if (this.pWiiProvider != null)
            {
                this.pWiiProvider.stop();
            }
        }

        private void completelyDisconnectAll()
        {
            //Disable Wiimote in device manager to disconnect it from the computer (so it doesn't drain battery when not used)
            Launcher.Launch("Driver", "devcon", " disable \"BTHENUM*_VID*57e*_PID&0306*\"", new Action(delegate ()
            {
                Launcher.Launch("Driver", "devcon", " enable \"BTHENUM*_VID*57e*_PID&0306*\"", new Action(delegate ()
                {
                    Launcher.Launch("Driver", "devcon", " disable \"BTHENUM*_VID*57e*_PID&0330*\"", new Action(delegate ()
                    {
                        Launcher.Launch("Driver", "devcon", " enable \"BTHENUM*_VID*57e*_PID&0330*\"", null);
                    }));
                }));
            }));
        }
        #endregion

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void PairWiimotes_Click(object sender, RoutedEventArgs e)
        {
            //this.disableMainControls();
            //this.pairWiimoteOverlay.Visibility = Visibility.Visible;
            //this.pairWiimoteOverlayPairing.Visibility = Visibility.Visible;

            this.runWiiPair();
        }

        private void runWiiPair()
        {
            if (!this.wiiPairRunning)
            {
                Dispatcher.BeginInvoke(new Action(delegate ()
                {
                    this.animateExpand(this.spPairing);//.Visibility = Visibility.Visible;
                    this.tbPair2.Visibility = Visibility.Collapsed;
                    this.tbPairDone.Visibility = Visibility.Visible;

                    this.pairWiimoteTRFail.Visibility = Visibility.Hidden;
                    this.pairWiimoteTryAgain.Visibility = Visibility.Hidden;
                    this.pairProgress.Visibility = Visibility.Visible;
                }), null);
                if (this.wiiPairThread != null)
                {
                    this.wiiPairThread.Abort();
                }
                this.wiiPairThread = new Thread(new ThreadStart(wiiPairThreadWorker));
                this.wiiPairThread.Priority = ThreadPriority.Normal;
                this.wiiPairThread.Start();
            }
        }

        private void wiiPairThreadWorker()
        {
            this.wiiPairRunning = true;
            wiiPair.start(true, 10);//First remove all connected devices.
        }

        private void stopWiiPair()
        {
            this.wiiPairRunning = false;
            wiiPair.stop();
        }

        public void onPairingProgress(WiiCPP.WiiPairReport report)
        {
            Console.WriteLine("Pairing progress: number=" + report.numberPaired + " removeMode=" + report.removeMode + " devicelist=" + report.deviceNames);
            if (report.status == WiiCPP.WiiPairReport.Status.RUNNING)
            {
                if (report.numberPaired > 0)
                {
                    Settings.Default.pairedOnce = true;
                }
            }
            else
            {
                if (report.removeMode && report.status != WiiCPP.WiiPairReport.Status.CANCELLED)
                {
                    this.wiiPairRunning = true;

                    Dispatcher.BeginInvoke(new Action(delegate ()
                    {
                        this.connectProvider();
                    }), null);

                    int stopat = 10;
                    if (this.startupPair)
                    {
                        stopat = 1;
                        this.startupPair = false;
                    }
                    wiiPair.start(false, stopat); //Run the actual pairing after removing all previous connected devices.
                }
                else
                {
                    this.wiiPairRunning = false;
                    Dispatcher.BeginInvoke(new Action(delegate ()
                    {
                        //this.canvasPairing.Visibility = Visibility.Collapsed;
                        this.animateCollapse(this.spPairing, false);
                        this.tbPair2.Visibility = Visibility.Visible;
                        this.tbPairDone.Visibility = Visibility.Collapsed;

                        this.pairProgress.IsActive = false;
                    }), null);
                }
            }
        }


        private void pairWiimoteTryAgain_Click(object sender, RoutedEventArgs e)
        {
            this.stopWiiPair();
            this.runWiiPair();
        }

        public void onPairingStarted()
        {
            this.disconnectProvider();
            Dispatcher.BeginInvoke(new Action(delegate ()
            {

                this.pairProgress.IsActive = true;
            }), null);
        }

        public void pairingConsole(string message)
        {
            Console.Write(message);
        }

        public void pairingMessage(string message, WiiCPP.WiiPairListener.MessageType type)
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                this.pairWiimoteText.Text = message;
                if (message == Scanning)
                {
                    pairWiimotePressSync.Visibility = Visibility.Visible;

                }
                else
                {
                    pairWiimotePressSync.Visibility = Visibility.Hidden;
                }

                if (type == WiiCPP.WiiPairListener.MessageType.ERR)
                {
                    this.ShowMessage(message, MessageType.Error);
                }

            }), null);
        }

        private void btnAppSettings_Click(object sender, RoutedEventArgs e)
        {
            this.showConfig();
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            this.showAbout();
        }

        private void PairWiimotesDone_Click(object sender, RoutedEventArgs e)
        {
            if (this.wiiPairRunning)
            {
                this.pairWiimoteText.Text = TClosing;
                this.pairWiimotePressSync.Visibility = Visibility.Hidden;

                this.stopWiiPair();
            }
            else
            {
                //this.pairWiimoteOverlay.Visibility = Visibility.Hidden;
                //this.enableMainControls();
            }
        }

        private void spInfoMsg_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //this.spInfoMsg.Visibility = Visibility.Collapsed;
            this.animateCollapse(spInfoMsg, false);
        }

        private void spErrorMsg_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //this.spErrorMsg.Visibility = Visibility.Collapsed;
            this.animateCollapse(spErrorMsg, false);
        }

        private void StartArcadeHook()
        {
            arcadeHook = ArcadeHookSingleton.Default;

            arcadeHookThread = new Thread(new ThreadStart(arcadeHook.ConnectToServer));
            arcadeHookThread.IsBackground = true;
            arcadeHookThread.Start();
        }

        public void StopArcadeHook()
        {
            if (arcadeHook != null)
            {
                arcadeHook.Stop();
                arcadeHookThread.Join();
                arcadeHook = null;
                arcadeHookThread = null;
            }
        }
        /// <summary>
        /// Obtiene una lista de las culturas (idiomas) disponibles en la aplicación,
        /// basándose en los ensamblados de recursos satélite.
        /// </summary>
        /// <returns>Una lista de objetos CultureInfo representando los idiomas disponibles.</returns>
        private List<CultureInfo> GetAvailableCultures()
        {
            var cultures = new List<CultureInfo>();
            // Obtiene la ruta del ensamblado principal
            var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (exePath == null) return cultures;

            // Añade la cultura por defecto (normalmente inglés, del fichero Resources.resx)
            cultures.Add(new CultureInfo("en"));

            // Busca directorios que coincidan con nombres de cultura válidos
            foreach (var dir in Directory.GetDirectories(exePath))
            {
                try
                {
                    var dirInfo = new DirectoryInfo(dir);
                    // Comprueba si el nombre del directorio es un nombre de cultura válido
                    var culture = CultureInfo.GetCultureInfo(dirInfo.Name);

                    // Comprueba si existe el ensamblado de recursos para esa cultura
                    if (File.Exists(Path.Combine(dir, $"{Assembly.GetExecutingAssembly().GetName().Name}.resources.dll")))
                    {
                        cultures.Add(culture);
                    }
                }
                catch (CultureNotFoundException)
                {
                    // El nombre del directorio no es una cultura válida, lo ignoramos.
                }
            }
            return cultures.Distinct().ToList();
        }
        private void btnChangeLang_Click(object sender, RoutedEventArgs e)
        {
            showLanguage();
        }

        public Task<string> ShowSelectAsync(string title, string message, IEnumerable<string> items, MetroDialogSettings settings)
        {
            string availableOptions = string.Join(", ", items);
            return this.ShowInputAsync(title, $"{message}\n\nOpciones: {availableOptions}", settings);
        }

        // **Función auxiliar ShowSelectAsync para MahApps.Metro**
        // MahApps no tiene un diálogo de selección directa, así que podemos simularlo.
        public Task<string> ShowSelectAsync(string title, string message, IEnumerable<string> items)
        {
           
            string availableOptions = string.Join(", ", items);
            return this.ShowInputAsync(title, $"{message}\n\nOpciones: {availableOptions}");
        }

        private void showLanguage()
        {
            // Oculta los otros paneles
            if (this.mainPanel.IsVisible) animateCollapse(this.mainPanel, false);
            if (this.canvasSettings.IsVisible) animateCollapse(this.canvasSettings, false);
            if (this.canvasAbout.IsVisible) animateCollapse(this.canvasAbout, false);

            // Muestra el panel de idioma
            if (!this.canvasLanguage.IsVisible)
            {
                animateExpand(this.canvasLanguage);
            }
        }

        private void LanguagePanel_OnClose()
        {
            // Le preguntamos al panel si el usuario eligió un nuevo idioma
            bool restart = languagePanel.ShouldRestart();

            // Volvemos a la pantalla principal
            showMain();

            // Si hay que reiniciar, mostramos el mensaje y lo hacemos
            if (restart)
            {
                MessageBox.Show(
                    Restart_Message,
                    Restart_Title,
                    MessageBoxButton.OK, MessageBoxImage.Information
                );

                //System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                Restart();
            }
        }
        /// <summary>
        /// Handles the Click event for the "Restart" button.
        /// Changes language and restarts the application.
        /// </summary>
        private void Restart()
        {

            try
            {
                string executablePath = System.Windows.Application.ResourceAssembly.Location;

                // Creates a new ProcessStartInfo for the restart
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    UseShellExecute = true, // Important for the operating system to handle the launch
                    WorkingDirectory = Path.GetDirectoryName(executablePath), // Sets the working directory
                    Arguments = "--restarting" // Adds the argument to indicate it's a restart
                };

                Process.Start(startInfo);

                // A small delay to ensure the new process has time to start
                Thread.Sleep(500);
                System.Windows.Application.Current.Shutdown();

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(string.Format(RestartError, ex.Message), "Restart Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }


}
