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
using Android.Support.V7.Widget;

namespace SubwayPro_TestApp.Droid
{
    public class NavigationLayoutManager : LinearLayoutManager
    {
        //Custom field used to allow vertical scrolling to be enabled and disabled
        public Boolean scrollable { get; set; }
        
        public NavigationLayoutManager(Context context) : base(context) { }

        public override bool CanScrollVertically()
        {
            return base.CanScrollVertically() && scrollable;
        }

    }
}