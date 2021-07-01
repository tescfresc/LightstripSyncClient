using System.Windows;
using System.Windows.Controls;


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
            Globals.colorSync = new ColorSync();
            Globals.bluetoothLEConnectionManager = new BluetoothLEConnectionManager(Device_List_Box);
            Globals.bluetoothLEConnectionManager.GetAvailableBluetoothDevices();
        }

        private void Device_List_Box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Connect_Button.IsEnabled = true;
        }

        private void Connect_Button_Click(object sender, RoutedEventArgs e)
        {
            Globals.bluetoothLEConnectionManager.InitiateConnection();
            Connecting_Panel.Visibility = Visibility.Visible;
            Connecting_Text.Visibility = Visibility.Visible;
        }
    }
}
