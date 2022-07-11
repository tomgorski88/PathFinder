using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using Android.Gms.Maps;
using Android;
using Android.Gms.Maps.Model;
using Android.Gms.Location;
using Android.Support.V4.App;
using PathFinder.Helpers;
using System;
using PathFinder.Fragments;
using System.Collections.Generic;
using Android.Content;
using Android.Support.V7.App;
using Google.Places;
using Newtonsoft.Json;

namespace PathFinder
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, IOnMapReadyCallback
    {
        readonly string[] permissionGroup = { Manifest.Permission.AccessFineLocation, Manifest.Permission.AccessCoarseLocation };

        TextView placeTextView;
        Button getDirectionsButton;
        Button startTripButton;
        ImageView centerMarker;
        ImageButton locationButton;
        GoogleMap map;
        FusedLocationProviderClient locationProviderClient;
        Android.Locations.Location myLastLocation;
        MapHelpers mapHelper = new MapHelpers();
        ProgressDialogFragment ProgressDialog;
        RelativeLayout placeLayout;
        LocationRequest mLocationRequest;
        LocationCallbackHelper mLocationCallback = new LocationCallbackHelper();
        ISharedPreferences pref = Application.Context.GetSharedPreferences("markers", FileCreationMode.Private);
        ISharedPreferences pref2 = Application.Context.GetSharedPreferences("currentKey", FileCreationMode.Private);
        ISharedPreferencesEditor editor;
        ISharedPreferencesEditor editor2;
        List<LocationMarker> markers = new List<LocationMarker>();
        Locations locationMarkers = new Locations();

        bool directionDrawn = false;

        private LatLng myposition;
        private LatLng destinationPoint;
        private bool tripStarted;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            RequestPermissions(permissionGroup, 0);

            SupportMapFragment mapFragment = (SupportMapFragment)SupportFragmentManager.FindFragmentById(Resource.Id.map);
            mapFragment.GetMapAsync(this);
            if (!PlacesApi.IsInitialized)
            {
                var key = Resources.GetString(Resource.String.mapkey);
                PlacesApi.Initialize(this, key);
            }

            editor = pref.Edit();

            var markersJson = pref.GetString("markers", string.Empty);
            if (!string.IsNullOrEmpty(markersJson))
            {
                locationMarkers = JsonConvert.DeserializeObject<Locations>(markersJson);                
            } else
            {
                locationMarkers.LocationMarkers = markers;
            }
            
            placeLayout = (RelativeLayout)FindViewById(Resource.Id.placeLayout);
            centerMarker = (ImageView)FindViewById(Resource.Id.centerMarker);
            placeTextView = (TextView)FindViewById(Resource.Id.placeTextView);
            getDirectionsButton = (Button)FindViewById(Resource.Id.getDirectionsButton);
            startTripButton = (Button)FindViewById(Resource.Id.startTripButton);
            locationButton = (ImageButton)FindViewById(Resource.Id.locationButton);
            getDirectionsButton.Click += GetDirectionButton_Click;
            placeLayout.Click += PlaceLayout_Click;
            startTripButton.Click += StartTripButton_Click;
            locationButton.Click += LocationButton_Click;

            CreateLocationRequest();
        }

        private async void LocationButton_Click(object sender, EventArgs e)
        {
            var key = Resources.GetString(Resource.String.mapkey);
            var myPosition = new LatLng(destinationPoint.Latitude, destinationPoint.Longitude);
            var address = await mapHelper.FindCoordinateAddress(myPosition, key);
            var location = new LocationMarker { Address = address, Lng = destinationPoint.Longitude, Ltd = destinationPoint.Latitude};
            mapHelper.AddMarker(location, map);
            locationMarkers.LocationMarkers.Add(location);

            string jsonString = JsonConvert.SerializeObject(locationMarkers);            

            editor.PutString($"markers", jsonString);
            editor.Apply();
        }

        private void StartTripButton_Click(object sender, EventArgs e)
        {
            if (!tripStarted)
            {
                var alert = new Android.Support.V7.App.AlertDialog.Builder(this);
                alert.SetTitle("Start Trip");
                alert.SetMessage("Are you sure?");
                alert.SetPositiveButton("Start", (thisalert, args) =>
                {
                    tripStarted = true;
                    var key = Resources.GetString(Resource.String.mapkey);
                    myposition = new LatLng(50.15, 21.97);
                    mapHelper.UpdateLocationToDestination(myposition, destinationPoint, map, key);
                    startTripButton.Text = "Stop trip";
                });

                alert.SetNegativeButton("Cancel", (thisalert, args) =>
                {
                    alert.Dispose();
                });

                alert.Show();
            } else
            {
                var alert = new Android.Support.V7.App.AlertDialog.Builder(this);
                alert.SetTitle("Stop Trip");
                alert.SetMessage("Are you sure?");
                alert.SetPositiveButton("Stop", (thisalert, args) =>
                {
                    locationProviderClient.RequestLocationUpdates(mLocationRequest, mLocationCallback, null);
                    tripStarted = false;
                    startTripButton.Text = "Start trip";
                    ResetApp();
                });

                alert.SetNegativeButton("Cancel", (thisalert, args) =>
                {
                    alert.Dispose();
                });

                alert.Show();
            }
        }
        void ResetApp()
        {
            directionDrawn = false;
            map.Clear();
            centerMarker.Visibility = Android.Views.ViewStates.Visible;
            getDirectionsButton.Visibility = Android.Views.ViewStates.Visible;
            startTripButton.Visibility = Android.Views.ViewStates.Invisible;
            CreateMarkers();
            DisplayLocation();

        }

        public void CreateLocationRequest()
        {
            mLocationRequest = new LocationRequest();
            mLocationRequest.SetInterval(5000);
            mLocationRequest.SetFastestInterval(5000);
            mLocationRequest.SetPriority(LocationRequest.PriorityHighAccuracy);
            mLocationRequest.SetSmallestDisplacement(1);
            mLocationCallback.OnLocationFound += MLocationCallBack_OnLocationFound;
            if (locationProviderClient == null)
            {
                locationProviderClient = LocationServices.GetFusedLocationProviderClient(this);
            }
        }

        private void MLocationCallBack_OnLocationFound(object sender, LocationCallbackHelper.OnLocationCapturedEventArgs e)
        {
            
            myLastLocation = e.Location;
            if (!directionDrawn)
            {
                if (myLastLocation != null)
                {
                    myposition = new LatLng(50.15, 21.97);
                    map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(myposition, 15));
                }
            }

            if (tripStarted)
            {
                if (myLastLocation != null)
                {
                    var key = Resources.GetString(Resource.String.mapkey);
                    myposition = new LatLng(50.15, 21.97);
                    mapHelper.UpdateLocationToDestination(myposition, destinationPoint, map, key);
                }
            }
        }

        private void StartLocationUpdates()
        {
            locationProviderClient.RequestLocationUpdates(mLocationRequest, mLocationCallback, null);
        }
        private void StopLocationUpdates()
        {
            locationProviderClient.RemoveLocationUpdates(mLocationCallback);
        }

        private void PlaceLayout_Click(object sender, EventArgs e)
        {
            if (tripStarted)
            {
                return;
            }
            List<Place.Field> fields = new List<Place.Field>();
            fields.Add(Place.Field.Address);
            fields.Add(Place.Field.Name);
            fields.Add(Place.Field.LatLng);

            Intent intent = new Autocomplete.IntentBuilder(AutocompleteActivityMode.Overlay, fields).SetCountry("PL").Build(this);

            StartActivityForResult(intent, 0);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Android.App.Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (resultCode == Android.App.Result.Ok)
            {
                var place = Autocomplete.GetPlaceFromIntent(data);
                placeTextView.Text = place.Name;
                destinationPoint = place.LatLng;
                GetDirection();
            }
        }

        private void GetDirectionButton_Click(object sender, System.EventArgs e)
        {
            GetDirection();
        }

        public async void GetDirection()
        {
            getDirectionsButton.Visibility = Android.Views.ViewStates.Invisible;
            startTripButton.Visibility = Android.Views.ViewStates.Visible;
            directionDrawn = true;
            centerMarker.Visibility = Android.Views.ViewStates.Invisible;
            var key = Resources.GetString(Resource.String.mapkey);

            ShowProgressDialog("Getting Directions", false);
            var directionJson = await mapHelper.GetDirectionJsonAsync(myposition, destinationPoint, key);
            map.Clear();
            mapHelper.DrawPolylineOnMap(directionJson, map);
            CloseProgressDialog();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (grantResults.Length < 1)
            {
                return;
            }
            if (grantResults[0] == (int)Android.Content.PM.Permission.Granted)
            {
                DisplayLocation();
            }
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            map = googleMap;
            map.UiSettings.ZoomControlsEnabled = true;
            map.CameraMoveStarted += Map_CameraMoveStarted;
            map.CameraIdle += Map_CameraIdle;
            CreateMarkers();

            if (CheckPermission())
            {
                StartLocationUpdates();
            }
        }

        private void CreateMarkers()
        {
            foreach (var marker in locationMarkers.LocationMarkers)
            {
                mapHelper.AddMarker(marker, map);
            }
        }

        public override void OnBackPressed()
        {
            if (directionDrawn)
            {
                ResetApp();
            }
            else
            {
                base.OnBackPressed();
            }
        }

        private async void Map_CameraIdle(object sender, System.EventArgs e)
        {
            if (directionDrawn)
            {
                return;
            }

            destinationPoint = map.CameraPosition.Target;
            var key = Resources.GetString(Resource.String.mapkey);
            var address = await mapHelper.FindCoordinateAddress(destinationPoint, key);
            if (!string.IsNullOrEmpty(address))
            {
                placeTextView.Text = address;
            }
            else
            {
                placeTextView.Text = "Where to?";
            }
        }

        private void Map_CameraMoveStarted(object sender, GoogleMap.CameraMoveStartedEventArgs e)
        {
            if (directionDrawn)
            {
                return;
            }

            placeTextView.Text = "Setting new location";
        }

        public bool CheckPermission()
        {
            var permissionGranted = false;
            if (AndroidX.Core.App.ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) == Android.Content.PM.Permission.Granted &&
                AndroidX.Core.App.ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == Android.Content.PM.Permission.Granted)
            {
                permissionGranted = true;
            }
            return permissionGranted;
        }

        public async void DisplayLocation()
        {
            if (locationProviderClient == null)
            {
                locationProviderClient = LocationServices.GetFusedLocationProviderClient(this);
            }

            myLastLocation = await locationProviderClient.GetLastLocationAsync();
            if (myLastLocation != null)
            {
                myposition = new Android.Gms.Maps.Model.LatLng(50.15, 21.97);
                map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(myposition, 15));
            }
        }

        public void ShowProgressDialog(string status, bool cancelable)
        {
            ProgressDialog = new ProgressDialogFragment(status);
            ProgressDialog.Cancelable = cancelable;
            var trans = SupportFragmentManager.BeginTransaction();

            ProgressDialog.Show(trans, "Progress");

        }
        public void CloseProgressDialog()
        {
            if (ProgressDialog != null)
            {
                ProgressDialog.Dismiss();
                ProgressDialog = null;
            }
        }
    }
}