using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinFS.CAVModels
{
    public class JoinfsSession
    {
        public int Id { get; set; }
        public string Nickname { get; set; }
        public string Callsign { get; set; }
        public DateTime LastUpdated { get; set; }

        // Constructor
        public JoinfsSession(int id, string nickname, string callsign, DateTime lastUpdated)
        {
            Id = id;
            Nickname = nickname;
            Callsign = callsign;
            LastUpdated = lastUpdated;
        }
    }
}
