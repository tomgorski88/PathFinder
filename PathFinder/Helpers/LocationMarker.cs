using System.Collections.Generic;

namespace PathFinder.Helpers
{
    public class Locations
    {
        public List<LocationMarker> LocationMarkers { get; set; }
    }
    public class LocationMarker
    {
        public string Address { get; set; }
        public double Lng { get; set; }
        public double Ltd { get; set; }
    }
}