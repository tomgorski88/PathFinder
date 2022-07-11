using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Gms.Maps.Utils;
using Android.Graphics;
using Java.Util;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace PathFinder.Helpers
{
    public class MapHelpers
    {
        Marker currentPositionMarker;
        private bool isRequestingDirection;

        public async Task<string> FindCoordinateAddress(LatLng position, string mapkey)
        {
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={position.Latitude.ToString()},{position.Longitude.ToString()}&key={mapkey}";
            var placeAddress = "";

            var handler = new HttpClientHandler();
            var httpClient = new HttpClient(handler);
            var result = await httpClient.GetStringAsync(url);

            if (!string.IsNullOrEmpty(result))
            {
                var geoCodeData = JsonConvert.DeserializeObject<GeocodingParser>(result);
                if (geoCodeData.status.Contains("OK"))
                {
                    placeAddress = geoCodeData.results[0].formatted_address;
                }
            }
            return placeAddress;
        }

        public async Task<string> GetDirectionJsonAsync(LatLng location, LatLng destination, string mapkey)
        {
            var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={location.Latitude.ToString()},{location.Longitude.ToString()}&destination={destination.Latitude.ToString()},{destination.Longitude.ToString()}&mode=driving&key={mapkey}";
            var handler = new HttpClientHandler();
            var httpClient = new HttpClient(handler);
            var jsonString = await httpClient.GetStringAsync(url);

            return jsonString;
        }

        public void DrawPolylineOnMap(string json, GoogleMap mainMap)
        {
            Android.Gms.Maps.Model.Polyline mPolyLine;

            var directionData = JsonConvert.DeserializeObject<DirectionParser>(json);
            var durationString = directionData.routes[0].legs[0].duration.text;
            var distanceString = directionData.routes[0].legs[0].distance.text;

            var polylineCode = directionData.routes[0].overview_polyline.points;
            var line = PolyUtil.Decode(polylineCode);

            LatLng firstPoint = line[0];
            LatLng lastPoint = line[line.Count - 1];
            ArrayList routeList = new ArrayList();

            foreach (var item in line)
            {
                routeList.Add(item);
            }

            PolylineOptions polylineOptions = new PolylineOptions()
                .AddAll(routeList)
                .InvokeWidth(10)
                .InvokeColor(Color.Teal)
                .Geodesic(true)
                .InvokeJointType(JointType.Round);

            mPolyLine = mainMap.AddPolyline(polylineOptions);

            // Add markers
            MarkerOptions locationMarkerOption = new MarkerOptions();
            locationMarkerOption.SetPosition(firstPoint);
            locationMarkerOption.SetTitle("My location");
            locationMarkerOption.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueGreen));
            Marker locationmarker = mainMap.AddMarker(locationMarkerOption);

            MarkerOptions destinationMarkerOption = new MarkerOptions();
            destinationMarkerOption.SetPosition(lastPoint);
            destinationMarkerOption.SetTitle("Destination");
            destinationMarkerOption.SetSnippet(durationString + ", " + distanceString);
            locationMarkerOption.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueRed));
            Marker destinationMarker = mainMap.AddMarker(destinationMarkerOption);

            // current location marker
            MarkerOptions positionMarkerOption = new MarkerOptions();
            positionMarkerOption.SetPosition(firstPoint);
            positionMarkerOption.SetTitle("Current Location");
            positionMarkerOption.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.positionmarker));
            positionMarkerOption.Visible(false);
            currentPositionMarker = mainMap.AddMarker(positionMarkerOption);

            CircleOptions locationCircleOption = new CircleOptions();
            locationCircleOption.InvokeCenter(firstPoint);
            locationCircleOption.InvokeRadius(10);
            locationCircleOption.InvokeStrokeColor(Color.Teal);
            locationCircleOption.InvokeFillColor(Color.Teal);
            mainMap.AddCircle(locationCircleOption);

            CircleOptions destinationCircleOption = new CircleOptions();
            destinationCircleOption.InvokeCenter(lastPoint);
            destinationCircleOption.InvokeRadius(10);
            destinationCircleOption.InvokeStrokeColor(Color.Teal);
            destinationCircleOption.InvokeFillColor(Color.Teal);
            mainMap.AddCircle(destinationCircleOption);

            LatLng southwest = new LatLng(directionData.routes[0].bounds.southwest.lat, directionData.routes[0].bounds.southwest.lng);
            LatLng northeast = new LatLng(directionData.routes[0].bounds.northeast.lat, directionData.routes[0].bounds.northeast.lng);

            LatLngBounds tripBounds = new LatLngBounds(southwest, northeast);
            mainMap.AnimateCamera(CameraUpdateFactory.NewLatLngBounds(tripBounds, 150));
            mainMap.UiSettings.ZoomControlsEnabled = true;
            destinationMarker.ShowInfoWindow();
        }

        public async void UpdateLocationToDestination(LatLng currentPosition, LatLng destination, GoogleMap map, string key)
        {
            currentPositionMarker.Visible = true;
            currentPositionMarker.Position = currentPosition;
            map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(currentPosition, 15));

            if (!isRequestingDirection)
            {
                isRequestingDirection = true;
                var json = await GetDirectionJsonAsync(currentPosition, destination, key);
                var directionData = JsonConvert.DeserializeObject<DirectionParser>(json);
                var duration = directionData.routes[0].legs[0].duration.text;
                var distance = directionData.routes[0].legs[0].distance.text;

                currentPositionMarker.Title = "Current location";
                currentPositionMarker.Snippet = $"Your Destination is {duration}, {distance} away";
                currentPositionMarker.ShowInfoWindow();
                isRequestingDirection = false;
            }
        }
        public void AddMarker(LocationMarker locationMarker, GoogleMap map)
        {
            MarkerOptions locationMarkerOption = new MarkerOptions();
            locationMarkerOption.SetPosition(new LatLng(locationMarker.Ltd, locationMarker.Lng));
            locationMarkerOption.SetTitle(locationMarker.Address);
            locationMarkerOption.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueOrange));
            locationMarkerOption.SetSnippet(locationMarker.Address);
            var marker = map.AddMarker(locationMarkerOption);
            marker.ShowInfoWindow();
        }
    }
}