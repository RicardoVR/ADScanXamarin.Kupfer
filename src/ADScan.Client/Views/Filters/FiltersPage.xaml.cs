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
    public partial class FiltersPage : ContentPage
    {
        ADScanDatabase database = null;

        public static ObservableCollection<Models.Filter> _filters;
        public ObservableCollection<Models.Filter> Filters
        {
            get { return _filters; }
            set
            {
                _filters = value;
                OnPropertyChanged("Filters");
            }
        }

        public FiltersPage()
        {
            InitializeComponent();

            btnAdd.Clicked += delegate
            {
                Navigation.PushAsync(new AddFilterPage());
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
            Label label = (Label)listViewItem.Children[0];

            string text = label.Text;

            await  database.DeleteFilterByName(text);
            await LoadData();

            Acr.UserDialogs.UserDialogs.Instance.Toast("Cambios guardados!");
        }

        private async Task LoadData()
        {
            var filters = await database.GetAll<Models.Filter>();
            Filters = new ObservableCollection<Models.Filter>(filters);
            filterList.ItemsSource = Filters;
        }
    }
}