using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ADScan.Client.Models;
using Xamarin.Forms;
using System.Linq;
using System.Text.RegularExpressions;
using ADScan.Client.Data;
using System.Threading.Tasks;
using ADScan.Client.ViewModels;
using ADScan.Client.Views;
using ADScan.Client.Workers;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE;

namespace ADScan.Client
{
    public partial class MassiveConfiguration : ContentPage
    {

        public static DeviceWorker worker;
        private bool mustStop = false;
        Regex validateMacAddressRegex = new Regex("^(?:[0-9A-Fa-f]{2}[:-]){5}(?:[0-9A-Fa-f]{2})$");

        public MassiveConfiguration()
        {
            InitializeComponent();

            this.BindingContext = new ConfigurationViewModel();

            btnConfig.Clicked += BtnConfig_Clicked;
            ScanButton.Clicked += ScanButton_Clicked;
            ExitButton.Clicked += ExitButton_Clicked;
            txtMac.TextChanged += TxtMac_TextChanged;
            
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            MessagingCenter.Unsubscribe<string>("DeviceConfigured", "NewMessage");

            MessagingCenter.Subscribe<string>("DeviceConfigured", "NewMessage", async (sender) =>
            {
                await ((ConfigurationViewModel)this.BindingContext).Load();
            });

            mustStop = false;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            mustStop = true;
        }

        private void TxtMac_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (e.NewTextValue != e.OldTextValue)
            {
                // Clean special characters from string only maintain letters, numbers and ":"
                var mac = Regex.Replace(e.NewTextValue.ToUpper(), @"[^a-zA-Z0-9:]", "");

                // Put a ":" every two characters that are numbers or letters
                mac = Regex.Replace(mac, @"([a-zA-Z0-9]{2})", "$1:").Replace("::", ":");

                //mac = Regex.Replace(mac, @"(.{2})", "$1:").Replace("::", ":");

                if (mac.Length >= 17)
                {
                    if (mac[mac.Length - 1] == ':')
                    {
                        mac = mac.Substring(0, mac.Length - 1);
                    }
                }

                if (txtMac.Text != mac)
                    (sender as Entry).Text = mac;
            }
        }

        private async void ExitButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void ScanButton_Clicked(object sender, EventArgs e)
        {
            if (CrossBluetoothLE.Current.State == BluetoothState.Off)
            {
                
                await DisplayAlert("Bluetooth", "Bluetooth is not enabled", "OK");
                return;
            }
            
            if (ScanButton.Text == "Escaneando...")
            {
                ScanButton.Text = "Escanear";
                return;
            }

            ScanButton.Text = "Escaneando...";
            
            var database = await ADScanDatabase.Instance;

            var data = await database.GetAll<Models.DeviceConfiguration>();

            if (data.Count > 0)
            {
                worker = new DeviceWorker();
                Xamarin.Forms.Device.StartTimer(TimeSpan.FromMilliseconds(200), Sync);
            }
        }

        private async void BtnConfig_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MassiveConfigValues());
        }

        private bool Sync()
        {
            if (ScanButton.Text == "Escanear" || mustStop)
            {
                return false;
            }
            
            worker.CheckConfiguration().ConfigureAwait(false);

            return true;
        }
    }
}

