using ADScan.Client.Data;
using ADScan.Client.Models;
using Android.App;
using Android.Companion;
using Android.Content;
using Android.Media;
using Android.Nfc;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using Plugin.BLE;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ADScan.Client.Droid
{
    [Service]
    public class BackgroundService : Service
    {
        private readonly Plugin.BLE.Abstractions.Contracts.IAdapter _bluetoothAdapter;
        public List<Models.FilterDevice> _deviceFilters;
        public List<Models.Filter> _filters;
        ADScanDatabase database = null;
        private Dictionary<string, string> messages;
        private char[] hexArray = "0123456789ABCDEF".ToCharArray();
        private PowerManager.WakeLock wakeLock;

        public BackgroundService()
        {
            _bluetoothAdapter = CrossBluetoothLE.Current.Adapter;
            _bluetoothAdapter.DeviceAdvertised += _bluetoothAdapter_DeviceAdvertised;

        }

        private async void _bluetoothAdapter_DeviceAdvertised(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            try
            {
                if (e.Device != null)
                {
                    var macAddress = (string)e.Device.NativeDevice.GetType().GetProperty("Address").GetValue(e.Device.NativeDevice);

                    System.Diagnostics.Debug.WriteLine($"Advertised: {macAddress}");

                    if (!App.GattDevices.Contains(e.Device))
                    { 
                        App.GattDevices.Add(e.Device);
                    }

                    
                    var record = e.Device.AdvertisementRecords;
                    var rawMessage = "0000";
                    var mustProceed = false;

                    foreach (var rd in record)
                    {
                        rawMessage += bytesToHex(rd.Data);
                        rawMessage += "0000";
                    }

                    if (string.IsNullOrEmpty(rawMessage))
                    {
                        return;
                    }

                    var dbDevice = _deviceFilters.FirstOrDefault(c => c.Mac == macAddress);
                    var deviceName = "";

                    if (dbDevice != null)
                    {
                        deviceName = dbDevice.Name;
                    }

                    // Filter by device
                    if (_filters != null)
                    {
                        if (_filters.Count > 0)
                        {
                            foreach (var filter in _filters)
                            {
                                if (macAddress.Contains(filter.Device) ||
                                    rawMessage.ToLower().Contains(filter.Device) ||
                                    deviceName.ToLower().Contains(filter.Device))
                                {
                                    mustProceed = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            mustProceed = true;
                        }
                    }
                    else
                    {
                        mustProceed = true;
                    }

                    //if (macAddress.ToUpper() == "FD:68:05:C7:DB:0B")
                    //{


                    if (!string.IsNullOrEmpty(rawMessage) && mustProceed)
                    {
                        if (messages.ContainsKey(macAddress))
                        {
                            if (messages[macAddress] == rawMessage)
                            {
                                return;
                            }
                        }

                        messages[macAddress] = rawMessage;

                        var deviceRow = ParseMessage(macAddress, rawMessage);

                        if (deviceRow == null)
                            return;

                        await SaveMessage(dbDevice != null ? dbDevice.Name ?? "" : "", macAddress, rawMessage);

                        MessagingCenter.Send<string>("DeviceAdvertised", "NewMessage");
                    }
                }
            }
            catch (Exception ex)
            { }
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public const int SERVICE_RUNNING_NOTIFICATION_ID = 10000;

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            var pendingIntentFlags = (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Mutable
                : PendingIntentFlags.UpdateCurrent;

            Intent mainIntent = new Intent(this, typeof(MainActivity));
            PendingIntent pendingIntent = PendingIntent.GetActivity(this, 0, mainIntent, pendingIntentFlags);

            CreateNotificationChannel();

            // Create a notification to let the user know the service is running
            var notification = new NotificationCompat.Builder(this, "ADScan")
                .SetContentTitle("ADScan Service")
                .SetContentText("The scanning is running in the background.")
                .SetSmallIcon(Resource.Drawable.icono2)
                .SetPriority(NotificationCompat.FlagForegroundService)
                .SetOngoing(true)
                .SetContentIntent(pendingIntent)
                .Build();

            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                StartForeground(SERVICE_RUNNING_NOTIFICATION_ID, notification);
            }
            else
            {
                StartService(intent);
            }

            // Perform the work in the background
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    if (database == null)
                        database = await ADScanDatabase.Instance;

                    messages = new Dictionary<string, string>();

                    var filters = await database.GetAll<Models.FilterDevice>();
                    _deviceFilters = filters.ToList();

                    var generalFilters = await database.GetAll<Models.Filter>();
                    _filters = generalFilters.ToList();

                    // TODO: Add code to perform the work in the background
                    _bluetoothAdapter.ScanTimeout = -1;
                    await _bluetoothAdapter.StartScanningForDevicesAsync();

                    // Stop the service when the work is finished
                    //StopSelf();
                }
                catch(Exception ex) 
                { 
                }
            });

            var powerManager = (PowerManager)GetSystemService(PowerService);
            wakeLock = powerManager.NewWakeLock(WakeLockFlags.Partial, "ADScan");
            wakeLock.Acquire();

            return StartCommandResult.Sticky;
        }

        public override void OnTaskRemoved(Intent rootIntent)
        {
            base.OnTaskRemoved(rootIntent);
        }

        public override bool OnUnbind(Intent intent)
        {
            return base.OnUnbind(intent);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            wakeLock.Release();
        }

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
            if (await database.IsMessageValid(deviceAddress, rawMessage))
            {

                await database.Persist<DeviceMessage>(new DeviceMessage()
                {
                    CreatedOn = DateTime.Now,
                    Device = name,
                    MacAddress = deviceAddress,
                    Raw = rawMessage
                });
            }
        }

        void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                // Notification channels are new in API 26 (and not a part of the
                // support library). There is no need to create a notification 
                // channel on older versions of Android.
                return;
            }

            var name ="ADScan";
            var description ="The service is running in the background.";
            var channel = new NotificationChannel("ADScan", name, NotificationImportance.Default)
            {
                Description = description
            };

            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.CreateNotificationChannel(channel);
        }
    }
}