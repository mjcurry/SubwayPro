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
using Android.Graphics.Drawables;

namespace SubwayPro_TestApp.Droid
{
    class StepRowHolder : RecyclerView.ViewHolder
    {

        public ImageView StepIcon { get; private set; }
        public TextView StepText { get; private set; }
        public TextView StepDistanceText { get; private set; }

        public StepRowHolder(View view) : base(view)
        {
            StepIcon = view.FindViewById<ImageView>(Resource.Id.stepIcon); //Binds the imageView showing the transit mode or turn direction icon
            StepText = view.FindViewById<TextView>(Resource.Id.stepText); //Binds the textView showing the instructions for the step
            StepDistanceText = view.FindViewById<TextView>(Resource.Id.stepDistanceText); //Binds the textView showing the distance traversed in the step
        }

    }
}