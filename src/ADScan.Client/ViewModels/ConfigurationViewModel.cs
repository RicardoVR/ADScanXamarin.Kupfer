using System;
using ADScan.Client.Data;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Xamarin.Forms;
using ADScan.Client.Models;
using System.Linq;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ADScan.Client.ViewModels
{
    public class ConfigurationViewModel : INotifyPropertyChanged
    {
        Regex validateMacAddressRegex = new Regex("^(?:[0-9A-Fa-f]{2}[:-]){5}(?:[0-9A-Fa-f]{2})$");
        ADScanDatabase database = null;

        private string _mac;
        public string Mac
        {
            get {
                return _mac;
            }

            set {
                _mac = value;
                OnPropertyChanged("Mac");
            }
        }

        public Command AddDeviceCommand { get; set; }
        public Command<string> DeleteDeviceCommand { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public static ObservableCollection<Models.MassiveDevice> _devices;
        public ObservableCollection<Models.MassiveDevice> Devices
        {
            get { return _devices; }
            set
            {
                _devices = value;
                OnPropertyChanged("Devices");

            }
        }

        public ConfigurationViewModel()
        {
            Devices = new ObservableCollection<MassiveDevice>();

            DeleteDeviceCommand = new Command<string>(Delete);
            AddDeviceCommand = new Command(Add);

            Load().ConfigureAwait(false);
        }

        public async Task Load() {

            if(database == null)
                database = await ADScanDatabase.Instance;

            var data = await database.GetAll<MassiveDevice>();

            Devices = new ObservableCollection<MassiveDevice>(data);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void Add()
        {
            var mac = Mac ?? "";


            if (!validateMacAddressRegex.IsMatch(mac))
            {
                Acr.UserDialogs.UserDialogs.Instance.Alert("No es una MAC válida!");
                return;
            }

            if (_devices.Count(c => c.Address == mac) == 0)
            {
                var device = new MassiveDevice()
                {
                    Address = mac
                };

                _devices.Add(device);

                await database.Persist<MassiveDevice>(device);

                await Load();

                Mac = "";
            }
        }

        public async void Delete(string address)
        {
            var device = await database.GetDevice(address);

            if (device != null)
            {
                await database.Delete<MassiveDevice>(device);

                await Load();
            }
        }
    }
}

