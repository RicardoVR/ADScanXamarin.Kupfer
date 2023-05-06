using Acr.UserDialogs;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content;
using XamarinEssentials = Xamarin.Essentials;
using System.Linq;
 
namespace ADScan.Client.Droid
{
    [Activity(Label = "Crea-Lab 2", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;


            base.OnCreate(savedInstanceState);
            XamarinEssentials.Platform.Init(this, savedInstanceState);
            UserDialogs.Init(this);

            CheckPermissions();
            
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());
        }

        private void CheckPermissions() {
            // Check bluetooth permissions without xamarin essentials
            var permissions = new string[]{
                Android.Manifest.Permission.Bluetooth,
                Android.Manifest.Permission.BluetoothAdmin,
                Android.Manifest.Permission.BluetoothPrivileged,
                Android.Manifest.Permission.BluetoothScan,
                Android.Manifest.Permission.BluetoothConnect,
                Android.Manifest.Permission.BluetoothAdvertise,
                //Android.Manifest.Permission.BluetoothStack,
                Android.Manifest.Permission.AccessCoarseLocation,
                Android.Manifest.Permission.AccessFineLocation
            };

            var requiredPermissions = permissions.Where(p => ContextCompat.CheckSelfPermission(this, p) == Permission.Denied);

            if (requiredPermissions.Any())
            {
                //this. .RequestPermissions(this, requiredPermissions.ToArray(), 0);
                RequestPermissions(permissions, 0);
            }

        }
        
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            XamarinEssentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}