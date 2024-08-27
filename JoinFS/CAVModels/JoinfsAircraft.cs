using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinFS.CAVModels
{
    public class JoinfsAircraft
    {
        public int? Id { get; set; }
        public string Callsign { get; set; }
        public string Owner { get; set; }
        public string OwnerNode { get; set; }
        public float? Distance { get; set; }
        public float? Heading { get; set; }
        public float? Altitude { get; set; }
        public float? Latitude { get; set; }
        public float? Longitude { get; set; }
        public string Model { get; set; }
        public DateTime LastUpdated { get; set; }

        // Constructor
        public JoinfsAircraft(string callsign, string owner, string ownernode, float? distance, float? heading, float? altitude, float? latitude, float? longitude, string model, DateTime lastUpdated, int? id = null)
        {
            Id = id;
            Callsign = callsign;
            Owner = owner;
            OwnerNode = ownernode;
            Distance = distance;
            Heading = heading;
            Altitude = altitude;
            Latitude = latitude;
            Longitude = longitude;
            Model = model;
            LastUpdated = lastUpdated;
        }
    }
}
