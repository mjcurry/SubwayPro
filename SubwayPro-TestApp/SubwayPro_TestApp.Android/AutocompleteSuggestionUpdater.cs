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
using Android.Text;
using Java.Lang;
using Mapbox.Maps;

namespace SubwayPro_TestApp.Droid
{
    class AutocompleteSuggestionUpdater : Java.Lang.Object, ITextWatcher
    {
        private AutoCompleteTextView view;
        private string old = ""; //Caches the text in the actv, allows comparisons
        private ArrayAdapterNoFilter adapter;
        private MapboxMap map;
        private MapBoxInterface mbi;
        public bool showDialog = false;

        public AutocompleteSuggestionUpdater(AutoCompleteTextView view, ArrayAdapterNoFilter adapter, MapboxMap map, MapBoxInterface mbi)
        {
            this.view = view;
            this.adapter = adapter;
            this.map = map;
            this.mbi = mbi;
            updateAdapter("Central Park");
            adapter.Clear();
        }

        public void AfterTextChanged(IEditable s)
        {
            //The show dialog flag is set to false to prevent the drop box from appearing after the text changes as a result of the user selecting an option
            //If it has not been set to false then it is okay to show the dialog
            if (showDialog)
            {
                //If the length of the text has changed by at least one character
                if (System.Math.Abs(s.Length() - old.Length) > 1)
                {
                    //Stores the current string in the cache
                    old = s.ToString();
                    //Updates the adapter with the current string
                    updateAdapter(s.ToString());
                }
            }
            else if (s.Length() > old.Length)
            {
                showDialog = true; //If the person is typing in a new query, always show dialog
            }
            //Otherwise, it does nothing.
        }

        protected async void updateAdapter(string query)
        {
            //Tries to get suggested results, shows whatever it can get
            try
            {
                //Checks that the query is not blank, fetches suggestions using current location if so
                if (query.Length > 0)
                {
                    //Gets up to 3 suggested results
                    string[] suggestedResults = await mbi.FetchAutocompleteSuggestions(query, 3, map.MyLocation.Latitude, map.MyLocation.Longitude);
                    //Clears the adapter
                    adapter.Clear();
                    //Sets the adapter to contain the suggested results
                    adapter.AddAll(suggestedResults);
                }
                //If the query was blank, clear the adapter
                else
                {
                    adapter.Clear();
                }
                //Makes sure that the current location is always an option
                adapter.Add("Current Location");
                //If the dialog should not be shown, dismiss the dialog
                if (!showDialog)
                {
                    view.DismissDropDown();
                }
                //Notifies the adapter that the data has been changed.
                adapter.NotifyDataSetChanged();
            }
            //Always shows the drop down if the flag is set when this function is called
            finally
            {
                if (showDialog)
                {
                    view.ShowDropDown();
                }
            }
        }

        public void BeforeTextChanged(ICharSequence s, int start, int count, int after)
        {
            //This function intentionally left blank
        }
        

        public void OnTextChanged(ICharSequence s, int start, int before, int count)
        {
            //This function intentionally left blank
        }
    }
}