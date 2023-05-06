using ADScan.Client.Droid;
using ADScan.Client.Renderers;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Plugin.BLE.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: Dependency(typeof(BackgroundServiceImplementation))]
namespace ADScan.Client.Droid
{
    public class BackgroundServiceImplementation : IBackgroundService
    {
        public bool IsRunning(string name)
        {
            // Obtener el contexto de Android
            Context context = Android.App.Application.Context;
            // Obtener el servicio de actividad
            ActivityManager manager = (ActivityManager)context.GetSystemService(Context.ActivityService);
            // Iterar sobre la lista de servicios que se están ejecutando
            foreach (ActivityManager.RunningServiceInfo service in manager.GetRunningServices(int.MaxValue))
            {
                if (service.Service.ClassName.Contains(name))
                {
                    return true;
                }
            }

            return false;
        }

        public void Start()
        {
            var intent = new Intent(Android.App.Application.Context, typeof(BackgroundService));
            Android.App.Application.Context.StartForegroundService(intent);
        }

        public void Stop()
        {
            //StopService(new Intent(this, typeof(DemoService));
            Android.App.Application.Context.StopService(new Intent(Android.App.Application.Context, typeof(BackgroundService)));
        }
    }
}