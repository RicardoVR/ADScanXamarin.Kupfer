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
using Xamarin.Essentials;
using ADScan.Client.Views.Server;

namespace ADScan.Client
{
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
       
        public MainPage()
        {
            InitializeComponent();
            //btnSmartServer
            btnSmartWear.Clicked += BtnSmartWear_Clicked;
            btnSmartServer.Clicked += BtnSmartServer_Clicked;
        }

        private async void BtnSmartWear_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new DevicesList());
        }

        private async void BtnSmartServer_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ServerPage());
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            await CheckBluetoothPermissions();

            lblVersion.Text = $"{AppInfo.VersionString}";
        }

        // Method to check Bluetooth permissions
        private async Task<bool> CheckBluetoothPermissions()
        {
            var status = await XamarinEssentials.Permissions.CheckStatusAsync<XamarinEssentials.Permissions.LocationWhenInUse>();
                        
            if (status != XamarinEssentials.PermissionStatus.Granted)
            {
                var results = await XamarinEssentials.Permissions.RequestAsync<XamarinEssentials.Permissions.LocationWhenInUse>();
                status = results;
            }

            status = await XamarinEssentials.Permissions.CheckStatusAsync<XamarinEssentials.Permissions.StorageWrite>();

            if (status != XamarinEssentials.PermissionStatus.Granted)
            {
                var results = await XamarinEssentials.Permissions.RequestAsync<XamarinEssentials.Permissions.StorageWrite>();
                status = results;
            }

            return status == XamarinEssentials.PermissionStatus.Granted;
        }
    }
}