using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Drawing;

namespace LightstripSyncClient
{
    public partial class MainWindow : Window
    {
        private bool powerButtonState = true;
        private System.Windows.Forms.NotifyIcon notifyIcon;

        private bool rainbowMode = false;
        private bool syncMode = false;

        public MainWindow()
        {
            InitializeComponent();

            notifyIcon = new NotifyIcon();
            notifyIcon.Visible = true;
            //notifyIcon.Icon = (SystemIcons.Application);
            notifyIcon.Icon = (Icon) LightstripSyncClient.Properties.Resources.icon;
            notifyIcon.Text = "Lightstrip Controller";
            notifyIcon.Click += new EventHandler(Notify_Icon_Click);
        }

        private void Power_Button_Click(object sender, RoutedEventArgs e)
        {
            powerButtonState = !powerButtonState;
            Globals.BluetoothLEConnectionManager.TogglePowerState(powerButtonState);
            Power_Button.Content = powerButtonState ? "Power: ON" : "Power: OFF";
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                this.Hide();

            base.OnStateChanged(e);
        }

        private void Notify_Icon_Click(object sender, EventArgs e)
        {
            this.Show();
            WindowState = WindowState.Normal;
            this.Focus();
        }

        private void Color_Picker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
        {
            var newColor = System.Drawing.Color.FromArgb(255, Color_Picker.R, Color_Picker.G, Color_Picker.B);
            Globals.BluetoothLEConnectionManager.ChangeColor(newColor);
        }

        private void Rainbow_Button_Click(object sender, RoutedEventArgs e)
        {
            rainbowMode = !rainbowMode;
            Globals.BluetoothLEConnectionManager.ToggleRainbowMode(rainbowMode);
            if (rainbowMode)
            {
                Rainbow_Button.Content = "Rainbow Mode: ON";
                Color_Picker.IsEnabled = false;
                Sync_Button.IsEnabled = false;
                Power_Button.IsEnabled = false;
            }
            else
            {
                Rainbow_Button.Content = "Rainbow Mode: OFF";
                Color_Picker.IsEnabled = true;
                Sync_Button.IsEnabled = true;
                Power_Button.IsEnabled = true;
            }

        }

        private void Sync_Button_Click(object sender, RoutedEventArgs e)
        {
            syncMode = !syncMode;
            Globals.ColorSync.ToggleSync(syncMode, Globals.BluetoothLEConnectionManager);
            if (syncMode)
            {
                Sync_Button.Content = "Sync Mode: ON";
                Color_Picker.IsEnabled = false;
                Rainbow_Button.IsEnabled = false;
                Power_Button.IsEnabled = false;
            }
            else
            {
                Sync_Button.Content = "Sync Mode: OFF";
                Color_Picker.IsEnabled = true;
                Rainbow_Button.IsEnabled = true;
                Power_Button.IsEnabled = true;
            }
        }

        private void Disconnect_Button_Click(object sender, RoutedEventArgs e)
        {
            Globals.BluetoothLEConnectionManager.lightStrip.Dispose();
            System.Windows.Application.Current.Shutdown();
        }
    }
}
