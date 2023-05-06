using ADScan.Client.Data;
using ADScan.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ADScan.Client.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AddFilterPage : ContentPage
    {
        ADScanDatabase database = null;

        public AddFilterPage()
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
            if(string.IsNullOrEmpty(txtFilter.Text))
            {
                Acr.UserDialogs.UserDialogs.Instance.Alert("El filtro no puede estar vacío");
                return;
            }

            await database.Persist<Filter>(new Filter() { 
                Device = txtFilter.Text
            });

            txtFilter.Text = "";

            Acr.UserDialogs.UserDialogs.Instance.Toast("Cambios guardados!");
        }
    }
}