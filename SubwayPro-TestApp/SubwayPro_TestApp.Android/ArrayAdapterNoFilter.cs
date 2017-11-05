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
using Java.Lang;

namespace SubwayPro_TestApp.Droid
{
    class ArrayAdapterNoFilter : ArrayAdapter
    {
        //Implements an array adapter with no filtering (just displays an array into a layout)

        Activity activity;
        public IList<string> values = new List<string>();
        Filter filter;

        public ArrayAdapterNoFilter(Activity activity, int resource) : base(activity, resource)
        {
            this.activity = activity;
            filter = new PassthroughFilter(this);

        }

        public void Add(string value)
        {
            values.Add(value);
        }

        public void AddAll(string[] values)
        {
            for(int i = 0; i < values.Length; i++)
            {
                this.values.Add(values[i]);
            }
        }

        public override int Count => values.Count;

        public override Filter Filter => filter;

        public override Java.Lang.Object GetItem(int position)
        {
            return values[position];
        }

        public override long GetItemId(int position)
        {
            return values[position].GetHashCode();
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = convertView;
            if (view == null)
            {
                view = activity.LayoutInflater.Inflate(Android.Resource.Layout.SimpleListItem1, null);
            }
            view.FindViewById<TextView>(Android.Resource.Id.Text1).Text = values[position].ToString();
            return view;
        }

        public override void Clear()
        {
            values.Clear();
        }
    }

    class PassthroughFilter : Filter
    {
        ArrayAdapterNoFilter adapter;        
        public PassthroughFilter(ArrayAdapterNoFilter adapter)
        {
            this.adapter = adapter;
        }

        protected override FilterResults PerformFiltering(ICharSequence constraint)
        {
            FilterResults results = new FilterResults();
            results.Values = adapter.values.ToArray();
            results.Count = adapter.values.Count;
            return results;
        }

        protected override void PublishResults(ICharSequence constraint, FilterResults results) {
            adapter.NotifyDataSetChanged();
        }
    }





}