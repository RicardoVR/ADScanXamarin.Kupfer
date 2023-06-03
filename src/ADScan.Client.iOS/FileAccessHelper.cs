using System;
using ADScan.Client.Renderers;

//[assembly: Xamarin.Forms.Dependency(typeof(FileAccessHelper))]
[assembly: Xamarin.Forms.Dependency(typeof(ADScan.Client.iOS.FileAccessHelper))]
namespace ADScan.Client.iOS
{

    public class FileAccessHelper : IFileAccessHelper
    {
        public string GetLocalFilePath(string filename)
        {

            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal); // Documents folder
            string libraryPath = System.IO.Path.Combine(documentsPath, "..", "Library");
            //var downloadsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
            //  var downloadsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);

            return libraryPath;
        }
    }
}

