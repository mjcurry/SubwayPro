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
    class RouteSummaryRowAdapter : RecyclerView.Adapter
    {
        public List<KeyValuePair<int, String>> Instructions { get; set; }
        //If the adaper will take clicks
        //public event EventHandler<int> ItemClick;

        //Constructs adapter with blank instructions
        public RouteSummaryRowAdapter()
        {
            Instructions = new List<KeyValuePair<int, string>>();
        }
        
        //Constructs adapter by parsing an itinerary into a set of instructions
        public RouteSummaryRowAdapter(Itinerary itinerary)
        {
            Instructions = new List<KeyValuePair<int, string>>();
            Leg[] legs = itinerary.Legs;
            //Iterates over each leg, producing an instruction for each one
            foreach (Leg leg in legs)
            {
                //Sets a default iconId for a (!) icon
                int iconId = Tools.TransitModeToIconId(leg.Mode);
                //Builds a text instruction based on the mode, destination station name, and duration in minutes
                StringBuilder instructionBuilder = new StringBuilder();
                instructionBuilder.Append(leg.Mode);
                instructionBuilder.Append(" to ");
                instructionBuilder.Append(leg.To.Name);
                instructionBuilder.Append(" (");
                instructionBuilder.Append(Math.Round((double)leg.Duration / 60, 1));
                instructionBuilder.Append(" minutes)");
                Instructions.Add(new KeyValuePair<int, string>(iconId, instructionBuilder.ToString()));
            }
        }

        //Allows construction with a List of raw instructions
        public RouteSummaryRowAdapter(List<KeyValuePair<int, String>> instructions)
        {
            this.Instructions = instructions;
        }

        public override int ItemCount => Instructions.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            //Sets the image to the one with iconId, sets text to the instruction at the given position
            NavigationInstructionViewHolder vh = holder as NavigationInstructionViewHolder;
            vh.Icon.SetImageResource(Instructions[position].Key);
            vh.Instructions.Text = Instructions[position].Value;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            //Generates a new view using the NavigationRowLayout
            View view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.NavigationRowLayout, parent, false);
            //Makes a new custom viewholder for the new view
            NavigationInstructionViewHolder vh = new NavigationInstructionViewHolder(view);
            return vh;
        }
    }
}