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
using Mapbox.Maps;
using Newtonsoft.Json.Linq;
using Mapbox.Annotations;
using Mapbox.Geometry;
using Mapbox.Camera;
using System.Threading.Tasks;
using Android.Support.V7.Widget;

namespace SubwayPro_TestApp.Droid
{
    class Tools
    {
        private static Dictionary<String, int> TransitModeToIconIdDictionary = new Dictionary<string, int>();

        static Tools()
        {
            TransitModeToIconIdDictionary.Add("RAIL", Resource.Drawable.ic_directions_car_black_24dp);
            TransitModeToIconIdDictionary.Add("SUBWAY", Resource.Drawable.ic_directions_car_black_24dp);
            TransitModeToIconIdDictionary.Add("WALK", Resource.Drawable.ic_directions_walk_black_24dp);
            TransitModeToIconIdDictionary.Add("PATH", Resource.Drawable.ic_directions_car_black_24dp);
        }

        public static int TransitModeToIconId(String transitMode)
        {
            //Provides the iconId associated with a transit mode, or (!) if none are available
            int iconId;
            if (TransitModeToIconIdDictionary.TryGetValue(transitMode, out iconId))
            {
                return iconId;
            }
            return Resource.Drawable.ic_info_outline_24dp;
        }

        //Clears all routes on the map
        public static void clearRoutes(MapboxMap map)
        {
            var polylines = map.Polylines;
            foreach (var polyline in polylines)
            {
                map.RemovePolyline(polyline);
            }
        }

        //Plots routes on the map
        public static void plotRoute(JObject response, MapboxMap map, int count, int[] colors)
        {
            //Parses the routes from the response into a list of points, stores all routes in a list
            List<List<double[]>> routes = OtpAPI.GetRoutePoints(response);

            //Handles case in which there are no routes to the destination
            if (routes.Count() == 0)
            {
                //TODO: Show message to user that no routes are possible
            }

            //Iterates over the routes in the response, plotting each one in a different color. 
            //The first route is plotted with full opacity, while the rest are 50% transparent.
            for (int i = 0; i < Math.Min(routes.Count(), count); i++)
            {
                //Sets up a new polyline options object to be plotted on the map
                var polylineOpts = new PolylineOptions();
                //Adds each point in the route to the polyline
                foreach (double[] point in routes[i])
                {
                    polylineOpts.Add(new LatLng(point[0], point[1]));
                }
                //Set a dark thick line if it's the primary route
                if (i == 0)
                {
                    polylineOpts.SetAlpha(1F);
                    polylineOpts.SetWidth(5F);
                }
                //Set a lighter, thinner, line if it's a secondary one
                else
                {
                    polylineOpts.SetAlpha(.5F);
                    polylineOpts.SetWidth(3F);
                }
                //Sets the color according to the set in the colors field
                polylineOpts.SetColor(colors[i % 3]);
                //Plots the polyline on the map
                map.AddPolyline(polylineOpts);
            }
        }

        //Marks the user's location on the map
        public static void markUserLocation(MapboxMap map)
        {
            //Enables location tracking, sets the desired accuracy to 10m, sets the accuracy ring to have 75% opacity
            map.MyLocationEnabled = true;
            map.MyLocation.Accuracy = 10;
            map.MyLocationViewSettings.AccuracyAlpha = 75;

            //Sets up a camera centered on the user
            var cameraPosition = new CameraPosition.Builder()
                                .Target(new LatLng(map.MyLocation.Latitude, map.MyLocation.Longitude))
                                .Zoom(13)
                                .Tilt(0)
                                .Build();

            //Sets that camera for viewing the map
            map.AnimateCamera(CameraUpdateFactory.NewCameraPosition(cameraPosition), 1000);
        }

        //Sets the camera so it can view the overall route
        public static void setRoutingCamera(double[] origin, double[] destination, MapboxMap map)
        {
            //Finds the center point between the origin and destination
            double[] center = new double[] { (origin[0] + destination[0]) / 2, (origin[1] + destination[1]) / 2 };
            //Sets a camera position at that location with an assumed zoom of 12 (usually okay, should be adjusted based on distance between locations eventually)
            var cameraPosition = new CameraPosition.Builder()
                                .Target(new LatLng(center[0], center[1])) // Sets the new camera position
                                .Zoom(12) // Sets the zoom
                                .Build(); // Creates a CameraPosition from the builder
            //Sets that camera for viewing the map
            map.AnimateCamera(CameraUpdateFactory.NewCameraPosition(cameraPosition), 1000);
        }
        
        //Makes the route button opaque if both addresses are valid
        public static async Task makeRouteButtonOpaqueIfBothAddressesAreValid(String originAddress, String destinationAddress, Button routeButton, MapBoxInterface mbi)
        {
            if (await mbi.AddressIsValid(originAddress) && await mbi.AddressIsValid(destinationAddress))
            {
                routeButton.Alpha = 1;
                routeButton.Clickable = true;
            }
            else
            {
                routeButton.Alpha = .5F;
                routeButton.Clickable = false;
            }
        }

        //Makes a visible view gone and vice versa
        public static void toggleVisibility(View view)
        {
            //Switches the visibility of the view, if it was visible it's gone and vice versa
            if (view.Visibility.Equals(ViewStates.Visible))
            {
                view.Visibility = ViewStates.Gone;
            }
            else
            {
                view.Visibility = ViewStates.Visible;
            }
        }

        //Rebinds the adapter of the recyler view to one suitable for navigation
        public static RecyclerView configureRecyclerViewForNavigation(Itinerary itinerary, RecyclerView recyclerView)
        {
            //Creates a new adapter with for the steps, allows the layout in the recycler view to be changed from the one used in route selection
            StepRowAdapter adapter = new StepRowAdapter(itinerary.Legs);
            recyclerView.SetAdapter(adapter);
            return recyclerView;
        }
    }
}