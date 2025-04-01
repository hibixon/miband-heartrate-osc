using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using MiBand_Heartrate.Extras;

namespace MiBand_Heartrate.Devices
{
    public class MiBand2_3_Device : Device
    {
        // Existing constants
        const string AUTH_SRV_ID = "0000fee1-0000-1000-8000-00805f9b34fb";
        const string AUTH_CHAR_ID = "00000009-0000-3512-2118-0009af100700";
        const string HEARTRATE_SRV_ID = "0000180d-0000-1000-8000-00805f9b34fb";
        const string HEARTRATE_CHAR_ID = "00002a39-0000-1000-8000-00805f9b34fb";
        const string HEARTRATE_NOTIFY_CHAR_ID = "00002a37-0000-1000-8000-00805f9b34fb";
        const string SENSOR_SRV_ID = "0000fee0-0000-1000-8000-00805f9b34fb";
        const string SENSOR_CHAR_ID = "00000001-0000-3512-2118-0009af100700";
        const string BATTERY_CHAR_ID = "00000006-0000-3512-2118-0009af100700";

        // Fields
        byte[] _key;
        BluetoothLEDevice _connectedDevice;
        GattDeviceService _heartrateService;
        GattCharacteristic _heartrateCharacteristic;
        GattCharacteristic _heartrateNotifyCharacteristic;
        GattDeviceService _sensorService;
        GattDeviceService _batteryService;
        GattCharacteristic _batteryNotifyCharacteristic;
        Thread _keepHeartrateAliveThread;
        bool _continuous;
        string _deviceId = "";


        public MiBand2_3_Device(DeviceInformation d)
        {
            _deviceId = d.Id;

            Name = d.Name;
            Model = DeviceModel.MIBAND_2_3;
            
            // doesn't seem to work
            //Properties.Settings.Default.autoConnectDeviceName = Name;
            //Properties.Settings.Default.autoConnectDeviceName = "2_3";
            //Properties.Settings.Default.Save();
        }


        /* Mi Band 2 - Auth
         * Before using Mi Band 2, an auth is required. Authenticate following this steps :
         *  1. Enable notification on authentification characteristic
         *  2. Generate auth key 16 bytes long
         *  3. Send key to auth characteristic and asking for a random number
         *  4. Get random number from characteristic notification
         *  5. Encrypt number using AES ECB mode without padding and with auth key
         *  6. Send encrypted random number to auth characteristic
         *  see https://leojrfs.github.io/writing/miband2-part1-auth/#reference
         */
        public override void Authenticate()
        {
            var task = Task.Run(async () => {
                Guid guid = new Guid(AUTH_SRV_ID);
                
                // doesn't seem to work
                //Properties.Settings.Default.autoConnectDeviceAuthKey = guid.ToString();
                //Properties.Settings.Default.Save();
                
                GattDeviceServicesResult service = await _connectedDevice.GetGattServicesForUuidAsync(guid);

                if (service.Status == GattCommunicationStatus.Success && service.Services.Count > 0) {
                    GattCharacteristicsResult characteristic = await service.Services[0].GetCharacteristicsForUuidAsync(new Guid(AUTH_CHAR_ID));

                    if (characteristic.Status == GattCommunicationStatus.Success && characteristic.Characteristics.Count > 0) {
                        GattCommunicationStatus notify = await characteristic.Characteristics[0].WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);

                        if (notify == GattCommunicationStatus.Success) {
                            characteristic.Characteristics[0].ValueChanged += OnAuthenticateNotify;

                            _key = SHA256.HashData(Guid.NewGuid().ToByteArray()).Take(16).ToArray();

                            using (var stream = new MemoryStream()) {
                                stream.Write(new byte[] { 0x01, 0x08 }, 0, 2);
                                stream.Write(_key, 0, _key.Length);
                                BLE.Write(characteristic.Characteristics[0], stream.ToArray());
                            }
                        }
                    }
                }
            });
        }

        void OnAuthenticateNotify(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            byte[] headers = new byte[3];
            
            using (DataReader reader = DataReader.FromBuffer(args.CharacteristicValue))
            {
                reader.ReadBytes(headers);

                if (headers[1] == 0x01)
                {
                    if (headers[2] == 0x01)
                    {
                        BLE.Write(sender, new byte[] { 0x02, 0x08 });
                    }
                    else
                    {
                        MessageWindow.ShowError("Authentication failed (1)");
                    }
                }
                else if (headers[1] == 0x02)
                {
                    byte[] number = new byte[reader.UnconsumedBufferLength];
                    reader.ReadBytes(number);

                    using (var stream = new MemoryStream())
                    {
                        stream.Write(new byte[] { 0x03, 0x08 }, 0, 2);

                        byte[] encryptedNumber = EncryptAuthenticationNumber(number);
                        stream.Write(encryptedNumber, 0, encryptedNumber.Length);

                        BLE.Write(sender, stream.ToArray());
                    }
                }
                else if (headers[1] == 0x03)
                {
                    if (headers[2] == 0x01)
                    {
                        Status = Devices.DeviceStatus.ONLINE_AUTH;
                    }
                    else
                    {
                        MessageWindow.ShowError("Authentication failed (3)");
                    }
                }
            }
        }

        byte[] EncryptAuthenticationNumber(byte[] number)
        {
            byte[] r;

            using (Aes aes = Aes.Create())
            {
                aes.Key = _key;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });

                using (MemoryStream stream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(stream, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(number, 0, number.Length);
                        cryptoStream.FlushFinalBlock();
                        r = stream.ToArray();
                    }
                }
            }

            return r;
        }


        public override void Connect()
        {
            Disconnect();

            if (_connectedDevice == null)
            {
                var task = Task.Run(async () => await BluetoothLEDevice.FromIdAsync(_deviceId));
                
                _connectedDevice = task.Result;
                _connectedDevice.ConnectionStatusChanged += OnDeviceConnectionChanged;

                Status = Devices.DeviceStatus.ONLINE_UNAUTH;

                Authenticate();
            }
        }

        private void OnDeviceConnectionChanged(BluetoothLEDevice sender, object args)
        {
            if (_connectedDevice != null && _connectedDevice.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                Status = Devices.DeviceStatus.OFFLINE;
            }
        }

        public override void Disconnect()
        {
            StopHeartrateMonitor();

            if (_connectedDevice != null)
            {
                _connectedDevice.Dispose();
                _connectedDevice = null;
            }

            Status = Devices.DeviceStatus.OFFLINE;
        }

        public override void Dispose()
        {
            Disconnect();
        }


        public override void StartHeartrateMonitor(bool continuous = false)
        {
            if (HeartrateMonitorStarted)
                return;

            _continuous = continuous;

            var task = Task.Run(async () =>
            {
                GattCharacteristic sensorCharacteristic = null;

                GattDeviceServicesResult sensorService = await _connectedDevice.GetGattServicesForUuidAsync(new Guid(SENSOR_SRV_ID));

                if (sensorService.Status == GattCommunicationStatus.Success && sensorService.Services.Count > 0)
                {
                    _sensorService = sensorService.Services[0];

                    GattCharacteristicsResult characteristic = await _sensorService.GetCharacteristicsForUuidAsync(new Guid(SENSOR_CHAR_ID));

                    if (characteristic.Status == GattCommunicationStatus.Success && characteristic.Characteristics.Count > 0)
                    {
                        sensorCharacteristic = characteristic.Characteristics[0];
                        BLE.Write(sensorCharacteristic, new byte[] { 0x01, 0x03, 0x19 });
                    }
                }

                GattDeviceServicesResult heartrateService = await _connectedDevice.GetGattServicesForUuidAsync(new Guid(HEARTRATE_SRV_ID));

                if (heartrateService.Status == GattCommunicationStatus.Success && heartrateService.Services.Count > 0)
                {
                    _heartrateService = heartrateService.Services[0];

                    GattCharacteristicsResult heartrateNotifyCharacteristic = await _heartrateService.GetCharacteristicsForUuidAsync(new Guid(HEARTRATE_NOTIFY_CHAR_ID));

                    if (heartrateNotifyCharacteristic.Status == GattCommunicationStatus.Success && heartrateNotifyCharacteristic.Characteristics.Count > 0)
                    {
                        GattCommunicationStatus notify = await heartrateNotifyCharacteristic.Characteristics[0].WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);

                        if (notify == GattCommunicationStatus.Success)
                        {
                            _heartrateNotifyCharacteristic = heartrateNotifyCharacteristic.Characteristics[0];
                            _heartrateNotifyCharacteristic.ValueChanged += OnHeartrateNotify;
                        }
                    }

                    GattCharacteristicsResult heartrateCharacteristicResult = await _heartrateService.GetCharacteristicsForUuidAsync(new Guid(HEARTRATE_CHAR_ID));

                    if (heartrateCharacteristicResult.Status == GattCommunicationStatus.Success && heartrateCharacteristicResult.Characteristics.Count > 0)
                    {
                        _heartrateCharacteristic = heartrateCharacteristicResult.Characteristics[0];

                        if (_continuous)
                        {
                            BLE.Write(_heartrateCharacteristic, new byte[] { 0x15, 0x01, 0x01 });

                            _keepHeartrateAliveThread = new Thread(RunHeartrateKeepAlive);
                            _keepHeartrateAliveThread.Start();
                        }
                        else
                        {
                            BLE.Write(_heartrateCharacteristic, new byte[] { 0x15, 0x02, 0x01 });
                        }

                        if (sensorCharacteristic != null)
                        {
                            BLE.Write(sensorCharacteristic, new byte[] { 0x02 });
                        }
                    }
                }

                Application.Current.Dispatcher.Invoke(delegate {
                    HeartrateMonitorStarted = true;
                });
            });
        }

        public override void StopHeartrateMonitor()
        {
            if (!HeartrateMonitorStarted)
                return;

            if (_keepHeartrateAliveThread != null)
            {
                _keepHeartrateAliveThread.Interrupt();
                _keepHeartrateAliveThread = null;
            }

            if (_heartrateCharacteristic != null)
            {
                BLE.Write(_heartrateCharacteristic, new byte[] { 0x15, 0x01, 0x00 });
                BLE.Write(_heartrateCharacteristic, new byte[] { 0x15, 0x02, 0x00 });
            }

            _heartrateCharacteristic = null;

            _heartrateNotifyCharacteristic = null;

            if (_heartrateService != null)
            {
                _heartrateService.Dispose();
                _heartrateService = null;
            }

            if (_sensorService != null)
            {
                _sensorService.Dispose();
                _sensorService = null;
            }

            Application.Current.Dispatcher.Invoke(delegate {
                HeartrateMonitorStarted = false;
            });

            GC.Collect();
        }


        void OnHeartrateNotify(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            using (DataReader reader = DataReader.FromBuffer(args.CharacteristicValue))
            {
                ushort value = reader.ReadUInt16();

                if (value > 0) // when sensor fail to retrieve heartrate it send a 0 value
                {
                    Heartrate = value;
                }

                if ( ! _continuous)
                {
                    StopHeartrateMonitor();
                }
            }
        }

        private async void InitializeBatteryService()
        {
            try
            {
                var batteryService = await _connectedDevice.GetGattServicesForUuidAsync(new Guid(SENSOR_SRV_ID));
                if (batteryService.Status != GattCommunicationStatus.Success || batteryService.Services.Count == 0) return;

                _batteryService = batteryService.Services[0];
                var characteristic = await _batteryService.GetCharacteristicsForUuidAsync(new Guid(BATTERY_CHAR_ID));
                if (characteristic.Status != GattCommunicationStatus.Success || characteristic.Characteristics.Count == 0) return;

                _batteryNotifyCharacteristic = characteristic.Characteristics[0];
                await _batteryNotifyCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                _batteryNotifyCharacteristic.ValueChanged += OnBatteryNotify;

                var result = await _batteryNotifyCharacteristic.ReadValueAsync();
                if (result.Status == GattCommunicationStatus.Success)
                {
                    UpdateBatteryValue(result.Value);
                }
            }
            catch { }
        }

        private void UpdateBatteryValue(IBuffer buffer)
        {
            try
            {
                if (buffer == null || buffer.Length < 3) return;

                byte[] rawData = new byte[buffer.Length];
                DataReader.FromBuffer(buffer).ReadBytes(rawData);

                Battery = rawData[1];
                IsCharging = rawData[2] == 0x01;
            }
            catch { }
        }

        private void OnBatteryNotify(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            UpdateBatteryValue(args.CharacteristicValue);
        }

        void RunHeartrateKeepAlive()
        {
            try {
                while (_heartrateCharacteristic != null)
                {
                    BLE.Write(_heartrateCharacteristic, new byte[] { 0x16 });
                    Thread.Sleep(5000);
                }
            } catch (ThreadInterruptedException) {
                Debug.WriteLine("BLE Device Alive Thread Killed");
            }
        }
    }
}
