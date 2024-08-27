using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinFS.CAVModels
{
    public class JoinfsSession
    {
        public int? Id { get; set; }
        public string Node { get; set; }
        public string Nickname { get; set; }
        public string Callsign { get; set; }
        public string Simulator { get; set; }
        public DateTime Connected { get; set; }
        public DateTime LastUpdated { get; set; }

        // Constructor
        public JoinfsSession(string node, string nickname, string callsign, string simulator, DateTime connected, DateTime lastUpdated, int? id = null)
        {
            Id = id;
            Node = node;
            Nickname = nickname;
            Callsign = callsign;
            Simulator = simulator;
            Connected = connected;
            LastUpdated = lastUpdated;
        }
    }
}
