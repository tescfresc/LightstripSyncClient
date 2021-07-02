using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Data;
using System.Drawing;


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
using Forms = System.Windows.Forms;
using System.Windows.Shapes;
using System.Windows.Interop;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using ColorHelper;

namespace LightstripSyncClient
{
    public class BluetoothLEConnectionManager
    {
        const string GattUUID = "00010203-0405-0607-0809-0a0b0c0d2b11";



        private ListView deviceListView;

        public BluetoothLEDevice lightStrip;
        private bool isRGBIC = false;
        private GattCharacteristic lightChar;
        private bool charFound = false;
        

        private Forms.Timer keepAliveTimer;

        private bool lightsOn = true;
        private bool rainbowLoop;

        public BluetoothLEConnectionManager(ListView deviceListView)
        {
            this.deviceListView = deviceListView;
        }
        public void GetAvailableBluetoothDevices()
        {
            InitiateDeviceWatcher();
        }
        private void InitiateDeviceWatcher()
        {
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };
            DeviceWatcher deviceWatcher = DeviceInformation.CreateWatcher(BluetoothLEDevice.GetDeviceSelectorFromPairingState(false), requestedProperties, DeviceInformationKind.AssociationEndpoint);
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;

            deviceWatcher.Start();
        }

        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInformation)
        {
            GetBluetoothDetails(deviceInformation);
        }
        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInformationUpdate)
        {

        }
        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate deviceInformationUpdate)
        {
        }

        async void GetBluetoothDetails(DeviceInformation deviceInformation)
        {
            BluetoothLEDevice bluetoothLEDevice = await BluetoothLEDevice.FromIdAsync(deviceInformation.Id);

            var deviceName = bluetoothLEDevice.Name;
            if (deviceName.Substring(0, 4) == "ihom")
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var item = new ListViewItem();
                    item.Content = bluetoothLEDevice.Name;
                    item.Tag = bluetoothLEDevice;
                    deviceListView.Items.Add(item);
                });
            }
        }

        public async void InitiateConnection()
        {
            var item = (ListViewItem)deviceListView.SelectedItem;
            lightStrip = (BluetoothLEDevice)item.Tag;
            isRGBIC = checkRGBIC();


            await GetGattCharacteristic();

            if(charFound)
            {
                MaintainConnection();
                Connect connectWindow = (LightstripSyncClient.Connect) App.Current.MainWindow;
                MainWindow main = new MainWindow();
                App.Current.MainWindow = main;
                connectWindow.Close();
                main.Show();
                
            }

        }

        async Task GetGattCharacteristic()
        {
            GattDeviceServicesResult result = await lightStrip.GetGattServicesAsync();
            if(result.Status == GattCommunicationStatus.Success)
            {
                var services = result.Services;
                foreach (var service in services)
                {
                    GattCharacteristicsResult characteristics = await service.GetCharacteristicsAsync();
                    foreach (var characteristic in characteristics.Characteristics)
                    {
                        if (characteristic.Uuid.ToString() == GattUUID)
                        {
                            lightChar = characteristic;
                            charFound = true;
                        }
                    }
                }
            }
        }

        private void MaintainConnection()
        {
            var keepAlive = "aa010000000000000000000000000000000000ab";
            var bytes = StringToByteArray(keepAlive);
            keepAliveTimer = new Forms.Timer();
            keepAliveTimer.Interval = 2000;
            keepAliveTimer.Tick += (sender, e) => KeepAliveTick(sender, e, bytes);
            keepAliveTimer.Start();
        }

        public void TogglePowerState(bool state)
        {
            lightsOn = state;
            WriteCharacteristic(CreateBluetoothPowerDataBytes(state));
        }

        private void KeepAliveTick(object sender, EventArgs e, byte[] bytes)
        {
            WriteCharacteristic(bytes);
        }

        public void ChangeColor(System.Drawing.Color color)
        {
            var hexColor = ColorHelper.ColorConverter.RgbToHex(new RGB(color.R, color.G, color.B));
            WriteCharacteristic(CreateBluetoothColourDataBytes(hexColor.ToString()));
        }

        public void ChangeBrightness(double value)
        {
            var hexValue = ((int)((value / 10) * 255)).ToString("X");
            hexValue = hexValue.Length == 1 ? "0" + hexValue : hexValue;

            WriteCharacteristic(CreateBluetoothBrightnessDataBytes(hexValue));
        }

        public void ToggleRainbowMode(bool state)
        {
            rainbowLoop = state;
            if (rainbowLoop) RainbowLoop();
        }

        async void RainbowLoop()
        {
            var colour = new HSV(0, 100, 100);
            while (rainbowLoop)
            {
                WriteCharacteristic(CreateBluetoothColourDataBytes(ColorHelper.ColorConverter.HsvToHex(colour).Value));
                colour.H += 1;
                await Task.Delay(20);
                if (colour.H > 359) colour.H = 0;
            }
        }

        private byte[] CreateBluetoothPowerDataBytes(bool state)
        {
            return StringToByteArray(state ? "3301010000000000000000000000000000000033" : "3301000000000000000000000000000000000032");
        }
        private byte[] CreateBluetoothColourDataBytes(string hexColor)
        {
            string btString;
            if(isRGBIC)
            {
                btString = "33051501" + hexColor + "0000000000ff7f0000000000";
            } else
            {
                btString = "330502" + hexColor + "00000000000000000000000000";
            }
            
            return CalculateCheckSum(StringToByteArray(btString));
          
        }
        private byte[] CreateBluetoothBrightnessDataBytes(string value)
        {
            var btString = "3304" + value + "00000000000000000000000000000000";
            return CalculateCheckSum(StringToByteArray(btString));
        }


        async void WriteCharacteristic(byte[] byteArray)
        {
            var writer = new DataWriter();
            writer.WriteBytes(byteArray);
            GattCommunicationStatus writeAttempt = await lightChar.WriteValueAsync(writer.DetachBuffer());
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        private byte[] CalculateCheckSum (byte[] bytes)
        {
            //calculate checksum
            var checksum = 0;
            foreach (var item in bytes)
            {
                checksum ^= item;
            }

            //add checksum to end of bytearray
            byte[] tempArray = new byte[bytes.Length + 1];
            bytes.CopyTo(tempArray, 0);
            tempArray[tempArray.Length - 1] = (byte)checksum;

            //reassign bytearray
            bytes = tempArray;

            return bytes;
        }

        public bool checkRGBIC()
        {
            return lightStrip.Name.Contains("ihoment_H6143");
        }

       
    }
}


