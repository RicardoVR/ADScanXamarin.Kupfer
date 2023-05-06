using ADScan.Client.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ADScan.Client.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DeviceFilterPage : ContentPage
    {
        ADScanDatabase database = null;

        public static ObservableCollection<Models.FilterDevice> _filters;
        public ObservableCollection<Models.FilterDevice> Filters
        {
            get { return _filters; }
            set
            {
                _filters = value;
                OnPropertyChanged("Filters");
            }
        }

        public DeviceFilterPage()
        {
            InitializeComponent();

            btnAdd.Clicked += delegate
            {
                Navigation.PushAsync(new AddDeviceFilterPage());
            };
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            if (database == null)
                database = await ADScanDatabase.Instance;

            await LoadData();
        }

        public async void DeleteItem(object sender, EventArgs args)
        {
            ImageButton button = (ImageButton)sender;
            StackLayout listViewItem = (StackLayout)button.Parent;

            if (button.CommandParameter == null)
                return;

            var mac = button.CommandParameter.ToString();

            if (!string.IsNullOrEmpty(mac))
            {
                await database.DeleteDeviceFilterByMac(mac);
                await LoadData();

                Acr.UserDialogs.UserDialogs.Instance.Toast("Cambios guardados!");
            }
        }

        private async Task LoadData()
        {
            var filters = await database.GetAll<Models.FilterDevice>();
            Filters = new ObservableCollection<Models.FilterDevice>(filters);
            filterList.ItemsSource = Filters;
        }
    }
}