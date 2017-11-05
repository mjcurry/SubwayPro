using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Util;

using Android.Support.V7.App;
using System.Threading.Tasks;
using Android.Content;

namespace SubwayPro_TestApp.Droid
{
	[Activity (Label = "NYC Subwayz", Icon = "@drawable/logo", Theme="@style/MainTheme", MainLauncher = false, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, NoHistory = true)]
	public class MainActivity : Activity
	{
        static readonly string TAG = "X:" + typeof(MainActivity).Name;
        protected override void OnCreate (Bundle bundle)
		{   

			base.OnCreate (bundle);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            StartActivity(typeof(MapActivity));
            //Comment out the above and uncomment in the below to test layouts
            //StartActivity(typeof(LayoutTestActivity));
		}

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
        }

    }
}

