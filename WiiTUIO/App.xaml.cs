using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

//using Hardcodet.Wpf.TaskbarNotification;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Interop;
using System.Windows.Media;
using System.IO.Pipes;
using System.IO;
using WiiTUIO.Properties; // Añadido para acceder a la clase Settings original

namespace WiiTUIO
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// The tray's taskbar icon
        /// </summary>
       // public static TaskbarIcon TB { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Carga la configuración al inicio de la aplicación
            // La llamada a Settings.Default.Load() se realiza implícitamente la primera vez que se accede a Settings.Default
            // Si necesitas forzar una carga explícita al inicio, puedes añadir:
            // Settings.Default.Load(); 

            // Verifica si la aplicación se está reiniciando (pasando el argumento --restarting)
            bool isRestarting = e.Args.Contains("--restarting");

            Process thisProc = Process.GetCurrentProcess();

            // Si NO estamos reiniciando Y ya hay otra instancia ejecutándose, muestra el mensaje y cierra.
            // O si hay argumentos pero no es el de reinicio, maneja los argumentos.
            if ((isAnotherInstanceRunning(thisProc) && !isRestarting) || (e.Args.Length != 0 && !isRestarting))
            {
                if (e.Args.Length != 0)
                {
                    // Si hay argumentos, los maneja (pero solo si no es un reinicio)
                    switch (e.Args[0])
                    {
                        case "-exit":
                            sendCommand("exit");
                            break;
                        case "-k":
                            string keymapValue = e.Args.Length > 1 ? e.Args[1] : "Default";
                            sendCommand("keymap", keymapValue);
                            break;
                        default:
                            Console.WriteLine("Invalid argument");
                            break;
                    }
                }
                else
                {
                    // Si no hay argumentos (y no es un reinicio), significa que se lanzó una segunda instancia normal.
                    MessageBox.Show("Touchmote is already running. Look for it in the taskbar.");
                }
                Application.Current.Shutdown(220);
            }
            else if (isRestarting)
            {
                // Si estamos reiniciando, simplemente permitimos que la aplicación continúe sin mostrar el mensaje.
                Debug.WriteLine("App: Iniciando como parte de un reinicio. Omitiendo la comprobación de instancia única.");
                // Si necesitas manejar otros argumentos durante el reinicio, puedes hacerlo aquí.
                // Por ejemplo, si el reinicio también pudiera llevar un argumento de keymap:
                // if (e.Args.Contains("-k")) { /* lógica para keymap */ }

                // Continúa con el inicio normal de la aplicación después de la lógica de reinicio
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();

                // Initialise the Tray Icon
                //TB = (TaskbarIcon)FindResource("tbNotifyIcon");
                //TB.ShowBalloonTip("Touchmote is running", "Click here to set it up", BalloonIcon.Info);

                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
                Application.Current.Exit += appWillExit;

                base.OnStartup(e);
            }
            else // No hay otra instancia ejecutándose y no hay argumentos (o solo el de reinicio que ya se manejó)
            {
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();

                // Initialise the Tray Icon
                //TB = (TaskbarIcon)FindResource("tbNotifyIcon");
                //TB.ShowBalloonTip("Touchmote is running", "Click here to set it up", BalloonIcon.Info);

                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
                Application.Current.Exit += appWillExit;

                base.OnStartup(e);
            }
        }

        private void appWillExit(object sender, ExitEventArgs e)
        {
            if (e.ApplicationExitCode != 220)
            {
                // Llama al método Save de la clase Settings original
                Settings.Default.Save();
                //TB.Dispose();
                SystemProcessMonitor.Default.Dispose();
            }
        }


        private void mnuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }
        /*
        private void TaskbarIcon_TrayBalloonTipClicked_1(object sender, RoutedEventArgs e)
        {
            TB.ShowTrayPopup();
        }
         * */

        private bool isAnotherInstanceRunning(Process thisProcess)
        {
            // Filtra el proceso actual para no contarse a sí mismo como "otra instancia"
            return Process.GetProcessesByName(thisProcess.ProcessName).Count(p => p.Id != thisProcess.Id) > 0;
        }

        private void sendCommand(string command, string value = null)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", "Gunmote", PipeDirection.Out))
                {
                    client.Connect(500);
                    using (var writer = new StreamWriter(client) { AutoFlush = true })
                    {
                        if (value != null)
                        {
                            writer.Write($"{command}{Convert.ToChar(31)}{value}");
                        }
                        else
                            writer.Write(command);
                    }
                    client.Close();
                }
            }
            catch { }
        }
    }
}
