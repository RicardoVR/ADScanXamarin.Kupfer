using ADScan.Client.Droid;
using ADScan.Client.Renderers;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: Xamarin.Forms.Dependency(typeof(FileAccessHelper))]
namespace ADScan.Client.Droid
{
    public class FileAccessHelper : IFileAccessHelper
    {
        public string GetLocalFilePath(string filename)
        {
            //var downloadsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
            var downloadsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);

            return System.IO.Path.Combine(downloadsPath, filename);
        }
    }
}