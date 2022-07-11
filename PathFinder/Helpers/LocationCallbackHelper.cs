using System;
using Android.Gms.Location;

namespace PathFinder.Helpers
{
    public class LocationCallbackHelper : LocationCallback
    {
        public event EventHandler<OnLocationCapturedEventArgs> OnLocationFound;
        public class OnLocationCapturedEventArgs : EventArgs
        {
            public Android.Locations.Location Location { get; set; }
        }

        public override void OnLocationResult(LocationResult result)
        {
            if (result.Locations.Count != 0)
            {
                OnLocationFound?.Invoke(this, new OnLocationCapturedEventArgs { Location = result.Locations[0] });
            }
        }

        public override void OnLocationAvailability(LocationAvailability locationAvailability)
        {

        }

    }
}