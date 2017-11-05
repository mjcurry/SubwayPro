using System;

using Android.Views;
using Android.Widget;
using Android.Support.V7.Widget;
using System.Collections.Generic;
using System.Linq;
using Mapbox.Geometry;

namespace SubwayPro_TestApp.Droid
{
    class StepRowAdapter : RecyclerView.Adapter
    {
        private Object[] displayObjects = new Object[0];
        private bool[] isLeg = new bool[0];
        public int HighlightedRow { get; set; }

        public StepRowAdapter(Leg[] legs)
        {
            //Initializes the highlighted row to be -1 so none of the rows are highlighted by default
            HighlightedRow = -1;
            //Iterates through the legs, adding each leg and each of its steps to the displayObjects array
            //Stores an index-synchronized boolean array indicating whether each object is a leg (true) or step (false)
            foreach(Leg leg in legs)
            {
                displayObjects = displayObjects.Append(leg).ToArray();
                if (leg.Steps != null)
                {
                    displayObjects = displayObjects.Concat(leg.Steps).ToArray();
                    isLeg = isLeg.Append(true).Concat(new bool[leg.Steps.Length]).ToArray();
                }
                else
                {
                    isLeg.Append(true);
                }
            }
        }

        // Create new views (invoked by the layout manager)
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {

            //Setup your layout here
            View itemView = null;
            var id = Resource.Layout.StepRowLayout;
            itemView = LayoutInflater.From(parent.Context).
                   Inflate(id, parent, false);

            var vh = new StepRowHolder(itemView);
            return vh;
        }

        // Replace the contents of a view (invoked by the layout manager)
        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            //Binds variables to the display object (step/leg) and the 
            var holder = viewHolder as StepRowHolder;

            //Uses the helper methods to get the instruction, icon, and distance, then sets each field of the viewHolder to each one
            holder.StepText.Text = InstructionText(position);
            holder.StepIcon.SetImageResource(IconId(position));
            holder.StepDistanceText.Text = DistanceText(position);

            //Highlights the view if it is in the position indicated by HighlightedRow, otherwise sets a white background
            if (position == HighlightedRow)
            {
                holder.ItemView.SetBackgroundResource(Android.Resource.Color.HoloBlueLight);
            }
            else
            {
                holder.ItemView.SetBackgroundResource(Android.Resource.Color.BackgroundLight);
            }
        }

        public int IconId(int position)
        {
            //Gets the icon id based on the transit type if it's a leg
            if (isLeg[position])
            {
                Leg leg = (Leg)displayObjects[position];
                Tools.TransitModeToIconId(leg.Mode);
            }
            //TODO: Get the icon id based on the relative direction if it's a step
            else
            {
                //Returns a placeholder leftArrow icon
                return Resource.Drawable.leftArrow;
            }
            //Should never be reached; returns a default (!) icon
            return Resource.Drawable.ic_info_outline_24dp;
        }

        public String InstructionText(int position)
        {
            //Returns the instruction as produced by the leg/step
            return displayObjects[position].ToString();
        }

        public String DistanceText(int position)
        {
            //Calculates the aerial distance for legs, uses the walking distance for steps
            double distanceFeet = 0;
            if (isLeg[position])
            {
                Leg leg = (Leg)displayObjects[position];
                //Creates LatLng points for the To and From stops on the leg
                LatLng toLatLng = new LatLng(leg.To.LatLong[0], leg.To.LatLong[1]);
                LatLng fromLatLng = new LatLng(leg.From.LatLong[0], leg.From.LatLong[1]);
                //Uses GPS coordinates to calculate distance (Mapbox SDK helps here)
                distanceFeet = Math.Round(fromLatLng.DistanceTo(toLatLng) * 3.28084, 0);
            }
            else
            {
                //Converts the distance returned by steps from meters to feet
                distanceFeet = Math.Round(((Step)displayObjects[position]).Distance * 3.28084, 0);
            }
            //Returns the distance either expressed as feet or miles, if it's over 0.1 mile.
            if (distanceFeet > 528)
            {
                return Math.Round((distanceFeet / 5280.0), 1).ToString() + " mi";
            }
            else
            {
                return distanceFeet.ToString() + " ft";
            } 
        }

        public override int ItemCount => displayObjects.Length;

    }
}