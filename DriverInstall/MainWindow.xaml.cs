using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Threading; // Added for Thread.CurrentThread
using System.Globalization; // Added for CultureInfo
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using static DriverInstall.Resources.Resources;

namespace DriverInstall
{
    public partial class MainWindow : Window
    {
        private bool shutdown = false;

        public MainWindow()
        {
            // Localization
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CurrentUICulture;

            Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture;

            InitializeComponent();

            if (Environment.GetCommandLineArgs().Contains("-silent"))
            {
                this.Visibility = Visibility.Hidden;
                this.shutdown = true;
            }

            if (Environment.GetCommandLineArgs().Contains("-install"))
            {
                if (Environment.GetCommandLineArgs().Contains("-vmulti"))
                {
                    this.installVmultiDriverComplete();
                }
            }
            else if (Environment.GetCommandLineArgs().Contains("-uninstall"))
            {

                if (Environment.GetCommandLineArgs().Contains("-vmulti"))
                {
                    this.uninstallVmultiDriverComplete();
                }
            }
            else if (Environment.GetCommandLineArgs().Contains("-removeAllButMK"))
            {
                this.removeAllButMKB();
            }

            if (shutdown)
            {
                Application.Current.Shutdown(1);
            }
        }

        private void consoleLine(string text)
        {
            this.console.Text += "\n";
            this.console.Text += text;
            // Desplazar el ScrollViewer al final para ver los mensajes más recientes
            this.console.ScrollToEnd();
        }

        private void installAll()
        {
            this.installVmultiDriverComplete();
        }

        private void uninstallAll()
        {
            this.uninstallVmultiDriverComplete();
        }

        private void installVmultiDriverComplete()
        {
            this.uninstallVmultiDriver();
            this.uninstallVmultiDriver(); // Llamada duplicada, ¿intencional?
            this.installVmultiDrivers();

            this.removeAllButMKB();
        }


        private void uninstallVmultiDriverComplete()
        {
            this.uninstallVmultiDriver();
            this.uninstallVmultiDriver(); // Llamada duplicada, ¿intencional?
        }

        private void installVmultiDrivers()
        {
            try
            {
                string[] drivers = { "vmultia", "vmultib", "vmultic", "vmultid" };

                foreach (string driver in drivers)
                {
                    System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        WorkingDirectory = System.AppDomain.CurrentDomain.BaseDirectory + "Driver\\" + driver + "\\",
                        FileName = System.AppDomain.CurrentDomain.BaseDirectory + "Driver\\devcon",
                        Arguments = $"install {driver}.inf ecologylab\\{driver}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    System.Diagnostics.Process proc = new System.Diagnostics.Process
                    {
                        StartInfo = procStartInfo
                    };

                    proc.Start();
                    string result = proc.StandardOutput.ReadToEnd();
                    Console.WriteLine(result); // Considerar enviar esto también a consoleLine
                    proc.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); // Considerar enviar esto también a consoleLine
                // --- LOCALIZACIÓN PENDIENTE: El mensaje de error también podría ser localizado o formateado ---
                consoleLine(string.Format(ErrorVmultiInstall, ex.Message));
            }
        }

        private void uninstallVmultiDriver()
        {
            try
            {
                //Devcon remove *multi*
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo();

                procStartInfo.WorkingDirectory = System.AppDomain.CurrentDomain.BaseDirectory + "Driver\\";

                procStartInfo.FileName = procStartInfo.WorkingDirectory + "devcon";
                procStartInfo.Arguments = "remove *vmulti*";

                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                string result = proc.StandardOutput.ReadToEnd();
                consoleLine(result);
                proc.WaitForExit();
            }
            catch (Exception objException)
            {
                consoleLine(objException.Message);
                // --- LOCALIZACIÓN PENDIENTE: El mensaje de error también podría ser localizado o formateado ---
                consoleLine(string.Format(ErrorVmultiUninstall, objException.Message ));
            }
        }

        private void removeAllButMKB()
        {
            // COLs que quieres bloquear y eliminar
            int[] drivers = { 1, 2, 4, 5, 6 };


            // Ahora ya sí, eliminas los dispositivos con devcon
            foreach (int i in drivers)
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory + "Driver\\",
                        FileName = AppDomain.CurrentDomain.BaseDirectory + "Driver\\devcon",
                        Arguments = "remove *vmulti*COL0" + i + "*",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    Process proc = new Process { StartInfo = psi };
                    proc.Start();
                    string result = proc.StandardOutput.ReadToEnd();
                    Console.WriteLine(result);
                    proc.WaitForExit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    consoleLine(string.Format(ErrorDisablingDriver, ex.Message));
                }
            }
        }
        private void btnInstall_Click(object sender, RoutedEventArgs e)
        {
            this.installVmultiDriverComplete();
            // --- LOCALIZACIÓN PENDIENTE: Este string debería venir de los recursos ---
            consoleLine(DriverInstallSuccesfull);
        }

        private void btnUninstall_Click(object sender, RoutedEventArgs e)
        {
            this.uninstallVmultiDriverComplete();
            // --- LOCALIZACIÓN PENDIENTE: Este string debería venir de los recursos ---
            consoleLine(DriverUninstallSuccessfull);
        }
    }
}