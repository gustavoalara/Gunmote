using MahApps.Metro.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using Microsoft.Win32; // +++ AÑADIDO: Necesario para OpenFileDialog

namespace WiiTUIO
{
    /// <summary>
    /// Interaction logic for KeymapConfigWindow.xaml
    /// </summary>
    public partial class KeymapConfigWindow : MetroWindow
    {
        public Action OnConfigChanged;

        private AdornerLayer adornerLayer;
        private KeymapOutputType selectedOutput = KeymapOutputType.ALL;
        private int selectedWiimote = 0;
        private Keymap currentKeymap;

        private SolidColorBrush defaultBrush = new SolidColorBrush(Color.FromRgb(46, 46, 46));
        private SolidColorBrush highlightBrush = new SolidColorBrush(Color.FromRgb(65, 177, 225));

        private HookApplicationViewModel hookAppVM;

        private static KeymapConfigWindow defaultInstance;
        public static KeymapConfigWindow Instance
        {
            get
            {
                if (defaultInstance == null)
                {
                    defaultInstance = new KeymapConfigWindow();
                }
                return defaultInstance;
            }
        }
        private void BrowseHookAppEntry_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null) return;

            HookApplicationViewModel.HookAppDataItem item = element.Tag as HookApplicationViewModel.HookAppDataItem;
            if (item == null) return;

            // Configurar el diálogo para buscar archivos .exe
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Seleccionar ejecutable"; // Puedes localizar esta cadena si lo deseas
            openFileDialog.Filter = "Aplicaciones ejecutables (*.exe)|*.exe|Todos los archivos (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            // Mostrar el diálogo
            if (openFileDialog.ShowDialog() == true)
            {
                // Obtener solo el nombre del archivo (ej. "mame.exe")
                string fileName = System.IO.Path.GetFileName(openFileDialog.FileName);

                // Asignar el nombre al SearchString.
                // La UI se actualizará gracias a INotifyPropertyChanged
                item.SearchString = fileName;

                // Asegurarse de que el cambio se guarde en la base de datos de keymap
                hookAppVM.UpdateKeymapDatabase(this.currentKeymap);
            }
        }
        public KeymapConfigWindow()
        {
            hookAppVM = new HookApplicationViewModel();

            InitializeComponent();

            this.tbKeymapTitle.Text = this.tbKeymapTitle.Tag.ToString();
            this.tbKeymapTitle.Foreground = new SolidColorBrush(Colors.Gray);
            this.tbOutputFilter.Text = this.tbOutputFilter.Tag.ToString();
            this.tbOutputFilter.Foreground = new SolidColorBrush(Colors.Gray);


            this.btnAll.IsEnabled = false;
            btnAllBorder.Background = highlightBrush;

            this.tbKeymapTitle.LostFocus += tbKeymapTitle_LostFocus;
            this.tbKeymapTitle.KeyUp += tbKeymapTitle_KeyUp;
            this.tbKeymapTitle.Foreground = new SolidColorBrush(Colors.Black);

            hookAppItemsControl.DataContext = hookAppVM;

            this.cbLayoutChooser.Checked += cbLayoutChooser_Checked;
            this.cbLayoutChooser.Unchecked += cbLayoutChooser_Unchecked;

            this.cbApplicationSearch.Checked += cbApplicationSearch_Checked;
            this.cbApplicationSearch.Unchecked += cbApplicationSearch_Unchecked;

            this.rbOnscreen.Checked += rbOnscreen_Checked;
            this.rbOffscreen.Checked += rbOffscreen_Checked;

        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            rbOnscreen.IsChecked = true;

            this.fillOutputList(selectedOutput, null);

            List<Keymap> allKeymaps = KeymapDatabase.Current.getAllKeymaps();
            // Only need to grab default keymap filename once. Was in the loop
            string defaultKeymapFilename = KeymapDatabase.Current.getKeymapSettings().getDefaultKeymap();
            foreach (Keymap keymap in allKeymaps)
            {
                if (keymap.Filename == defaultKeymapFilename)
                {
                    this.selectKeymap(keymap);
                }
            }

            this.fillKeymapList(allKeymaps);
        }

        private void fillKeymapList(List<Keymap> allKeymaps)
        {
            this.spLayoutList.Children.Clear();
            foreach (Keymap keymap in allKeymaps)
            {
                if (keymap.Filename != KeymapDatabase.Current.getKeymapSettings().getCalibrationKeymap() &&
                        keymap.Filename != KeymapDatabase.Current.getKeymapSettings().getFinishCalibrationKeymap()) //Hide calibration keymap from config window
                {
                    bool active = this.currentKeymap.Filename == keymap.Filename;
                    bool defaultk = keymap.Filename == KeymapDatabase.Current.getKeymapSettings().getDefaultKeymap();
                    KeymapRow row = new KeymapRow(keymap, active, defaultk);
                    row.OnClick += selectKeymap;
                    this.spLayoutList.Children.Add(row);
                }
            }
        }

        private void output_OnDragStop(Adorner obj)
        {
            this.adornerLayer.Remove(obj);
        }

        private void output_OnDragStart(Adorner obj)
        {
            if (this.adornerLayer == null)
            {
                this.adornerLayer = AdornerLayer.GetAdornerLayer(this.mainPanel);
            }
            if (!this.adornerLayer.GetChildObjects().Contains(obj))
            {
                this.adornerLayer.Add(obj);
            }
        }

        private void selectWiimoteNumber(int number)
        {
            this.selectedWiimote = number;
            this.fillConnectionLists(currentKeymap, number);
        }


        private void selectKeymap(Keymap keymap)
        {
            this.currentKeymap = keymap;

            this.tbKeymapTitle.Text = keymap.getName();

            hookAppItemsControl.DataContext = null;
            hookAppVM.SearchStrings.Clear();
            hookAppVM.GenerateStringsForKeymap(this.currentKeymap);
            hookAppItemsControl.DataContext = hookAppVM;

            this.cbApplicationSearch.IsChecked = KeymapDatabase.Current.getKeymapSettings().isInApplicationSearch(this.currentKeymap);
            this.cbLayoutChooser.IsChecked = KeymapDatabase.Current.getKeymapSettings().isInLayoutChooser(this.currentKeymap);

            this.tbDelete.Visibility = this.currentKeymap.Filename == KeymapDatabase.Current.getKeymapSettings().getDefaultKeymap() ? Visibility.Hidden : Visibility.Visible;

            this.fillConnectionLists(keymap, 0);

            this.fillKeymapList(KeymapDatabase.Current.getAllKeymaps());
        }

        private void appendConnectionList(List<KeymapInput> list, Keymap keymap, int wiimote, bool defaultKeymap, Panel container)
        {
            foreach (KeymapInput input in list)
            {
                KeymapOutConfig config = keymap.getConfigFor(wiimote, input.Key);
                if (config != null)
                {
                    KeymapConnectionRow row = new KeymapConnectionRow(input, config, defaultKeymap);
                    row.OnConfigChanged += connectionRow_OnConfigChanged;
                    row.OnDragStart += output_OnDragStart;
                    row.OnDragStop += output_OnDragStop;
                    container.Children.Add(row);
                }
            }
        }

        private void fillConnectionLists(Keymap keymap, int wiimote)
        {
            bool defaultKeymap = wiimote == 0 && keymap.Filename == KeymapDatabase.Current.getDefaultKeymap().Filename;

            this.spWiimoteConnections.Children.Clear();
            this.spNunchukConnections.Children.Clear();
            this.spClassicConnections.Children.Clear();

            // Need to force garbage collection on a switch
            GC.Collect(2, GCCollectionMode.Forced);

            this.appendConnectionList(KeymapDatabase.Current.getAvailableInputs(KeymapInputSource.IR, rbOnscreen.IsChecked ?? false), keymap, wiimote, defaultKeymap, this.spWiimoteConnections);
            this.appendConnectionList(KeymapDatabase.Current.getAvailableInputs(KeymapInputSource.WIIMOTE, rbOnscreen.IsChecked ?? false), keymap, wiimote, defaultKeymap, this.spWiimoteConnections);
            this.appendConnectionList(KeymapDatabase.Current.getAvailableInputs(KeymapInputSource.NUNCHUK, rbOnscreen.IsChecked ?? false), keymap, wiimote, defaultKeymap, this.spNunchukConnections);
            this.appendConnectionList(KeymapDatabase.Current.getAvailableInputs(KeymapInputSource.CLASSIC, rbOnscreen.IsChecked ?? false), keymap, wiimote, defaultKeymap, this.spClassicConnections);

        }

        private void connectionRow_OnConfigChanged(KeymapInput input, KeymapOutConfig config)
        {
            this.currentKeymap.setConfigFor(this.selectedWiimote, input, config);
            if (OnConfigChanged != null)
            {
                OnConfigChanged();
            }
        }

        private void cbOutput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbOutput.SelectedItem != null && ((ComboBoxItem)cbOutput.SelectedItem).Content != null)
            {
                ComboBoxItem cbItem = (ComboBoxItem)cbOutput.SelectedItem;
                if (cbItem == cbiAll)
                {
                    this.selectedOutput = KeymapOutputType.ALL;
                }
                else if (cbItem == cbiKeyboard)
                {
                    this.selectedOutput = KeymapOutputType.KEYBOARD;
                }
                else if (cbItem == cbiMouse)
                {
                    this.selectedOutput = KeymapOutputType.MOUSE;
                }
                else if (cbItem == cbi360)
                {
                    this.selectedOutput = KeymapOutputType.XINPUT;
                }
                else if (cbItem == cbiWiimote)
                {
                    this.selectedOutput = KeymapOutputType.WIIMOTE;
                }
                else if (cbItem == cbiCursor)
                {
                    this.selectedOutput = KeymapOutputType.CURSOR;
                }
                else if (cbItem == cbiOther)
                {
                    this.selectedOutput = KeymapOutputType.DISABLE;
                }
                this.fillOutputList(this.selectedOutput, "");
            }
        }

        private void fillOutputList(KeymapOutputType type, string filter)
        {
            this.spOutputList.Children.Clear();
            List<KeymapOutput> allOutputs = KeymapDatabase.Current.getAvailableOutputs(type);
            allOutputs.Sort(new KeymapOutputComparer());

            foreach (KeymapOutput output in allOutputs)
            {
                if (filter == null || filter == "" || output.Name.ToLower().Contains(filter.ToLower()))
                {
                    KeymapOutputRow row = new KeymapOutputRow(output);
                    row.OnDragStart += output_OnDragStart;
                    row.OnDragStop += output_OnDragStop;
                    this.spOutputList.Children.Add(row);
                }
            }
        }

        private void tbOutputFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.tbOutputFilter.Text == this.tbOutputFilter.Tag.ToString())
            {
                this.fillOutputList(selectedOutput, null);
            }
            else
            {
                this.fillOutputList(selectedOutput, this.tbOutputFilter.Text);
            }
        }

        private void tb_placeholder_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox)
            {
                TextBox tb = (TextBox)sender;
                if (tb.Text == tb.Tag.ToString())
                {
                    tb.Text = "";
                    tb.Foreground = new SolidColorBrush(Colors.Black);
                }
            }
        }

        private void tb_placeholder_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox)
            {
                TextBox tb = (TextBox)sender;
                if (tb.Text == "")
                {
                    tb.Text = tb.Tag.ToString();
                    tb.Foreground = new SolidColorBrush(Colors.Gray);
                }
            }
        }

        private void tbKeymapTitle_LostFocus(object sender, RoutedEventArgs e)
        {
            if (tbKeymapTitle.Text != "" && tbKeymapTitle.Text != tbKeymapTitle.Tag.ToString())
            {
                this.currentKeymap.setName(this.tbKeymapTitle.Text);
                this.fillKeymapList(KeymapDatabase.Current.getAllKeymaps());
            }
        }

        void tbKeymapTitle_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (tbKeymapTitle.Text != "" && tbKeymapTitle.Text != tbKeymapTitle.Tag.ToString())
                {
                    this.currentKeymap.setName(this.tbKeymapTitle.Text);
                    this.fillKeymapList(KeymapDatabase.Current.getAllKeymaps());
                }
            }
        }

        private void btnAll_Click(object sender, RoutedEventArgs e)
        {
            btnAll.IsEnabled = false;
            btnAllBorder.Background = highlightBrush;
            btn1.IsEnabled = true;
            btn1Border.Background = defaultBrush;
            btn2.IsEnabled = true;
            btn2Border.Background = defaultBrush;
            btn3.IsEnabled = true;
            btn3Border.Background = defaultBrush;
            btn4.IsEnabled = true;
            btn4Border.Background = defaultBrush;
            this.selectWiimoteNumber(0);
        }

        private void btn1_Click(object sender, RoutedEventArgs e)
        {
            btnAll.IsEnabled = true;
            btnAllBorder.Background = defaultBrush;
            btn1.IsEnabled = false;
            btn1Border.Background = highlightBrush;
            btn2.IsEnabled = true;
            btn2Border.Background = defaultBrush;
            btn3.IsEnabled = true;
            btn3Border.Background = defaultBrush;
            btn4.IsEnabled = true;
            btn4Border.Background = defaultBrush;
            this.selectWiimoteNumber(1);
        }

        private void btn2_Click(object sender, RoutedEventArgs e)
        {
            btnAll.IsEnabled = true;
            btnAllBorder.Background = defaultBrush;
            btn1.IsEnabled = true;
            btn1Border.Background = defaultBrush;
            btn2.IsEnabled = false;
            btn2Border.Background = highlightBrush;
            btn3.IsEnabled = true;
            btn3Border.Background = defaultBrush;
            btn4.IsEnabled = true;
            btn4Border.Background = defaultBrush;
            this.selectWiimoteNumber(2);
        }

        private void btn3_Click(object sender, RoutedEventArgs e)
        {
            btnAll.IsEnabled = true;
            btnAllBorder.Background = defaultBrush;
            btn1.IsEnabled = true;
            btn1Border.Background = defaultBrush;
            btn2.IsEnabled = true;
            btn2Border.Background = defaultBrush;
            btn3.IsEnabled = false;
            btn3Border.Background = highlightBrush;
            btn4.IsEnabled = true;
            btn4Border.Background = defaultBrush;
            this.selectWiimoteNumber(3);
        }

        private void btn4_Click(object sender, RoutedEventArgs e)
        {
            btnAll.IsEnabled = true;
            btnAllBorder.Background = defaultBrush;
            btn1.IsEnabled = true;
            btn1Border.Background = defaultBrush;
            btn2.IsEnabled = true;
            btn2Border.Background = defaultBrush;
            btn3.IsEnabled = true;
            btn3Border.Background = defaultBrush;
            btn4.IsEnabled = false;
            btn4Border.Background = highlightBrush;
            this.selectWiimoteNumber(4);
        }

        private void cbLayoutChooser_Checked(object sender, RoutedEventArgs e)
        {
            KeymapDatabase.Current.getKeymapSettings().addToLayoutChooser(this.currentKeymap);
        }

        private void cbLayoutChooser_Unchecked(object sender, RoutedEventArgs e)
        {
            KeymapDatabase.Current.getKeymapSettings().removeFromLayoutChooser(this.currentKeymap);
        }

        private void cbApplicationSearch_Checked(object sender, RoutedEventArgs e)
        {
            // Need blank string to add profile to application search list
            KeymapDatabase.Current.getKeymapSettings().addToApplicationSearch(this.currentKeymap, "");
        }

        private void cbApplicationSearch_Unchecked(object sender, RoutedEventArgs e)
        {
            // Keep events from updating UI controls for now
            hookAppItemsControl.DataContext = null;

            hookAppVM.ClearApplicationList(this.currentKeymap);
            // Update UI with updated data
            hookAppItemsControl.DataContext = hookAppVM;
        }

        private void tbDelete_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (MessageBoxResult.Yes == MessageBox.Show("Esto borrará permanentemente el fichero " + this.currentKeymap.Filename + ", ¿estás seguro?", "Confirmación de borrado de keymap", MessageBoxButton.YesNo, MessageBoxImage.Warning))
            {
                if (KeymapDatabase.Current.deleteKeymap(this.currentKeymap))
                {
                    this.selectKeymap(KeymapDatabase.Current.getDefaultKeymap());
                }
            }
        }

        private void bAddKeymap_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Keymap newKeymap = KeymapDatabase.Current.createNewKeymap();
            selectKeymap(newKeymap);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void HookApp_TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            hookAppVM.UpdateKeymapDatabase(this.currentKeymap);
        }

        private void HookApp_TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                hookAppVM.UpdateKeymapDatabase(this.currentKeymap);
            }
        }

        private void AddHookAppEntry(object sender, MouseButtonEventArgs e)
        {
            hookAppVM.AddNewSearchString();
        }

        private void RemoveHookAppEntry(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            HookApplicationViewModel.HookAppDataItem item =
                element.Tag as HookApplicationViewModel.HookAppDataItem;

            hookAppVM.RemoveSearchString(item, this.currentKeymap);
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            hookAppItemsControl.DataContext = null;
        }

        private void rbOnscreen_Checked(object sender, RoutedEventArgs e)
        {
            rbOffscreen.IsChecked = false;
            this.fillConnectionLists(currentKeymap, this.selectedWiimote);
        }

        private void rbOffscreen_Checked(object sender, RoutedEventArgs e)
        {
            rbOnscreen.IsChecked = false;
            this.fillConnectionLists(currentKeymap, this.selectedWiimote);
        }
    }

    public class HookApplicationViewModel
    {
        public class HookAppDataItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
            private string searchString;
            public string SearchString
            {
                get => searchString;
                set
                {
                    // --- MODIFICADO: Añadida comprobación y notificación ---
                    if (searchString == value) return; // Evitar notificaciones innecesarias
                    searchString = value;
                    Placeholder = false;
                    OnPropertyChanged("SearchString"); // ¡Notificar a la UI!
                    // --- FIN DE LA MODIFICACIÓN ---
                }
            }

            private bool placeholder;
            public bool Placeholder
            {
                get => placeholder;
                set
                {
                    if (placeholder == value) return;
                    placeholder = value;

                    PlaceholderChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler PlaceholderChanged;

            public SolidColorBrush BrushColor
            {
                get
                {
                    SolidColorBrush brush = null;
                    if (!placeholder)
                    {
                        brush = new SolidColorBrush(Colors.Black);
                    }
                    if (placeholder)
                    {
                        brush = new SolidColorBrush(Colors.Gray);
                    }

                    return brush;
                }
            }
            public event EventHandler BrushColorChanged;

            private int index;
            public int Index
            {
                get => index;
                set => index = value;
            }

            public HookAppDataItem()
            {
                PlaceholderChanged += HookAppDataItem_PlaceholderChanged;
            }

            public HookAppDataItem(string searchString, bool placeholder) : this()
            {
                this.searchString = searchString;
                this.placeholder = placeholder;
            }

            private void HookAppDataItem_PlaceholderChanged(object sender, EventArgs e)
            {
                BrushColorChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private const string SEARCH_STRING_HELPTEXT = "Cadena de búsqueda (procesa el nombre o título de la ventana, p.ej. example.exe)";
        private ObservableCollection<HookAppDataItem> searchStrings = new ObservableCollection<HookAppDataItem>();
        public ObservableCollection<HookAppDataItem> SearchStrings => searchStrings;

        public HookApplicationViewModel()
        {
            searchStrings.CollectionChanged += SearchStrings_CollectionChanged;
        }

        private void SearchStrings_CollectionChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                for (int i = e.OldStartingIndex; i < searchStrings.Count; i++)
                {
                    searchStrings[i].Index = i;
                }
            }
        }

        public void GenerateStringsForKeymap(Keymap keymap)
        {
            string searchstring = KeymapDatabase.Current.getKeymapSettings().getSearchStringFor(keymap);
            if (searchstring != null && searchstring != "")
            {
                List<string> tempList = searchstring.Split(Convert.ToChar(31)).ToList();
                int index = 0;
                foreach (string hookString in tempList)
                {
                    searchStrings.Add(
                        new HookAppDataItem(hookString, false)
                        {
                            Index = index,
                        });

                    index++;
                }
            }
            else
            {
                searchStrings.Add(
                    new HookAppDataItem(SEARCH_STRING_HELPTEXT, true)
                    {
                        Index = 0,
                    });
            }
        }

        public void AddNewSearchString()
        {
            HookAppDataItem item = new HookAppDataItem("", false)
            {
                Index = searchStrings.Count,
            };

            searchStrings.Add(item);
        }

        public void RemoveSearchString(HookAppDataItem item, Keymap keymap)
        {
            int count = searchStrings.Count;
            if (count <= 1)
            {
                return;
            }

            int index = searchStrings.IndexOf(item);
            //int index = item.Index;
            searchStrings.RemoveAt(index);
            UpdateKeymapDatabase(keymap);
        }

        public void UpdateKeymapDatabase(Keymap keymap)
        {
            StringBuilder sb = new StringBuilder();
            int count = searchStrings.Count;
            int idx = 0;
            foreach (HookAppDataItem item in searchStrings)
            {
                if (!string.IsNullOrEmpty(item.SearchString) &&
                    !item.Placeholder)
                {
                    sb.Append(item.SearchString);

                    if (idx < count - 1)
                    {
                        sb.Append(Convert.ToChar(31));
                    }
                }

                idx++;
            }

            KeymapDatabase.Current.getKeymapSettings().setSearchStringFor(keymap,
                sb.ToString());
        }

        public void AddToApplicationSearch(Keymap keymap)
        {
            // Need blank string to add profile to application search list
            KeymapDatabase.Current.getKeymapSettings().addToApplicationSearch(keymap, "");
            // Possibly update with current data
            //UpdateKeymapDatabase(keymap);
        }

        public void ClearApplicationList(Keymap keymap)
        {
            KeymapDatabase.Current.getKeymapSettings().removeFromApplicationSearch(keymap);
            searchStrings.Clear();
            searchStrings.Add(
                new HookAppDataItem(SEARCH_STRING_HELPTEXT, true)
                {
                    Index = 0,
                });
        }
    }
}
