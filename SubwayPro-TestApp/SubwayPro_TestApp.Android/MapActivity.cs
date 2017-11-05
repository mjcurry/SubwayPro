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
using Mapbox.Camera;
using Mapbox.Geometry;
using Mapbox.Utils;
using Square.OkHttp3;
using Android.Util;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Android.Graphics.Drawables;
using Android.Views.InputMethods;
using Newtonsoft.Json.Linq;
using Java.Util;
using Mapbox.Annotations;
using Plugin.DeviceInfo;
using Com.Sothree.Slidinguppanel;
using Android.Support.V7.Widget;
using Android.Views.Animations;
using Java.Text;
using Android.Graphics;
using Clans.Fab;

namespace SubwayPro_TestApp.Droid
{
    [Activity(Label = "MapActivity")]
    public class MapActivity : Activity
    {
        /** Field declarations **/
        //Tag for logging purposes
        static readonly string TAG = "X:" + typeof(MapActivity).Name;
        
        //MapView and Map; must be initialized before use
        private MapView mapView;
        private MapboxMap map;
        
        //Instance of a MapBoxInterface
        private MapBoxInterface mbi = new MapBoxInterface();
        
        //Allows the response from otp to be cached after being fetched
        private JObject otpResponse = null;
        
        //Stores the current step along the route that the user is on
        private int currentStep = 0; 
        private readonly int[] colors = new int[] {Android.Graphics.Color.DarkBlue, Android.Graphics.Color.IndianRed, Android.Graphics.Color.Olive};
        
        /** Method Declarations **/
        /** Route Selection Screen **/
        protected override async void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            RequestWindowFeature(WindowFeatures.NoTitle);
            SetContentView(Resource.Layout.MapLayout);
            mapView = FindViewById<MapView>(Resource.Id.mapview);

            //Registers the device if it's not already in the database, puts it on another thread
            registerDeviceAsync();

            //Loads the map background
            await loadMap(bundle);

            //Binds layout elements to a set of variables            
            AutoCompleteTextView destinationBox = FindViewById<AutoCompleteTextView>(Resource.Id.destinationSearchView);
            AutoCompleteTextView originBox = FindViewById<AutoCompleteTextView>(Resource.Id.originSearchView);
            FrameLayout originFrame = FindViewById<FrameLayout>(Resource.Id.originFrame);
            FrameLayout destinationFrame = FindViewById<FrameLayout>(Resource.Id.destinationFrame);
            Button routeButton = FindViewById<Button>(Resource.Id.routeButton);
            Button originClear = FindViewById<Button>(Resource.Id.originClearButton);
            Button destinationClear = FindViewById<Button>(Resource.Id.destinationClearButton);
            SlidingUpPanelLayout view = FindViewById<SlidingUpPanelLayout>(Resource.Id.sliding_layout);
            Button startButton = FindViewById<Button>(Resource.Id.startButton);
            FloatingActionMenu fam = FindViewById<FloatingActionMenu>(Resource.Id.fabMenu);
            FloatingActionButton delayFab = FindViewById<FloatingActionButton>(Resource.Id.delayFab);
            FloatingActionButton closureFab = FindViewById<FloatingActionButton>(Resource.Id.closureFab);

            //Configures the autocomplete text views
            configureActv(destinationBox, originBox, destinationFrame, destinationClear, routeButton);
            configureActv(originBox, destinationBox, originFrame, originClear, routeButton);

            //Turns the originBox green if it already contains a valid value
            makeActvTurnGreenWhenAddressesAreValid(originBox, originFrame); 

            //Sets up the sliding panel on the bottom
            var manager = initializeSlidingPanel();
            initializeSlidingPanelDelegates(view, manager);
            
            //Sets up delegates for the stawrt and select buttons
            bindStartSelectButtonDelegates(startButton, routeButton, originBox, destinationBox);

            //Sets focus on the destination box so the user can immediately begin typing there
            destinationBox.RequestFocus();

            //Binds the delegates to each floating action menu option
            bindFloatingActionMenuOptionDelegates(fam, delayFab, closureFab);
            
        }

        private async Task registerDeviceAsync()
        {
            // register the device with the backend
            string device_id = "a-" + CrossDeviceInfo.Current.Id;
            DatabaseAPI dbase = new DatabaseAPI();
            var response = await dbase.GetRegistrationInfo(device_id);
            if (response != null && (string)response["status"] == "error")
            {
                // device was not found in DB, so register as a new user
                var reg_response = await dbase.RegisterNewDevice(device_id, "Android", Android.OS.Build.VERSION.Sdk);
                if (reg_response != null && (string)reg_response["status"] != "ok")
                {
                    Console.WriteLine("registerNewDevice Error: " + (string)reg_response["message"]);
                }
            }

        }

        protected async Task<bool> loadMap(Bundle bundle)
        {
            //Attempts to create the mapview with the specified style using our Mapbox credentials, logs the exception if it fails
            try
            {
                Mapbox.MapboxAccountManager.Start(this, GetString(Resource.String.mapboxAccessToken));
                mapView.StyleUrl = "mapbox://styles/andrewharris/cj4x3b6hd2i412sqnrg5sipbt";
                mapView.OnCreate(bundle);
            }
            catch (Exception e)
            {
                Log.Debug("MapActivity.loadMap", e.StackTrace);
            }

            //Makes a default camera while waiting for the user's location to be determined
            var cameraPosition = new CameraPosition.Builder()
                    .Target(new LatLng(40.7128, -74.0059)) // Sets the new camera position
                    .Zoom(9) // Sets the zoom
                    .Build(); // Creates a CameraPosition from the builder

            //Attempts to initialize the map field, returns false (failure) if unable to
            try
            {
                map = await mapView.GetMapAsync();
            }
            catch(Exception e)
            {
                return false;
            }
            //Sets up the map UI elements, disables tilt gestures, the Mapbox logo, and the compass
            map.UiSettings.TiltGesturesEnabled = false;
            map.UiSettings.LogoEnabled = false;
            map.UiSettings.CompassEnabled = false;

            //Tries to mark the user's location on the map and setup a user-centered camera, uses the default camera position otherwise
            try
            {
                Tools.markUserLocation(map);
            }
            catch (Exception e)
            {
                map.AnimateCamera(CameraUpdateFactory.NewCameraPosition(cameraPosition), 3000);
                Log.Error(TAG, e.StackTrace);
                return false;
            }

            //Binds delegate to handle the recentering floating action button
            FindViewById<FloatingActionButton>(Resource.Id.recenterOnUserActionButton).Click += delegate
            {
                cameraPosition = new CameraPosition.Builder()
                                .Target(new LatLng(map.MyLocation.Latitude, map.MyLocation.Longitude))
                                .Zoom(13)
                                .Tilt(0)
                                .Build();
                map.AnimateCamera(CameraUpdateFactory.NewCameraPosition(cameraPosition), 3000);
            };
            return true; //If it was successfully able to set up the user-centered map, return a success flag
        }

        protected void configureActv(AutoCompleteTextView actv, AutoCompleteTextView actvOther, FrameLayout frame, Button clear, Button navButton)
        {
            //Initializes the actv suggestions adapter
            ArrayAdapterNoFilter adapter = new ArrayAdapterNoFilter(this, Android.Resource.Layout.SimpleDropDownItem1Line);
            //Initializes the suggestions updater for the adapter
            AutocompleteSuggestionUpdater suggestor = new AutocompleteSuggestionUpdater(actv, adapter, map, mbi);
            //Sets the adapter of the actv
            actv.Adapter = adapter;
            //Sets the updater to update the suggestions when the text changes
            actv.AddTextChangedListener(suggestor);
            InputMethodManager imm = (InputMethodManager)GetSystemService(Activity.InputMethodService);
            //Sets delegates for handling the opacity of the navigation button
            actv.AfterTextChanged += delegate
            {
                //Updates the color of the actv based on whether the address is valid
                makeActvTurnGreenWhenAddressesAreValid(actv, frame);
            };
            //Sets delegate for when the enter button is pushed on the keyboard
            actv.EditorAction += delegate
            {
                //Unfocuses from the actv, closes the dropdown, and hides the keyboard.
                actv.ClearFocus();
                actv.DismissDropDown();
                imm.HideSoftInputFromWindow(actv.WindowToken, 0);
            };
            //Sets delegate for if the actv is clicked
            actv.ItemClick += delegate
            {
                //Closes the dropdown menu, turns off the suggestor, updates the route button's opacity, and hides the keyboard
                actv.DismissDropDown();
                suggestor.showDialog = false;
                Tools.makeRouteButtonOpaqueIfBothAddressesAreValid(actv.Text, actvOther.Text, navButton, mbi);
                imm.HideSoftInputFromWindow(actv.WindowToken, 0);
            };
            //Sets delegate for if the actv changes color
            frame.LayoutChange += delegate
            {
                //Updates the route button's opacity
                Tools.makeRouteButtonOpaqueIfBothAddressesAreValid(actv.Text, actvOther.Text, navButton, mbi);
            };
            //Sets delegate for if the clear button has been pressed
            clear.Click += delegate
            {
                //Closes the dropdown menu, updates the route button's opacity, 
                //gets the suggestor ready to start making suggestions when the user types again, and clears the text in the box 
                actv.DismissDropDown();
                Tools.makeRouteButtonOpaqueIfBothAddressesAreValid(actv.Text, actvOther.Text, navButton, mbi);
                suggestor.showDialog = true;
                actv.Text = "";
            };
        }

        private async void makeActvTurnGreenWhenAddressesAreValid(AutoCompleteTextView actv, FrameLayout frame)
        {
            //Prevents the function from running if the map is not initialized
            if (map == null)
            {
                return;
            }
            //Checks if the address in the actv is valid, changes the background color if so, sets it to gray if not
            if (await mbi.AddressIsValid(actv.Text))
            {
                actv.Alpha = .75F;
                frame.SetBackgroundColor(Android.Graphics.Color.DarkGreen);
            }
            else
            {
                actv.Alpha = .75F;
                frame.SetBackgroundColor(Android.Graphics.Color.Gray);
            }
        }

        private NavigationLayoutManager initializeSlidingPanel()
        {
            //Binds a variable for the recycler view, layout manager, and slideUp panel
            var recyclerView = FindViewById<RecyclerView>(Resource.Id.slidingRecycler);
            var layoutManager = new NavigationLayoutManager(this);
            var slideUpLayout = FindViewById<SlidingUpPanelLayout>(Resource.Id.sliding_layout);
            
            //Sets the layout manager to not allow scrolling so it doesn't interfere with pulling up the sliding layout
            layoutManager.scrollable = false;

            //Creates a blank new adapter
            var adapter = new RouteSummaryRowAdapter();
            
            //Sets the layout manager and adapter for the recyclerView
            recyclerView.SetLayoutManager(layoutManager);
            recyclerView.SetAdapter(adapter);

            //Returns the layoutManager produced (not really used)
            return layoutManager;
        }

        public void initializeSlidingPanelDelegates(SlidingUpPanelLayout view, NavigationLayoutManager manager)
        {
            //Binds a delegate that enables recyclerView scrolling iff the panel has been expanded, otherwise it disables it; it runs every time the panel state changes.
            //Also enables map scrolling iff the panel has been collapsed, otherwise disables it.
            view.PanelStateChanged += delegate
            {
                manager.scrollable = (view.GetPanelState() == SlidingUpPanelLayout.PanelState.Expanded);
                map.UiSettings.ScrollGesturesEnabled = (view.GetPanelState() == SlidingUpPanelLayout.PanelState.Collapsed);
            };
        }

        private void bindStartSelectButtonDelegates(Button start, Button select, 
            AutoCompleteTextView originBox, AutoCompleteTextView destinationBox)
        {
            select.Click += async delegate
            {
                //Gets the coordinates for the origin and destination points
                double[] originCoords = mbi.ConvertAddressToGpsCoordinates(originBox.Text, map.MyLocation.Latitude, map.MyLocation.Longitude);
                double[] destinationCoords = mbi.ConvertAddressToGpsCoordinates(destinationBox.Text, map.MyLocation.Latitude, map.MyLocation.Longitude)
                ;
                //Clears old routes
                Tools.clearRoutes(map);

                //Asks Otp for response based on the origin and destination
                otpResponse = await OtpAPI.Query(originCoords, destinationCoords);

                //Plots the first two routes in the response
                Tools.plotRoute(otpResponse, map, 2, colors);

                //Sets the camera to show the overall route
                Tools.setRoutingCamera(originCoords, destinationCoords, map);

                //Parses the itinerary, uses it to create an adapter for a recycler view
                Itinerary itin = OtpAPI.ParseItinerary(otpResponse["plan"]["itineraries"][0]);
                var adapter = new RouteSummaryRowAdapter(itin);

                //Binds the adapter to the recyclerView
                var rv = FindViewById<RecyclerView>(Resource.Id.slidingRecycler);
                rv.SetAdapter(adapter);

                //Sets the start button to be fully opaque and clickable
                start.Alpha = 1;
                start.Clickable = true;

                //Slides up the bottom panel to show the route summary
                FindViewById<SlidingUpPanelLayout>(Resource.Id.sliding_layout).SetPanelState(SlidingUpPanelLayout.PanelState.Expanded);
            };
            start.Click += delegate {
                //Reconfigures the ui for the navigation mode, now that the user wants to start navigation
                reconfigureUiForNavigation(0);
            };

        }

        private void bindFloatingActionMenuOptionDelegates(FloatingActionMenu fam, FloatingActionButton delayFab, FloatingActionButton closureFab)
        {
            //When clicked, the buttons print to the console and display a toast on the screen.
            //Should eventually be replaced with better dialog for user and http put/post request to backend
            delayFab.Click += delegate
            {
                Console.WriteLine("Delay reported");
                Toast.MakeText(this, "Delay reported", ToastLength.Long).Show();
                fam.Close(true);
            };

            closureFab.Click += delegate
            {
                Console.WriteLine("Closure reported");
                Toast.MakeText(this, "Closure reported", ToastLength.Long).Show();
                fam.Close(true);
            };

        }
        
        /**Turn By Turn Navigation Ui**/

        private void reconfigureUiForNavigation(int itineraryIndex)
        {
            //Disable the actv layout, and route/start buttons
            FindViewById<LinearLayout>(Resource.Id.actvLayout).Visibility = ViewStates.Gone;
            FindViewById<Button>(Resource.Id.startButton).Visibility = ViewStates.Gone;
            FindViewById<Button>(Resource.Id.routeButton).Visibility = ViewStates.Gone;

            //Binds a variable to the recyclerView
            RecyclerView recyclerView = FindViewById<RecyclerView>(Resource.Id.slidingRecycler);

            //Parses the itinerary selected
            Itinerary itin = OtpAPI.ParseItinerary(otpResponse["plan"]["itineraries"][itineraryIndex]);

            //Binds a variable to a configured recycler view
            recyclerView = Tools.configureRecyclerViewForNavigation(itin, recyclerView);

            //Sets up the turn by turn ui (the upper section)
            initializeUpperNavigationUi(itin, recyclerView);

            //Sets the first row in the recyclerView to be highlighted
            ((StepRowAdapter)recyclerView.GetAdapter()).HighlightedRow = 0;

            //Hides the recentering button when navigating
            Tools.toggleVisibility(FindViewById<FloatingActionButton>(Resource.Id.recenterOnUserActionButton));
            
            //Binds the backToRoutingButton to go back to the routing screen
            Button backToRoutingButton = FindViewById<Button>(Resource.Id.backToRoutingButton);
            backToRoutingButton.Click += delegate
            {
                //TODO: implement reverse screen reconfiguration
                reconfigureUiForRouting();
            };
            
            //Configures the station image thumbnail with the sample image
            configureStationImage();
            
            //Starts turn by turn tracking for stepping through the steps
            initializeTurnByTurnTracking(itin, recyclerView);
        }
        

        private void initializeUpperNavigationUi(Itinerary itin, RecyclerView recyclerView)
        {
            LinearLayout navigationUiView = FindViewById<LinearLayout>(Resource.Id.navigationUiView);
            navigationUiView.Visibility = ViewStates.Visible;

            //Calculates trip duration, distance, and ETA, for the trip information window
            String duration = Math.Round(itin.Duration / 60.0).ToString() + " minutes";
            String distance = Math.Round(itin.WalkDistance * 3.28084).ToString() + " ft";//TODO: make this the total distance, not just the distance walked
            String arrivalTime = "4:20 pm"; //TODO: convert arrival time from epoch (seconds) to AM/PM time 
            
            //Initializes the trip information window with initial values
            initializeNavigationUiTripInformationWindow(duration, distance, arrivalTime, "", 0);

            //Updates the window with data from the first step (i = 0) if there is at least one leg
            if (itin.Legs.Length > 0)
            {
                updateNavigationUiTripInformationWindows(0, (StepRowAdapter)recyclerView.GetAdapter());
            }

            //Binds delegates for the next and back buttons to step the route forward and backwards (used for debugging, not going to be in release)
            Button nextButton = FindViewById<Button>(Resource.Id.nextNavigationStep);
            Button backButton = FindViewById<Button>(Resource.Id.previousNavigationStep);
            nextButton.Click += delegate
            {
                stepRouteForward(recyclerView);
            };
            backButton.Click += delegate
            {
                stepRouteBackward(recyclerView);
            };
        }

        private void configureStationImage()
        {

            //Binds variable for the layers and the images in each layer
            LinearLayout thumbLayer = FindViewById<LinearLayout>(Resource.Id.subwayThumbLayer);
            ImageView thumbImage = FindViewById<ImageView>(Resource.Id.subwayThumbImage);
            FrameLayout fullLayer = FindViewById<FrameLayout>(Resource.Id.subwayFullLayer);
            ImageView fullImage = FindViewById<ImageView>(Resource.Id.subwayFullImage);

            //Binds variable for sliding panel
            SlidingUpPanelLayout slider = FindViewById<SlidingUpPanelLayout>(Resource.Id.sliding_layout);

            //Sets the image resources of each imageView, they're invisible so they don't show up yet
            thumbImage.SetImageResource(Resource.Drawable.entrance_example);
            fullImage.SetImageResource(Resource.Drawable.entrance_example);

            //Sets the thumbnail layer to be visible and take click events
            thumbLayer.Visibility = ViewStates.Visible;
            thumbImage.Clickable = true;
            
            //Hides the thumbnail layer when the sliding panel is extended
            slider.PanelStateChanged += delegate
            {
                if (slider.GetPanelState() == SlidingUpPanelLayout.PanelState.Expanded || slider.GetPanelState() == SlidingUpPanelLayout.PanelState.Dragging)
                {
                    Tools.toggleVisibility(thumbLayer);
                }
                else
                {
                    Tools.toggleVisibility(thumbLayer);
                }
            };

            //Switches view from the thumbnail view with map to a fullscreen image
            thumbImage.Click += delegate
            {
                Tools.toggleVisibility(thumbLayer);
                Tools.toggleVisibility(fullLayer);
            };

            //Switches view from the full layer to the thumbnail view
            fullLayer.Click += delegate
            {
                Tools.toggleVisibility(thumbLayer);
                Tools.toggleVisibility(fullLayer);
            };
        }

        private void initializeTurnByTurnTracking(Itinerary itinerary, RecyclerView recyclerView)
        {
            //Creates a list to store a set of waypoints
            List<LatLng> waypoints = new List<LatLng>();

            //Generates the waypoints from the GPS coordinates of each step and the "to" coords of each leg
            foreach (Leg leg in itinerary.Legs)
            {
                //Add the "to" coords of the leg
                waypoints.Add(new LatLng(leg.To.LatLong[0], leg.To.LatLong[1]));
                //Adds the coords of each step
                foreach(Step step in leg.Steps)
                {
                    waypoints.Add(new LatLng(step.LatLong[0], step.LatLong[1]));
                }
            }
            //Binds a delegate that progresses through waypoints whenever the user crosses their respective geofences
            map.MyLocationChange += delegate
            {
                progressThroughHitWaypoints(waypoints, recyclerView);
            };
        }

        private void initializeNavigationUiTripInformationWindow(String duration, String distance, String arrivalTime, String turnInstruction, double stepDistance)
        {
            //Sets up the top bar with the specified duration, distance, eta, turn instruction, and distance (in meters)
            FindViewById<TextView>(Resource.Id.totalTripDurationText).Text = duration; //doesn't change
            FindViewById<TextView>(Resource.Id.totalTravelDistanceText).Text = distance; //doesn't change
            FindViewById<TextView>(Resource.Id.estimatedArrivalTimeText).Text = arrivalTime; //doesn't change
            FindViewById<TextView>(Resource.Id.turnInstruction).Text = turnInstruction; //gets updated
            FindViewById<TextView>(Resource.Id.stepDistanceText).Text = Math.Round(stepDistance * 3.28084, 0).ToString() + " ft"; //gets updated
        }

        private void progressThroughHitWaypoints(List<LatLng> waypoints, RecyclerView recyclerView)
        {
            //Gets the current position
            LatLng currentPos = new LatLng(map.MyLocation.Latitude, map.MyLocation.Longitude);
            //Steps forward through all waypoints that are within 50 meters of the current location
            while (currentPos.DistanceTo(waypoints[currentStep]) < 50 && currentStep < recyclerView.GetAdapter().ItemCount)
            {
                stepRouteForward(recyclerView);
            }
        }

        private void stepRouteForward(RecyclerView recyclerView)
        {
            //Checks that the current step won't go out of bounds when incremented, does nothing if it would
            if (currentStep < recyclerView.GetAdapter().ItemCount - 1)
            {
                //Gets the adapter being used
                var adapter = (StepRowAdapter)recyclerView.GetAdapter();
                //Increments the row that's highlighted
                adapter.HighlightedRow = currentStep + 1;

                //Tells the adapter that the highlighted row has changed
                adapter.NotifyDataSetChanged();

                //Refreshes the child view that should no longer be highlighted
                if (recyclerView.GetChildAt(currentStep) != null)
                {
                    recyclerView.GetChildAt(currentStep).RefreshDrawableState();
                }

                //Increments the current step
                currentStep += 1;

                //Refreshes the child view that should now be highlighted
                if (recyclerView.GetChildAt(currentStep) != null)
                {
                    recyclerView.GetChildAt(currentStep).RefreshDrawableState();
                }

                //Updates the top bar to include the current step instructions, icon, and distance.
                updateNavigationUiTripInformationWindows(currentStep, adapter);
            }
        }

        private void stepRouteBackward(RecyclerView recyclerView)
        {
            //Checks that the current step is not the first one, does nothing if it is
            if (currentStep > 0)
            {
                //Gets the adapter for the recyclerView
                var adapter = (StepRowAdapter)recyclerView.GetAdapter();
                //Decrements the highlighted row in the adapter, notifies the adapter of the change
                adapter.HighlightedRow = currentStep - 1;
                adapter.NotifyDataSetChanged();
                //Refreshes the child view that should no longer be highlighted
                if (recyclerView.GetChildAt(currentStep) != null)
                {
                    recyclerView.GetChildAt(currentStep).RefreshDrawableState();
                }
                //Decrements the current step
                currentStep -= 1;
                //Refreshes the child view that should now be highlighted
                if (recyclerView.GetChildAt(currentStep) != null)
                {
                    recyclerView.GetChildAt(currentStep).RefreshDrawableState();
                }
                //Updates the top bar to include the current step instructions, icon, and distance
                updateNavigationUiTripInformationWindows(currentStep, adapter);
            }
        }

        private void updateNavigationUiTripInformationWindows(int currentStep, StepRowAdapter adapter)
        {
            //Gets the iconId, instruction, and distance from the adapter
            int iconId = adapter.IconId(currentStep);
            String instruction = adapter.InstructionText(currentStep);
            String distance = adapter.DistanceText(currentStep);
            //Sets the top bar instruction, distance, and icon to the same values
            FindViewById<TextView>(Resource.Id.turnInstruction).Text = instruction;
            FindViewById<TextView>(Resource.Id.stepDistanceText).Text = distance;
            FindViewById<ImageView>(Resource.Id.navigationUiImage).SetImageResource(iconId);
        }

        private void reconfigureUiForRouting()
        {
            //TODO: Implement function
            Console.WriteLine("Reconfigure for routing");
        }
        
    }
}