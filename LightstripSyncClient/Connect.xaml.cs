using System.Windows;
using System.Windows.Controls;
using Windows.Devices.Bluetooth;

namespace LightstripSyncClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Connect : Window
    {
        public Connect()
        {
            InitializeComponent();
            Globals.ColorSync = new ColorSync();
            Globals.BluetoothLEConnectionManager = new BluetoothLEConnectionManager();
            Globals.BluetoothLEConnectionManager.Devices.CollectionChanged += Devices_CollectionChanged;
            Globals.BluetoothLEConnectionManager.GetAvailableBluetoothDevices();
        }

        private void Devices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        { //   

            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var item = new ListViewItem();
                        foreach (var device in e.NewItems)
                        {
                            var btDevice = (BluetoothLEDevice)device;
                            item.Content = btDevice.Name;
                            item.Tag = btDevice;
                            Device_List_Box.Items.Add(item);
                        }

                    });
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    break;
                default:
                    break;
            }


        }

        private void Device_List_Box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Connect_Button.IsEnabled = true;
        }

        private async void Connect_Button_Click(object sender, RoutedEventArgs e)
        {
            var item = (ListViewItem)Device_List_Box.SelectedItem;
            var lightStrip = (BluetoothLEDevice)item.Tag;
            Connecting_Panel.Visibility = Visibility.Visible;
            Connecting_Text.Visibility = Visibility.Visible;

            if (await Globals.BluetoothLEConnectionManager.InitiateConnection(lightStrip))
            {
                Connect connectWindow = (Connect)Application.Current.MainWindow;
                MainWindow main = new MainWindow();
                App.Current.MainWindow = main;
                connectWindow.Close();
                main.Show();
            }
            Connecting_Panel.Visibility = Visibility.Hidden;
            Connecting_Text.Visibility = Visibility.Hidden;
        }
    }
}
