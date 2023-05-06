using Xamarin.Forms;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Crashes;
using Microsoft.AppCenter.Analytics;
using ADScan.Client.Data;
using System;
using ADScan.Client.Workers;
using Plugin.BLE.Abstractions.Contracts;
using System.Collections.Generic;

namespace ADScan.Client
{
    public partial class App : Application
    {
        //static ADScanDatabase database;
        public static bool IsScanning = false;
        public static List<IDevice> GattDevices = new List<IDevice>();

        public App()
        {
            InitializeComponent();
            var navigationPage = new NavigationPage(new MainPage()) {
                BarTextColor = Color.Gray
            };
            MainPage = navigationPage;
        }

        protected async override void OnStart()
        {
            AppCenter.Start("android=73fc8695-e6b8-4ee7-9981-e4cfe83e132c;", 
                  //"ios=73fc8695-e6b8-4ee7-9981-e4cfe83e132c;",
                  typeof(Analytics), typeof(Crashes));          
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }

        
    }
}