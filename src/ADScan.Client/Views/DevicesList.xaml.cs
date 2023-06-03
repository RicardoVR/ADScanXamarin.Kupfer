using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using XamarinEssentials = Xamarin.Essentials;
using System.Linq;
using System.Collections.ObjectModel;
using ADScan.Client.Models;
using System.Collections;
using Xamarin.Forms.Xaml;
using ADScan.Client.Views;
using ADScan.Client.Data;
using System.IO;
using Xamarin.Forms.PlatformConfiguration;
using ADScan.Client.Renderers;
using System.Text;
using Xamarin.Essentials;
using Microsoft.AppCenter;
using static Xamarin.Essentials.Permissions;

namespace ADScan.Client
{
    public partial class DevicesList : ContentPage
    {
        ADScanDatabase database = null;
        private readonly IAdapter _bluetoothAdapter;
        private List<IDevice> _gattDevices = new List<IDevice>();
        public List<Models.FilterDevice> _deviceFilters;
        public List<Models.Filter> _filters;
        private Dictionary<string, string> messages;
        private bool pauseCapturing = false;
        public static ObservableCollection<Models.Device> _devices;
        public ObservableCollection<Models.Device> Devices
        {
            get { return _devices; }
            set
            {
                _devices = value;
                    OnPropertyChanged("Devices");
                
            }
        }

        public DevicesList()
        {
            InitializeComponent();

            _devices = new ObservableCollection<Models.Device>();

            _bluetoothAdapter = CrossBluetoothLE.Current.Adapter;

            btnMassive.Clicked += BtnMassive_Clicked;
            messages = new Dictionary<string, string>();
        }

        private async void BtnMassive_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MassiveConfiguration());
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            if (database == null)
                database = await ADScanDatabase.Instance;

            await PermissionsGrantedAsync();

            var filters = await database.GetAll<Models.FilterDevice>();
            _deviceFilters = filters.ToList();

            var generalFilters = await database.GetAll<Models.Filter>();
            _filters = generalFilters.ToList();

            if (App.IsScanning || DependencyService.Get<IBackgroundService>().IsRunning("BackgroundService"))
            {
                ScanButton.Text = "Detener";

                try
                {
                    DependencyService.Get<IBackgroundService>().Stop();
                    DependencyService.Get<IBackgroundService>().Start();
                }
                catch (Exception e) { }
            }
            else
            {
                ScanButton.Text = "Escanear";
            }

            MessagingCenter.Unsubscribe<string>("DeviceAdvertised", "NewMessage");

            MessagingCenter.Subscribe<string>("DeviceAdvertised", "NewMessage", async (sender) =>
             {
                 await LoadData();
             });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            MessagingCenter.Unsubscribe<string>("DeviceAdvertised", "NewMessage");
        }

        protected async void OnFiltersClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new FiltersPage());
        }

        protected async void OnDvcFiltersClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new DeviceFilterPage());
        }

        private async Task<bool> PermissionsGrantedAsync()
        {
            var locationPermissionStatus = await XamarinEssentials.Permissions.CheckStatusAsync<XamarinEssentials.Permissions.LocationWhenInUse>();

            if (locationPermissionStatus != XamarinEssentials.PermissionStatus.Granted)
            {
                var status = await XamarinEssentials.Permissions.RequestAsync<XamarinEssentials.Permissions.LocationWhenInUse>();
                return status == XamarinEssentials.PermissionStatus.Granted;
            }
            return true;
        }

        private async void ScanButton_Clicked(object sender, EventArgs e)
        {
            // Check if bluetooth is enabled
            if(CrossBluetoothLE.Current.State == BluetoothState.Off)
            {
                await DisplayAlert("Bluetooth", "Bluetooth is not enabled", "OK");
                return;
            }

            if (ScanButton.Text == "Escanear")
            {
                ScanButton.Text = "Detener";
                App.IsScanning = true;
            }
            else
            {
                ScanButton.Text = "Escanear";
                App.IsScanning = false;
            }

            IsBusyIndicator.IsVisible = ScanButton.Text == "Detener";

            if (ScanButton.Text == "Escanear")
            {
                DependencyService.Get<IBackgroundService>().Stop();
                return;
            }

            foreach (var device in _bluetoothAdapter.ConnectedDevices)
                _gattDevices.Add(device);

            //_bluetoothAdapter.ScanTimeout = -1;
            //await _bluetoothAdapter.StartScanningForDevicesAsync();
            DependencyService.Get<IBackgroundService>().Start();

            //foundBleDevicesListView.ItemsSource = _devices.ToArray();
            var ob2list = _devices.ToList();
            Devices = new System.Collections.ObjectModel.ObservableCollection<Models.Device>(ob2list);

            //if (ScanButton.Text == "Escanear")
            //    ScanButton.Text = "Detener";
            //else
            //    ScanButton.Text = "Escanear";

            //IsBusyIndicator.IsVisible = ScanButton.Text == "Detener";
        }

        private async void SendButton_Clicked(object sender, EventArgs e)
        {
            pauseCapturing = true;

            var data = await database.GetAll<DeviceMessage>();
            
            if (data.Count == 0)
            {
                Acr.UserDialogs.UserDialogs.Instance.Toast("Sin datos para enviar!");
                return;
            }

            await database.ClearMessages();
            await database.ClearMessagessServer();

            try {

                string fileName = "LocalData.csv";
                //string libraryPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal); ;
                ////var path = Path.Combine(libraryPath, sqliteFilename);
                //string filePath = Path.Combine(libraryPath, fileName);
                 string filePath = DependencyService.Get<IFileAccessHelper>().GetLocalFilePath(fileName);
                var exportRows = new List<DataRow>();


                string[] columns = { "Id", "Created On", "Device", "MacAddress", "Raw" };

                string separator = ",";
                StringBuilder output = new StringBuilder();

                output.AppendLine(string.Join(separator, columns));

                var previousRaw = "";
                var dataList = new List<string>();

                foreach (var device in data)
                {
                    if(device.Raw == previousRaw || dataList.Contains($"{device.MacAddress}-{device.CreatedOn.ToString("ddMMyyyyHHmmss")}-{device.Raw}"))
                    {
                        continue;
                    }

                    // Generate csv line
                    string[] newLine =
                        {
                                device.ID.ToString(),
                                device.CreatedOn.ToString("yyyy/MM/dd HH:mm:ss"),
                                device.MacAddress,
                                device.Raw
                        };

                    output.AppendLine(string.Join(separator, newLine));

                    // Generate line to send to web server
                    var deviceRow = ParseMessage(device.MacAddress, device.Raw);

                    exportRows.Add(
                        new DataRow()
                        {
                            Adc1 = deviceRow.V1,
                            Adc2 = deviceRow.V2,
                            Bate = "",
                            Cont = int.Parse(deviceRow.Number),
                            Date = device.CreatedOn.ToString("dd/MM/yyyy"),
                            Desg = deviceRow.Desg,
                            Hour = device.CreatedOn.ToString("HH:mm:ss"),
                            Listened = 0,
                            Mac = device.MacAddress,
                            Temp = 0,
                            Type = 10
                        }
                    );

                    previousRaw = device.Raw;
                    dataList.Add($"{device.MacAddress}-{device.CreatedOn.ToString("ddMMyyyyHHmmss")}-{device.Raw}");
                }

                var exportData = new DataRequest();
                exportData.Data = exportRows.ToArray();
                exportData.Type = "sensor_batch";

                var response = await NetworkUtils.PostAsync<DataResponse>("conflux/api/externallist", exportData, "");

                SendButton.Text = $"Enviar";

                try
                {
                    var result = await Acr.UserDialogs.UserDialogs.Instance.ConfirmAsync(new Acr.UserDialogs.ConfirmConfig()
                    {
                        Title = "Desea generar y compartir el CSV?"
                    });


                    if (result)
                    {
                        File.AppendAllText(filePath, output.ToString());

                        await Share.RequestAsync(new ShareFileRequest
                        {
                            Title = $"LocalData_{DateTime.Now.ToString("yyyyMMddHHmmss")}.csv",
                            File = new ShareFile(filePath)
                        });
                    }

                    pauseCapturing = false;
                }
                catch (Exception ex)
                {
                    Acr.UserDialogs.UserDialogs.Instance.Alert("Error: " + ex.Message);
                    pauseCapturing = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                Acr.UserDialogs.UserDialogs.Instance.Alert("Error: " + ex.Message);
            }

            pauseCapturing = false;
            return;

          
        }

        private async void FoundBluetoothDevicesListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (ScanButton.Text == "Escanear")
                ScanButton.Text = "Detener";
            else
                ScanButton.Text = "Escanear";

            IsBusyIndicator.IsVisible = ScanButton.Text == "Detener";

            Models.Device selectedItem = e.Item as Models.Device;
            
            var test = App.GattDevices.Where(c =>
                c.NativeDevice.GetType().GetProperty("Address").GetValue(c.NativeDevice).ToString() == selectedItem.OriginalMac).FirstOrDefault();

            if (test == null)
            {
                await Acr.UserDialogs.UserDialogs.Instance.AlertAsync("No se pudo conectar");
                return;
            }

            if (test.State == DeviceState.Connected)
            {
                await Navigation.PushAsync(new DeviceConfiguration(test));
            }
            else
            {
                try
                {
                    var connectParameters = new ConnectParameters(false, true);
                    await _bluetoothAdapter.ConnectToDeviceAsync(test, connectParameters);
                    await Navigation.PushAsync(new DeviceConfiguration(test));
                }
                catch
                {
                    await DisplayAlert("Error connecting", $"Error connecting to BLE device: {test.Name ?? "N/A"}", "Retry");
                }
            }

            if (ScanButton.Text == "Escanear")
                ScanButton.Text = "Detener";
            else
                ScanButton.Text = "Escanear";

            IsBusyIndicator.IsVisible = ScanButton.Text == "Detener";
        }

        private char[] hexArray = "0123456789ABCDEF".ToCharArray();
        
        private string bytesToHex(byte[] bytes)
        {
            char[] hexChars = new char[bytes.Length * 2];
            
            for (int j = 0; j < bytes.Length; j++)
            {
                int v = bytes[j] & 0xFF;
                //hexChars[j * 2] = hexArray[v >>> 4];
                hexChars[j * 2] = hexArray[unchecked((int)((uint)v >> 4))];
                
                hexChars[j * 2 + 1] = hexArray[v & 0x0F];
            }
            return new String(hexChars);
        }

        private Models.Device ParseMessage(string deviceAddress, string rawMessage)
        {

            // Si es 10 (HEX) 16 (DEC) es medidor de espesor (esparrago 250 mm)
            // Si es 11 (HEX) 16 (DEC) es medidor de espesor (esparrago 200 mm )
            // Si es 12 (HEX) 16 (DEC) es medidor de espesor (Perno 120 mm )

            try
            {
                // Contador ADV
                int ContadorIndex = ((9 * 2) - 2);
                String ContadorHex = rawMessage.Substring(ContadorIndex, 4);
                var ContadorValue = ParseHex(ContadorHex); // = Int16.Parse(pBarHex);
                
                var Contador = ContadorValue.ToString();

                // ADC_1
                int ADC_1_Index = ((11 * 2) - 2);
                string ADC_1_Hex = rawMessage.Substring(ADC_1_Index, 4);
                int ADC_1_Value = ParseHex(ADC_1_Hex);

                var ADC_1 = ADC_1_Value.ToString();

                // ADC_2
                int ADC_2_Index = ((13 * 2) - 2);
                string ADC_2_Hex = rawMessage.Substring(ADC_2_Index, 4);
                int ADC_2_Value = ParseHex(ADC_2_Hex);

                var ADC_2 = ADC_2_Value.ToString();

                // Status
                int statusIndex = ((11 * 2) - 2);
                String statusHex = rawMessage.Substring(statusIndex, 4);
                int statusValue = ParseHex(statusHex);
                string statusText = "";

                //statusText = "Revisar";

                int DesgasteIndex = ((21 * 2) - 2);
                String DesgasteHex = rawMessage.Substring(DesgasteIndex, 2);
                int DesgasteValue = ParseHex(DesgasteHex); //Integer.parseInt(DesgasteHex, 16);

                if (DesgasteValue >= 254)
                { statusText = "Revisar"; }
                else
                {
                    statusText = DesgasteValue.ToString() + "mm";
                }

                if (ADC_1_Value <= 147 && ADC_2_Value <= 147)
                {
                    statusText = "Sin Sensor";
                }
                if (ADC_1_Value > 147 && ADC_2_Value <= 147)
                {
                    statusText = "Mal Conectado";
                }
                if (ADC_1_Value <= 147 && ADC_2_Value > 147)
                {
                    statusText = "Mal Conectado";
                }

                var status = statusText;

                return new Models.Device()
                {
                    Mac = deviceAddress,
                    Number = Contador,
                    V1 = ADC_1,
                    V2 = ADC_2,
                    MM = status,
                    Desg = DesgasteValue,
                    RAW_ADV = rawMessage
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private int ParseHex(string hex)
        {
            return int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
        }

        private async Task SaveMessage(string name, string deviceAddress, string rawMessage)
        {
            await database.Persist<DeviceMessage>(new DeviceMessage() { 
                CreatedOn = DateTime.Now,
                Device = name,
                MacAddress = deviceAddress,
                Raw = rawMessage
            });

            await database.Persist<DeviceMessageList>(new DeviceMessageList()
            {
                CreatedOn = DateTime.Now,
                Device = name,
                MacAddress = deviceAddress,
                Raw = rawMessage
            });
        }

        private bool isLoading = false;

        private async Task LoadData()
        {
            if(isLoading) return;

            isLoading = true;

            try
            {
                var messages = await database.GetAll<Models.DeviceMessage>();

                var data = messages.GroupBy(l => l.MacAddress)
                .Select(g => g.OrderByDescending(c => c.CreatedOn).FirstOrDefault())
                .ToList();

                foreach (var message in data)
                {
                    var deviceRow = ParseMessage(message.Device, message.Raw);

                    if (deviceRow == null)
                        return;

                    var device = _devices.Where(c => c.OriginalMac == message.MacAddress).FirstOrDefault();

                    if (device == null)
                    {
                        deviceRow.Mac = !string.IsNullOrEmpty(message.Device) ? message.Device : message.MacAddress;
                        deviceRow.OriginalMac = message.MacAddress;
                        _devices.Add(deviceRow);
                    }
                    else
                    {
                        device.Number = deviceRow.Number;
                        device.V1 = deviceRow.V1;
                        device.V2 = deviceRow.V2;
                        device.MM = deviceRow.MM;
                    }
                }

                Devices = new ObservableCollection<Models.Device>(_devices);
                foundBleDevicesListView.ItemsSource = Devices;

                var dbMessages = await database.GetAll<DeviceMessage>();
                var messagesToSend = dbMessages.Select(c => $"{c.MacAddress}-{c.CreatedOn.ToString("ddMMyyyyHHmmss")}-{c.Raw}").Distinct().ToList();

                SendButton.Text = $"Enviar ({messagesToSend.Count})";
            }
            catch (Exception ex) 
            { 
            }

            isLoading = false;
        }
    }
}

