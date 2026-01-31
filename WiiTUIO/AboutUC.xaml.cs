using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
using System.Reflection;

namespace WiiTUIO
{
    /// <summary>
    /// Interaction logic for AboutUC.xaml
    /// </summary>
    public partial class AboutUC : UserControl, SubPanel
    {
        public event Action OnClose;

        public AboutUC()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void btnAboutBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.OnClose != null)
            {
                this.OnClose();
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
        /// <summary>
        /// Obtiene la versión del ensamblado de la aplicación.
        /// </summary>
        public string AppVersion
        {
            get
            {
                // Obtiene el ensamblado en ejecución (tu aplicación)
                Assembly assembly = Assembly.GetExecutingAssembly();
                // Obtiene el objeto Version del ensamblado
                Version version = assembly.GetName().Version;

                // Formatea la versión como "vX.Y.Z"
                // Puedes ajustar el formato si solo quieres Major.Minor, etc.
                return $"v{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            }
        }
    }
}
