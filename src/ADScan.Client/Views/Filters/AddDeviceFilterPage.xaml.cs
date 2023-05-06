using ADScan.Client.Data;
using ADScan.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ADScan.Client.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AddDeviceFilterPage : ContentPage
    {
        ADScanDatabase database = null;
        Regex validateMacAddressRegex = new Regex("^(?:[0-9A-Fa-f]{2}[:-]){5}(?:[0-9A-Fa-f]{2})$");
        public AddDeviceFilterPage()
        {
            InitializeComponent();

            btnAdd.Clicked += BtnAdd_Clicked;
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            if (database == null)
                database = await ADScanDatabase.Instance;
        }

        private async void BtnAdd_Clicked(object sender, EventArgs e)
        {
            if(string.IsNullOrEmpty(txtName.Text) || string.IsNullOrEmpty(txtMac.Text))
            {
                Acr.UserDialogs.UserDialogs.Instance.Alert("Todos los datos son requeridos");
                return;
            }

            if (!validateMacAddressRegex.IsMatch(txtMac.Text))
            {
                Acr.UserDialogs.UserDialogs.Instance.Alert("No es una MAC válida!");
                return;
            }

            await database.Persist<FilterDevice>(new FilterDevice() { 
                Name = txtName.Text,
                Mac = txtMac.Text
            });

            txtName.Text = "";
            txtMac.Text = "";

            Acr.UserDialogs.UserDialogs.Instance.Toast("Cambios guardados!");
        }
    }
}