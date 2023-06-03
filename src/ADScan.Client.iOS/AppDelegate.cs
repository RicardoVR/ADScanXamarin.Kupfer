using Foundation;
using UIKit;
using SQLite;
using Xamarin.Forms;
using ADScan.Client.Renderers;

namespace ADScan.Client.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init();
            global::Xamarin.Forms.FormsMaterial.Init();
            Xamarin.Forms.DependencyService.Register<IFileAccessHelper>();
            Xamarin.Forms.DependencyService.Register<IBackgroundService>();
            

          //  DependencyService.Register<IFileAccessHelper, DeviceOrientationService>();
            LoadApplication(new App());

            return base.FinishedLaunching(app, options);
        }
    }
}