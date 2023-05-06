using System;
using ADScan.Client.Data;
using ADScan.Client.Models;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System.Collections;
using System.Linq;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Extensions;
using System.Text;
using System.Threading.Tasks;
using Acr.UserDialogs;
using System.Threading;
using XamarinEssentials = Xamarin.Essentials;
using System.Reflection;
using Xamarin.Forms;

namespace ADScan.Client.Workers
{
    public class DeviceWorker
    {
        ADScanDatabase database = null;
        private readonly IAdapter _bluetoothAdapter;
        private System.Collections.Generic.List<MassiveDevice> _devices;
        private bool isBusy = false;
        
        public DeviceWorker()
        {
            _bluetoothAdapter = CrossBluetoothLE.Current.Adapter;
            //_bluetoothAdapter.ScanTimeout = 1200000;
            
            _bluetoothAdapter.DeviceAdvertised += _bluetoothAdapter_DeviceAdvertised;
            _bluetoothAdapter.DeviceDiscovered += _bluetoothAdapter_DeviceDiscovered;

        }

        private async void _bluetoothAdapter_DeviceDiscovered(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            if (e.Device != null)
            {
                if(e.Device.NativeDevice == null || _devices == null)
                {
                    return;
                }

                var macAddress = (string)e.Device.NativeDevice.GetType().GetProperty("Address").GetValue(e.Device.NativeDevice);

                if(string.IsNullOrEmpty(macAddress) || _devices.Count == 0)
                {
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Descubierto: " + macAddress);

                var device = _devices.Where(c => c.Address == macAddress && 
                                            (string.IsNullOrEmpty(c.Status) || c.Status == "Sent")).FirstOrDefault();

                if (device != null)
                {
                    // Connect to device
                    var connectParameters = new ConnectParameters(false, true);
                    await _bluetoothAdapter.ConnectToDeviceAsync(e.Device, connectParameters);

                    _connectedDevice = e.Device;
                    
                    if ((device.Status ?? "") == "Sent")
                    {
                        System.Diagnostics.Debug.WriteLine("Chequeando configuracion");

                        await ValidateConfiguration();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Enviando configuracion");
                        
                        var result = await InitializeServices();

                        if (result)
                        {
                            device.Status = "Sent";
                            
                            await database.Update<MassiveDevice>(device);
                        }
                    }
                }
            }
        }

        public async Task CheckConfiguration()
        {
            if (isBusy)
                return;

            System.Diagnostics.Debug.WriteLine("Iniciando...");

            isBusy = !(isBusy = false);
            
            if (database == null)
                database = await ADScanDatabase.Instance;

            _devices = await database.GetAll<MassiveDevice>();

            await _bluetoothAdapter.StartScanningForDevicesAsync();

            int count = 0;
            while (isBusy || count <= 3)
            {
                // Wait for one second
                await Task.Delay(1000);
                count += 1;

                if (count > 3)
                    break;
            }
            
            isBusy = !(isBusy = true);

            await _bluetoothAdapter.StopScanningForDevicesAsync();
        }

        private ICharacteristic sendCharacteristic;
        private ICharacteristic receiveCharacteristic;
        private IDevice _connectedDevice;

        private async void _bluetoothAdapter_DeviceAdvertised(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            
        }

        private async Task<bool> InitializeServices()
        {
            try
            {

                var service = await _connectedDevice.GetServiceAsync(GattIdentifiers.UartGattServiceId);

                if (service != null)
                {
                    try
                    {
                        var characterictis = await service.GetCharacteristicsAsync();
                        sendCharacteristic = await service.GetCharacteristicAsync(GattIdentifiers.UartGattCharacteristicSendId);

                        receiveCharacteristic = await service.GetCharacteristicAsync(GattIdentifiers.UartGattCharacteristicReceiveId);
                        
                        await SendConfiguration();

                        System.Diagnostics.Debug.WriteLine("Configuration sent");

                        await _bluetoothAdapter.DisconnectDeviceAsync(_connectedDevice);

                        isBusy = false;

                        return true;
                    }
                    catch (Exception ex)
                    {
                        return false;
                    }
                }
            }
            catch(Exception ex)
            {
                return false;
                // Output.Text += "Error initializing UART GATT service." + Environment.NewLine;
            }

            return false;
        }

        private async Task SendConfiguration(string index = "")
        {
            var data = await database.GetAll<Models.DeviceConfiguration>();

            foreach (var dt in data)
            {
                try
                {
                    if (dt.Index != index && !string.IsNullOrEmpty(index))
                        continue;

                    if (!dt.Enabled)
                        continue;

                    if (dt.Index == "01" && !string.IsNullOrEmpty(dt.Value))
                    {
                        // Fill string with 0 to the left
                        var rebooted = dt.Value.PadLeft(5, '0');
                        await SendCommand("01" + rebooted);
                    }

                    if (dt.Index == "02" && !string.IsNullOrEmpty(dt.Value))
                    {
                        var sleep = 0;

                        int.TryParse(dt.Value, out sleep);

                        // Fill string with 0 to the left
                        string hexValue = sleep.ToString().PadLeft(5, '0');

                        await SendCommand("02" + hexValue);
                    }

                    if (dt.Index == "03" && !string.IsNullOrEmpty(dt.Value))
                    {
                        // Fill string with 0 to the left
                        var rebooted = dt.Value.PadLeft(5, '0');
                        await SendCommand("03" + rebooted);
                    }

                    if (dt.Index == "04" && !string.IsNullOrEmpty(dt.Value))
                    {
                        // ADV Quantityt
                        var qty = dt.Value.PadLeft(5, '0');
                        await SendCommand("04" + qty);
                    }

                    if (dt.Index == "30" && !string.IsNullOrEmpty(dt.Value))
                    {
                        // Sensor
                        var sensor = dt.Value;

                        if (sensor == "Perno 120mm" || sensor == "10")
                        {
                            await SendCommand("3010");
                        }
                        else if (sensor == "Esparrago 250" || sensor == "30")
                        {
                            await SendCommand("3030");
                        }
                        else
                        {
                            await SendCommand("3020");
                        }
                    }

                    // Intern
                    if (dt.Index == "31" && !string.IsNullOrEmpty(dt.Value))
                    {
                        var intern = dt.Value.ToString();

                        if (intern == "6.2" || intern == "00")
                        {
                            await SendCommand("3100");
                        }
                        else
                        {
                            await SendCommand("3101");
                        }
                    }

                    // Battery
                    if (dt.Index == "40" && !string.IsNullOrEmpty(dt.Value))
                    {
                        var battery = dt.Value;

                        if (battery == "No/Rec" || battery == "00")
                        {
                            await SendCommand("4000");
                        }
                        else
                        {
                            await SendCommand("4001");
                        }
                    }

                    // Offset
                    if (dt.Index == "50" && !string.IsNullOrEmpty(dt.Value))
                    {
                        var offset = dt.Value;

                        int intValue = int.TryParse(offset, out intValue) ? intValue : 0;

                        if (intValue > -128 && intValue < 128)
                        {
                            var newValue = intValue - 128;
                            string message = "50" + Convert.ToInt32(newValue).ToString("X");

                            await SendCommand("50" + message);
                        }
                    }
                }
                catch(Exception ex) { }
            }
        }

        private async Task SendCommand(string command)
        {
            try
            {
                if (sendCharacteristic != null)
                {
                    var bytes = await sendCharacteristic.WriteAsync(Encoding.ASCII.GetBytes($"{command}\r\n"));

                    System.Diagnostics.Debug.WriteLine($"Enviado {command} result {bytes}");

                    Thread.Sleep(200);
                }
            }
            catch (Exception ex) { }
            {
                // Output.Text += "Error sending comand to UART." + Environment.NewLine;
            }
        }

        private async Task ValidateConfiguration()
        {
            if (_connectedDevice == null)
                return;
            
            var service = await _connectedDevice.GetServiceAsync(GattIdentifiers.UartGattServiceId);

            if (service != null)
            {
                var characterictis = await service.GetCharacteristicsAsync();
                sendCharacteristic = await service.GetCharacteristicAsync(GattIdentifiers.UartGattCharacteristicSendId);

                receiveCharacteristic = await service.GetCharacteristicAsync(GattIdentifiers.UartGattCharacteristicReceiveId);
                if (receiveCharacteristic != null)
                {
                    var descriptors = await receiveCharacteristic.GetDescriptorsAsync();

                    receiveCharacteristic.ValueUpdated += async (o, args) =>
                    {
                        try
                        {
                            var receivedBytes = args.Characteristic.Value;

                            // Convert the received bytes to hex
                            var hex = BitConverter.ToString(receivedBytes).Replace("-", string.Empty);

                            System.Diagnostics.Debug.WriteLine("TT: " + hex);

                            var result = await CheckConfigurationIsValid(hex.Substring(0, 2), hex.Substring(2));

                            if (!result)
                            {
                                await SendConfiguration(hex.Substring(0, 2));

                                var macAddress = (string)_connectedDevice.NativeDevice.GetType().GetProperty("Address").GetValue(_connectedDevice.NativeDevice);

                                var device = _devices.Where(c => c.Address == macAddress &&
                                                (string.IsNullOrEmpty(c.Status) || c.Status == "Sent")).FirstOrDefault();

                                if (device != null)
                                {
                                    device.Status = "OK";

                                    Acr.UserDialogs.UserDialogs.Instance.Toast($"Dispositivo {device.Address} actualizado");

                                    await database.Update<MassiveDevice>(device);

                                    if (_connectedDevice != null)
                                    {
                                        await _bluetoothAdapter.DisconnectDeviceAsync(_connectedDevice);
                                    }

                                    MessagingCenter.Send<string>("DeviceConfigured", "NewMessage");
                                }
                            }
                        }
                        catch(Exception ex)
                        {
                        }
                    };

                    await receiveCharacteristic.StartUpdatesAsync();

                    // Let's get the configuration
                    await SendCommand("99");
                }
            }
        }

        private async Task<bool> CheckConfigurationIsValid(string index, string value)
        {
            var data = await database.GetAll<Models.DeviceConfiguration>();
            var config = data.Where(c => c.Index == index).FirstOrDefault();

            if (config == null)
            {
                return false;
            }

            var result = false;
            var currentValue = "";
            
            switch (index)
            {
                case "01":
                    currentValue = ParseHex(value).ToString();

                    if (currentValue == config.Value || (int.Parse(currentValue) + 1).ToString() == value)
                        result = true;
                    
                    break;
                case "02":
                case "03":
                case "04":
                    currentValue = ParseHex(value).ToString();

                    if (currentValue == config.Value)
                        result = true;
                    
                    break;
                case "20":
                    currentValue = value;

                    if (config.Index == "20")
                        result = true;

                    break;
                case "21":
                    currentValue = value;

                    if (config.Index == "21")
                        result = true;

                    break;
                case "22":
                    // MacType.SelectedItem = "Custom";
                    break;
                case "23":
                    // txtMac.Text = value;
                    break;
                case "30":
                case "31":
                case "40":
                case "50":
                    currentValue = value;

                    if (config.Index == index && config.Value == value)
                        result = true;
                    
                    break;
                default:
                    break;
            }

            System.Diagnostics.Debug.WriteLine($"Validando: {index}, Valores: {value}, {currentValue}, Resultado: {result}");

            return result;
        }

        private int ParseHex(string hex)
        {
            return int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
        }
    }
}

