using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static System.Net.Mime.MediaTypeNames;
using XamarinEssentials = Xamarin.Essentials;

namespace ADScan.Client
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DeviceConfiguration : ContentPage
    {
        private readonly IDevice _connectedDevice;
        private readonly IAdapter _bluetoothAdapter;
        
        public DeviceConfiguration(IDevice connectedDevice)
        {
            InitializeComponent();
            _connectedDevice = connectedDevice;
            _bluetoothAdapter = CrossBluetoothLE.Current.Adapter;

            btnLoad.Clicked += BtnLoad_Clicked;
            btnSave.Clicked += BtnSave_Clicked;
            btnDisconnect.Clicked += BtnDisconnect_Clicked;
            MacType.SelectedIndexChanged += MacType_SelectedIndexChanged;
        }

        protected async override void OnDisappearing()
        {
            base.OnDisappearing();

            if (_bluetoothAdapter.ConnectedDevices.Count > 0)
            {
                if (_connectedDevice != null)
                {
                    await _bluetoothAdapter.DisconnectDeviceAsync(_connectedDevice);
                }
            }
        }

        private async void BtnDisconnect_Clicked(object sender, EventArgs e)
        {
            if (_bluetoothAdapter.ConnectedDevices.Count > 0)
            {
                if (_connectedDevice != null)
                {
                    await _bluetoothAdapter.DisconnectDeviceAsync(_connectedDevice);
                }
            }
        }

        private void BtnLoad_Clicked(object sender, EventArgs e)
        {
            LoadPreviousConfiguration();
        }

        private ICharacteristic sendCharacteristic;
        private ICharacteristic receiveCharacteristic;

        protected override void OnAppearing()
        {
            base.OnAppearing();

            InitializeServices();   
        }

        private void MacType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (MacType.SelectedIndex == 0)
            {
                txtMacCustom.IsEnabled = false;
            }
            else
            {
                txtMacCustom.IsEnabled = true;
            }
        }
        
        private async void BtnSave_Clicked(object sender, EventArgs e)
        {
            if (_connectedDevice.State != Plugin.BLE.Abstractions.DeviceState.Connected)
            {
                Acr.UserDialogs.UserDialogs.Instance.ShowLoading("Detectando dispositivo...");

                // While loop with 30 seconds timeout
                var maxTime = DateTime.Now.AddSeconds(30);
                
                while (_connectedDevice.State != Plugin.BLE.Abstractions.DeviceState.Connected && DateTime.Now < maxTime)
                {
                    var connectParameters = new ConnectParameters(false, true);
                    await _bluetoothAdapter.ConnectToDeviceAsync(_connectedDevice, connectParameters);
                }
            }
            
            if (_connectedDevice.State == Plugin.BLE.Abstractions.DeviceState.Connected)
            {
                Acr.UserDialogs.UserDialogs.Instance.HideLoading();

                Acr.UserDialogs.UserDialogs.Instance.ShowLoading("Enviando Configuración");

                await SendConfiguration();

                SaveConfiguration();

                Acr.UserDialogs.UserDialogs.Instance.HideLoading();

                Acr.UserDialogs.UserDialogs.Instance.Toast("Configuración enviada correctamente");
            }
            else
            {
                Acr.UserDialogs.UserDialogs.Instance.HideLoading();
                await DisplayAlert("Error", "No se pudo conectar al dispositivo", "OK");
            }
        }
        
        private async void InitializeServices()
        {
            try
            {
                var test = await _connectedDevice.GetServicesAsync();
                
                var service = await _connectedDevice.GetServiceAsync(GattIdentifiers.UartGattServiceId);

                if (service != null)
                {
                    var characterictis = await service.GetCharacteristicsAsync();
                    sendCharacteristic = await service.GetCharacteristicAsync(GattIdentifiers.UartGattCharacteristicSendId);

                    receiveCharacteristic = await service.GetCharacteristicAsync(GattIdentifiers.UartGattCharacteristicReceiveId);
                    if (receiveCharacteristic != null)
                    {
                        var descriptors = await receiveCharacteristic.GetDescriptorsAsync();

                        receiveCharacteristic.ValueUpdated += (o, args) =>
                        {
                            var receivedBytes = args.Characteristic.Value;
                            XamarinEssentials.MainThread.BeginInvokeOnMainThread(() =>
                            {
                                // Convert the received bytes to hex
                                var hex = BitConverter.ToString(receivedBytes).Replace("-", string.Empty);

                                System.Diagnostics.Debug.WriteLine("TT: " + hex);

                                ShowValue(hex.Substring(0, 2), hex.Substring(2));
                                //Output.Text += hex.Substring(0, 2) + "x" + hex.Substring(2) + Environment.NewLine;  //Encoding.UTF8.GetString(receivedBytes, 0, receivedBytes.Length) + Environment.NewLine;
                            });
                        };

                        await receiveCharacteristic.StartUpdatesAsync();
                        //InitButton.IsEnabled = !(ScanButton.IsEnabled = true);
                        
                        // Let's get the configuration
                        await SendCommand("99");
                    }
                }
                else
                {
                    Output.Text += "UART GATT service not found." + Environment.NewLine;
                }
            }
            catch
            {
                Output.Text += "Error initializing UART GATT service." + Environment.NewLine;
            }
        }

        private void LoadPreviousConfiguration() {

            var rebooted = Preferences.Get("rebooted", "");
            var sleepTime = Preferences.Get("sleepTime", "");
            var advTime = Preferences.Get("advTime", "");
            var advQty = Preferences.Get("advQty", "");
            var macCustom = Preferences.Get("macCustom", "");
            var mac = Preferences.Get("mac", "");
            var macType = Preferences.Get("macType", "");
            var sensor = Preferences.Get("sensor", "");
            var intern = Preferences.Get("intern", "");
            var battery = Preferences.Get("battery", "");
            var offset = Preferences.Get("offset", "");
            var cutsensor = Preferences.Get("cutsensor", "");

            txtRebooted.Text = rebooted;
            txtSleepTime.Text = sleepTime;
            txtAdvTime.Text = advTime;
            txtAdvQty.Text = advQty;
            txtMacCustom.Text = macCustom;
            //txtMac.Text = "";
            MacType.SelectedItem = macType;
            cmbSensor.SelectedItem = sensor;
            cmbIntern.SelectedItem = intern;
            cmbBattery.SelectedItem = battery;
            txtOffset.Text = offset;
            txtcutsensor.Text = cutsensor;
        }

        private void SaveConfiguration() {
            var rebooted= txtRebooted.Text;
            var sleepTime = txtSleepTime.Text;
            var advTime = txtAdvTime.Text;
            var advQty = txtAdvQty.Text;
            var macCustom= txtMacCustom.Text;
            //txtMac.Text = "";
            var macType = MacType.SelectedItem.ToString();
            var sensor = cmbSensor.SelectedItem.ToString();
            var intern= cmbIntern.SelectedItem.ToString();
            var battery= cmbBattery.SelectedItem.ToString();
            var offset= txtOffset.Text;
            var cutsensor = txtcutsensor.Text;


            Preferences.Set("rebooted", rebooted);
            Preferences.Set("sleepTime", sleepTime);
            Preferences.Set("advTime", advTime);
            Preferences.Set("advQty", advQty);
            Preferences.Set("macCustom", macCustom);
            
            Preferences.Set("macType", macType);
            Preferences.Set("sensor", sensor);
            Preferences.Set("intern", intern);
            Preferences.Set("battery", battery);
            Preferences.Set("offset", offset);
            Preferences.Set("cutsensor", cutsensor);
        }

        private async Task SendConfiguration()
        {
            try
            {
                // Fill string with 0 to the left
                var rebooted = txtRebooted.Text.PadLeft(5, '0');
                await SendCommand("01" + rebooted);

                // Fill string with 0 to the left
                var sleep = 0;

                int.TryParse(txtSleepTime.Text, out sleep);

                if (sleep > 0)
                {
                    string hexValue = sleep.ToString().PadLeft(5, '0');
                    await SendCommand("02" + hexValue);
                }

                // Fill string with 0 to the left
                var advTime = txtAdvTime.Text.PadLeft(5, '0');
                await SendCommand("03" + advTime);

                // ADV Quantityt
                var qty = txtAdvQty.Text.PadLeft(5, '0');
                await SendCommand("04" + qty);

                // Mac configuration
                if (MacType.SelectedItem != null)
                {
                    var macType = MacType.SelectedItem.ToString();
                    var macCustom = txtMacCustom.Text;

                    if (macType == "Custom")
                    {
                        await SendCommand("22");
                        await SendCommand("20" + macCustom);
                    }

                    if (macType == "Original")
                    {
                        await SendCommand("21");
                    }
                }

                // Sensor
                if (cmbSensor.SelectedItem != null)
                {
                    var sensor = cmbSensor.SelectedItem.ToString();

                    if (sensor == "Perno 120mm")
                    {
                        await SendCommand("3010");
                    }

                    else if(sensor == "Esparrago 250")
                    {
                        await SendCommand("3011");
                    }

                    else if(sensor == "Esparrago 200")
                    {
                        await SendCommand("3012");
                    }

                    else if(sensor == "Esparrago 110 1.3mm")
                    {
                        await SendCommand("3013");
                    }

                    else
                        await SendCommand("3010");

                    //else if (sensor == "Esparrago 250")
                    //{
                    //    await SendCommand("3011");
                    //}
                    //else
                    //{
                    //    await SendCommand("3012");
                    //}
                }

                // Intern
                if (cmbIntern.SelectedItem != null)
                {
                    var intern = cmbIntern.SelectedItem.ToString();

                    if (intern == "6.2")
                    {
                        await SendCommand("3100");
                    }
                    else
                    {
                        await SendCommand("3101");
                    }
                }

                // Battery
                if (cmbBattery.SelectedItem != null)
                {
                    var battery = cmbBattery.SelectedItem.ToString();

                    if (battery == "No/Rec")
                    {
                        await SendCommand("4000");
                    }
                    else
                    {
                        await SendCommand("4001");
                    }
                }

                // Offset
                var offset = txtOffset.Text;

                int intValue = int.TryParse(offset, out intValue) ? intValue : 0;


                if (intValue > -128 && intValue < 128)
                {
                    //int newValue = intValue + 128;
                    //string message = "50" + Convert.ToInt32(newValue).ToString("X");

                    int newValue = intValue + 128;

                    string newValue_OFFSET = newValue.ToString().PadLeft(3, '0');
                    await SendCommand("50" + newValue_OFFSET);

                    
                }

                // Offset
                var cutsensor = txtcutsensor.Text;

                int intValue1 = int.TryParse(cutsensor, out intValue1) ? intValue1 : 0;
                if (intValue1 >= 0 && intValue1 <= 255)
                {

                    int newValue1 = intValue1;

                    string newValue1_cutsensor = newValue1.ToString().PadLeft(3, '0');
                    await SendCommand("51" + newValue1_cutsensor);

                  
                }






            }
            catch (Exception ex)
            {

            }
        }   
        
        private async Task SendCommand(string command)
        {
            try
            {
                if (sendCharacteristic != null)
                {
                    var bytes = await sendCharacteristic.WriteAsync(Encoding.ASCII.GetBytes($"{command}\r\n"));
                }
            }
            catch(Exception ex) { }
            {
                Output.Text += "Error sending comand to UART." + Environment.NewLine;
            }
        }
        
        private void ShowValue(string index, string value)
        {
            System.Diagnostics.Debug.WriteLine($"Indice: {index}, valor: {value}");

            switch(index)
            {
                case "01":
                    int data = ParseHex(value);
                    txtRebooted.Text = ParseHex(value).ToString();
                    break;
                case "02":
                    var scData = ParseHex(value);
                    
                    txtSleepTime.Text = Convert.ToInt32(scData/10).ToString(); 
                    break;
                case "03":
                    var trData = ParseHex(value);
                    txtAdvTime.Text = Convert.ToInt32(trData / 100).ToString();
                    break;
                case "04":
                    var data3 = ParseHex(value);
                    txtAdvQty.Text = data3.ToString();
                    break;
                case "20":
                    txtMacCustom.Text = value;

                    var macType = Preferences.Get("macType", "");

                    if (!string.IsNullOrEmpty(value) && (string.IsNullOrEmpty(macType) || macType == "Custom"))
                    {
                        MacType.SelectedItem = "Custom";
                    }
                    else {
                        MacType.SelectedItem = "Original";
                    }

                    break;
                case "21":
                    MacType.SelectedItem = "Original";

                    break;
                case "22":
                    MacType.SelectedItem = "Custom";
                    break;
                case "23":
                    txtMac.Text = value;
                    break;
                case "30":

                    if (value == "10") {
                        cmbSensor.SelectedItem = "Perno 120mm";
                    }
                    if (value == "11") {
                        cmbSensor.SelectedItem = "Esparrago 250";
                    }
                    if (value == "12")
                    {
                        cmbSensor.SelectedItem = "Esparrago 200";
                    }
                    if (value == "13")
                    {
                        cmbSensor.SelectedItem = "Esparrago 110 1.3mm";
                    }

                        
                    break;
                case "31":

                    if (value == "00")
                    {
                        cmbIntern.SelectedItem = "6.2";
                    }
                    else {
                        cmbIntern.SelectedItem = "6.8";
                    }

                    break;
                case "40":

                    if (value == "00")
                    {
                        cmbBattery.SelectedItem = "No/Rec";
                    }
                    else
                    {
                        cmbBattery.SelectedItem = "Recargable";
                    }

                    break;
                case "50":

                    // var newValue = intValue + 125;
                    //string hexValue = newValue.ToString("X");

                    var offData = ParseHex(value);
                    txtOffset.Text = Convert.ToInt32(offData - 128).ToString();
                    break;
                case "51":

                    var offData1 = ParseHex(value);
                    txtcutsensor.Text = Convert.ToInt32(offData1).ToString();
                    break;

                default:
                    break;
            }
        }

        // Parse hex to int
        private int ParseHex(string hex)
        {
            return int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
        }
    }
}