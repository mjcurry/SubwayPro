using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace SubwayPro_TestApp.Droid
{
    [Activity(Label = "LayoutTestActivity")]
    public class LayoutTestActivity : Activity
    {
        //Class used to test layouts before implementing them in the main program
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            RequestWindowFeature(WindowFeatures.NoTitle);
            SetContentView(Resource.Layout.TestLayout);
            // Create your application here
        }
    }
}