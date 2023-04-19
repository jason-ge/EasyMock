using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyMockLib.Models
{
    public class Request
    {
        public Request()
        {
            RequestBody = new Body();
        }
        public ServiceType RequestType { get; set; }
        public string MethodName { get; set; }
        public string ServiceName { get; set; }
        public string Url { get; set; }
        public Body RequestBody { get; set; }
    }
}
