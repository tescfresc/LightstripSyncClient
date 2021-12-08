using System;
using System.Diagnostics;
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
        private GattCharacteristic lightChar;
        private bool charFound = false;

        private Forms.Timer keepAliveTimer;

        private bool lightsOn = true;
        private bool flickerLoop;
        private System.Drawing.Color color;

        private float colorTimeout = 50;
        private Stopwatch colorStopwatch = new Stopwatch();

        public BluetoothLEConnectionManager()
        {
            colorStopwatch.Start();
        }
        public void GetAvailableBluetoothDevices()
        {
            InitiateDeviceWatcher();
        }
        private void InitiateDeviceWatcher()
        {
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };
            var deviceWatcher = DeviceInformation.CreateWatcher(BluetoothLEDevice.GetDeviceSelectorFromPairingState(true), requestedProperties, DeviceInformationKind.AssociationEndpoint);
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
            return bluetoothLEDevice;
        }

        public async Task<bool> InitiateConnection(BluetoothLEDevice device)
        {
            lightStrip = device;

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
            Task.Run(() => FlickerLoop());
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

        public void ChangeColor(System.Drawing.Color newColor)
        {
            if (colorStopwatch.ElapsedMilliseconds < colorTimeout)
                return;
            this.color = newColor;
            var hexColor = ColorConverter.RgbToHex(new RGB(newColor.R, newColor.G, newColor.B));
            WriteCharacteristic(CreateBluetoothColourDataBytes(hexColor.ToString()));
            //WriteCharacteristic(CreateBluetoothColourDataBytes(hexColor.ToString(), 0xC300));
            //WriteCharacteristic(CreateBluetoothColourDataBytes("000000", 0x3C00));
            colorStopwatch.Restart();
        }
        private void ChangeColorTemp(System.Drawing.Color newColor)
        {
            if (colorStopwatch.ElapsedMilliseconds < colorTimeout)
                return;
            var hexColor = ColorConverter.RgbToHex(new RGB(newColor.R, newColor.G, newColor.B));
            WriteCharacteristic(CreateBluetoothColourDataBytes(hexColor.ToString()));
            colorStopwatch.Restart();
        }

        public void ToggleFlickerMode(bool state)
        {
            flickerLoop = state;
        }

        int Clamp(int v, int min, int max)
        {
            return Math.Max(Math.Min(v, max), min);
        }

        private async void FlickerLoop()
        {
            var random = new Random();
            while (true)
            {
                if (!flickerLoop)
                    continue;

                var flickerRange = 5;
                var flickerAmount = random.Next(flickerRange * 2) - flickerRange;
                var flickerColor = System.Drawing.Color.FromArgb(color.A, Clamp(color.R + flickerAmount, 0, 255),
                    Clamp(color.G + flickerAmount, 0, 255),
                    Clamp(color.B + flickerAmount, 0, 255));
                ChangeColorTemp(flickerColor);
                await Task.Delay(100);
            }
        }

        private byte[] CreateBluetoothPowerDataBytes(bool state)
        {
            return StringToByteArray(state ? "3301010000000000000000000000000000000033" : "3301000000000000000000000000000000000032");
        }
        private byte[] CreateBluetoothColourDataBytes(string hexColor)
        {
            var btString = "33051501" + hexColor + "0000000000ff7f0000000000";
            //var btString = "33051501" + hexColor + "000000000055550000000000";
            return CalculateCheckSum(StringToByteArray(btString));
        }
        private byte[] CreateBluetoothColourDataBytes(string hexColor, int lights)
        {
            var btString = "33051501" + hexColor + "0000000000" + lights.ToString("X4") + "0000000000";
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


    }
}


