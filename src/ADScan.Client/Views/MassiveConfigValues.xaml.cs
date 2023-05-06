using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ADScan.Client.Data;
using ADScan.Client.Models;
using Xamarin.Forms;

namespace ADScan.Client.Views
{
    public partial class MassiveConfigValues : ContentPage
    {
        ADScanDatabase database = null;

        public MassiveConfigValues()
        {
            InitializeComponent();

            btnSave.Clicked += BtnSave_Clicked;
            btnClose.Clicked += BtnClose_Clicked;
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            await Load();
        }

        private async void BtnClose_Clicked(object sender, EventArgs e)
        {
            await Application.Current.MainPage.Navigation.PopAsync();
        }

        private async void BtnSave_Clicked(object sender, EventArgs e)
        {
            await SaveConfiguration();

            Acr.UserDialogs.UserDialogs.Instance.Alert("Cambios Guardados!");
        }

        private async Task Load()
        {

            if (database == null)
                database = await ADScanDatabase.Instance;

            var data = await database.GetAll<Models.DeviceConfiguration>();

            foreach (var dt in data)
            {
                if (!dt.Enabled)
                    continue;

                if (dt.Index == "01")
                {
                    chkRebooted.IsChecked = true;
                    txtRebooted.Text = dt.Value;
                }

                if (dt.Index == "02")
                {
                    chkSleepTime.IsChecked = true;
                    txtSleepTime.Text = dt.Value;
                }

                if (dt.Index == "03")
                {
                    chkAdvTime.IsChecked = true;
                    txtAdvTime.Text = dt.Value;
                }

                if (dt.Index == "04")
                {
                    chkAdvQty.IsChecked = true;
                    txtAdvQty.Text = dt.Value;
                }

                if (dt.Index == "20")
                {
                    chkMacType.IsChecked = true;

                    if (dt.Value == "22")
                    {
                        MacType.SelectedItem = "Custom";
                    }
                    else {
                        MacType.SelectedItem = "Original";
                    }
                }

                if (dt.Index == "30")
                {
                    chkSensor.IsChecked = true;

                    if (dt.Value == "10")
                    {
                        cmbSensor.SelectedItem = "Perno 120mm";
                    }
                    else if (dt.Value == "30")
                    {
                        cmbSensor.SelectedItem = "Esparrago 250";
                    }
                    else
                    {
                        cmbSensor.SelectedItem = "Esparrago 200";
                    }
                }

                // Intern

                if (dt.Index == "31")
                {
                    chkIntern.IsChecked = true;

                    if (dt.Value == "00")
                    {
                        cmbIntern.SelectedItem = "6.2";
                    }
                    else {
                        cmbIntern.SelectedItem = "6.8";
                    }
                }

                // Battery

                if (dt.Index == "40")
                {
                    chkBattery.IsChecked = true;

                    if (dt.Value == "00")
                    {
                        cmbBattery.SelectedItem = "No/Rec";
                    }
                    else {
                        cmbBattery.SelectedItem = "Recargable";
                    }
                }

                // Offset
                if (dt.Index == "50")
                {
                    chkOffset.IsChecked = true;
                    txtOffset.Text = dt.Value;
                }
            }
        }

        private async Task SaveConfiguration() {
            // Fill string with 0 to the left
            var rebooted = txtRebooted.Text;
            await SaveValue("01" , rebooted, chkRebooted.IsChecked);

            var sleepTime = txtSleepTime.Text;
            await SaveValue("02", sleepTime, chkSleepTime.IsChecked);

            var advTime = txtAdvTime.Text;
            await SaveValue("03", advTime, chkAdvTime.IsChecked);

            // ADV Quantityt
            var qty = txtAdvQty.Text;
            await SaveValue("04" ,qty, chkAdvQty.IsChecked);
            
            // Mac configuration
            var macType = MacType.SelectedItem != null ? MacType.SelectedItem.ToString() : "";

            if (macType == "Custom")
            {
                await SaveValue("20", "22", chkMacType.IsChecked);
            }
            else {
                await SaveValue("20", "21", chkMacType.IsChecked);
            }
            
            // Sensor
            var sensor = cmbSensor.SelectedItem != null ? cmbSensor.SelectedItem.ToString() : "";

            if (sensor == "Perno 120mm")
            {
                await SaveValue("30","10", chkSensor.IsChecked);
            }
            else if (sensor == "Esparrago 250")
            {
                await SaveValue("30","30", chkSensor.IsChecked);
            }
            else
            {
                await SaveValue("30","20", chkSensor.IsChecked);
            }

            // Intern
            var intern = cmbIntern.SelectedItem != null ? cmbIntern.SelectedItem.ToString() : "";

            if (intern == "6.2")
            {
                await SaveValue("31","00", chkIntern.IsChecked);
            }
            else
            {
                await SaveValue("31","01", chkIntern.IsChecked);
            }

            // Battery
            var battery = cmbBattery.SelectedItem != null ? cmbBattery.SelectedItem.ToString() : "";

            if (battery == "No/Rec")
            {
                await SaveValue("40","00", chkBattery.IsChecked);
            }
            else
            {
                await SaveValue("40","01", chkBattery.IsChecked);
            }

            // Offset
            var offset = txtOffset.Text;

            int intValue = int.TryParse(offset, out intValue) ? intValue : 0;

            if (intValue > -128 && intValue < 128)
            {

                await SaveValue("50", intValue.ToString(), chkOffset.IsChecked);
            }
        }

        private async Task SaveValue(string index, string value, bool enabled)
        {
            if (string.IsNullOrEmpty(value))
                return;

            var dbValue = await database.GetConfiguration(index);

            if (dbValue == null)
            {
                if (enabled)
                {
                    dbValue = new Models.DeviceConfiguration()
                    {
                        Index = index,
                        Value = value,
                        Enabled = enabled
                    };

                    await database.Persist<Models.DeviceConfiguration>(dbValue);
                }
            }
            else
            {
                if (enabled)
                {
                    dbValue.Value = value;
                    await database.Update<Models.DeviceConfiguration>(dbValue);
                }
                else { 
                    await database.Delete<Models.DeviceConfiguration>(dbValue);   
                }
            }            
        }
    }
}

