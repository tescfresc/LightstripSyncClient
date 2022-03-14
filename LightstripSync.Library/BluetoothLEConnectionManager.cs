using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ColorHelper;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using Forms = System.Windows.Forms;

namespace LightstripSyncClient
{
    public class BluetoothLEConnectionManager
    {
        private const string GattUUID = "00010203-0405-0607-0809-0a0b0c0d2b11";

        public ObservableCollection<BluetoothLEDevice­> Devices { get; private set; } = new ObservableCollection<BluetoothLEDevice>();
        public BluetoothLEDevice lightStrip;
        private bool isRGBIC = false;
        private GattCharacteristic lightChar;
        private bool charFound = false;

        private Forms.Timer keepAliveTimer;

        private bool lightsOn = true;
        private bool rainbowLoop;

        public BluetoothLEConnectionManager()
        {
        }
        public void GetAvailableBluetoothDevices()
        {
            InitiateDeviceWatcher();
        }
        private void InitiateDeviceWatcher()
        {
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };
            var deviceWatcher = DeviceInformation.CreateWatcher(BluetoothLEDevice.GetDeviceSelectorFromPairingState(false), requestedProperties, DeviceInformationKind.AssociationEndpoint);
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;

            deviceWatcher.Start();
        }

        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInformation)
        {
            var device = await GetBluetoothDetails(deviceInformation);
            if (device != null)
            {
                Devices.Add(device);
            }
        }
        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInformationUpdate)
        {

        }
        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate deviceInformationUpdate)
        {
        }

        private async Task<BluetoothLEDevice> GetBluetoothDetails(DeviceInformation deviceInformation)
        {
            var bluetoothLEDevice = await BluetoothLEDevice.FromIdAsync(deviceInformation.Id);

            var deviceName = bluetoothLEDevice.Name;
            return (deviceName.Substring(0, 4) == "ihom" || deviceName.Substring(0, 4) == "GBK_") ? bluetoothLEDevice : null;
        }

        public async Task<bool> InitiateConnection(BluetoothLEDevice device)
        {
            lightStrip = device;
            isRGBIC = checkRGBIC();

            await GetGattCharacteristic();

            if (charFound)
            {
                MaintainConnection();

                return true;
            }
            return false;

        }

        private async Task GetGattCharacteristic()
        {
            var result = await lightStrip.GetGattServicesAsync();
            if (result.Status == GattCommunicationStatus.Success)
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
            keepAliveTimer = new Forms.Timer
            {
                Interval = 2000
            };
            keepAliveTimer.Tick += (sender, e) => KeepAliveTick(sender, e, bytes);
            keepAliveTimer.Start();
        }

        public void TogglePowerState(bool state)
        {
            lightsOn = state;
            WriteCharacteristic(CreateBluetoothPowerDataBytes(lightsOn));
        }

        private void KeepAliveTick(object sender, EventArgs e, byte[] bytes)
        {
            WriteCharacteristic(bytes);
        }

        public void ChangeColor(System.Drawing.Color color)
        {
            var hexColor = ColorConverter.RgbToHex(new RGB(color.R, color.G, color.B));
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
            if (rainbowLoop)
            {
                RainbowLoop();
            }
        }

        private async void RainbowLoop()
        {
            var colour = new HSV(0, 100, 100);
            while (rainbowLoop)
            {
                WriteCharacteristic(CreateBluetoothColourDataBytes(ColorHelper.ColorConverter.HsvToHex(colour).Value));
                colour.H += 1;
                await Task.Delay(20);
                if (colour.H > 359)
                {
                    colour.H = 0;
                }
            }
        }

        private byte[] CreateBluetoothPowerDataBytes(bool state)
        {
            return StringToByteArray(state ? "3301010000000000000000000000000000000033" : "3301000000000000000000000000000000000032");
        }
        private byte[] CreateBluetoothColourDataBytes(string hexColor)
        {
            var btString = isRGBIC ? "33051501" + hexColor + "0000000000ff7f0000000000" : "330502" + hexColor + "00000000000000000000000000";
            return CalculateCheckSum(StringToByteArray(btString));
        }
        private byte[] CreateBluetoothBrightnessDataBytes(string value)
        {
            var btString = "3304" + value + "00000000000000000000000000000000";
            return CalculateCheckSum(StringToByteArray(btString));
        }

        private async void WriteCharacteristic(byte[] byteArray)
        {
            var writer = new DataWriter();
            writer.WriteBytes(byteArray);
            _ = await lightChar.WriteValueAsync(writer.DetachBuffer());
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        private byte[] CalculateCheckSum(byte[] bytes)
        {
            //calculate checksum
            var checksum = 0;
            foreach (var item in bytes)
            {
                checksum ^= item;
            }

            //add checksum to end of bytearray
            var tempArray = new byte[bytes.Length + 1];
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


