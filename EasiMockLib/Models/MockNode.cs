using EasyMockLib.MatchingPolicies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EasyMockLib.Models
{
    public class MockNode
    {
        public Request Request { get; set; }
        public Response Response { get; set; }
        public string SimulateException { get; set; }
    }
}
