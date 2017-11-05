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
    class NavigationInstructionViewHolder : RecyclerView.ViewHolder
    {

        public ImageView Icon { get; private set; }
        public TextView Instructions { get; private set; }

        public NavigationInstructionViewHolder(View view): base(view)
        {
            Icon = view.FindViewById<ImageView>(Resource.Id.actionIcon);
            Instructions = view.FindViewById<TextView>(Resource.Id.actionText);
        }

    }
}