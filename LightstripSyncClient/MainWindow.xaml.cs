using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LightstripSyncClient
{
    public partial class MainWindow : Window
    {
        private bool powerButtonState = true;
        private System.Drawing.Color currentColour = System.Drawing.Color.White;

        private bool rainbowMode = false;
        private bool syncMode = false;
        public MainWindow()
        {
            InitializeComponent();
            
        }

        private void Power_Button_Click(object sender, RoutedEventArgs e)
        {
            powerButtonState = !powerButtonState;
            Globals.bluetoothLEConnectionManager.TogglePowerState(powerButtonState);
            Power_Button.Content = powerButtonState ? "Power: ON" : "Power: OFF";
        }

        private void Pick_Colour_Button_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog colorPicker = new ColorDialog();
            colorPicker.AllowFullOpen = true;
            colorPicker.Color = System.Drawing.Color.White;
            DialogResult result = colorPicker.ShowDialog();
            if(result.ToString() == "OK")
            {
                currentColour = colorPicker.Color;
                Globals.bluetoothLEConnectionManager.ChangeColor(currentColour);
            }
        }

        private void Brightness_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Globals.bluetoothLEConnectionManager.ChangeBrightness(Brightness_Slider.Value);
        }

        private void Rainbow_Button_Click(object sender, RoutedEventArgs e)
        {
            rainbowMode = !rainbowMode;
            Globals.bluetoothLEConnectionManager.ToggleRainbowMode(rainbowMode);
            if(rainbowMode)
            {
                Rainbow_Button.Content = "Rainbow Mode: ON";
                Pick_Colour_Button.IsEnabled = false;
                Brightness_Slider.IsEnabled = false;
                Sync_Button.IsEnabled = false;
                Power_Button.IsEnabled = false;
            } else
            {
                Rainbow_Button.Content = "Rainbow Mode: OFF";
                Pick_Colour_Button.IsEnabled = true;
                Brightness_Slider.IsEnabled = true;
                Sync_Button.IsEnabled = true;
                Power_Button.IsEnabled = true;
            }
            
        }

        private void Sync_Button_Click(object sender, RoutedEventArgs e)
        {
            syncMode = !syncMode;
            Globals.colorSync.ToggleSync(syncMode);
            if (syncMode)
            {
                Sync_Button.Content = "Sync Mode: ON";
                Pick_Colour_Button.IsEnabled = false;
                Brightness_Slider.IsEnabled = false;
                Rainbow_Button.IsEnabled = false;
                Power_Button.IsEnabled = false;
            }
            else
            {
                Sync_Button.Content = "Sync Mode: OFF";
                Pick_Colour_Button.IsEnabled = true;
                Brightness_Slider.IsEnabled = true;
                Rainbow_Button.IsEnabled = true;
                Power_Button.IsEnabled = true;
            }
        }

        private void Disconnect_Button_Click(object sender, RoutedEventArgs e)
        {
            Globals.bluetoothLEConnectionManager.lightStrip.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        private void Test_Button_Click(object sender, RoutedEventArgs e)
        {
            //Globals.bluetoothLEConnectionManager.TestCommand();
        }
    }
}
