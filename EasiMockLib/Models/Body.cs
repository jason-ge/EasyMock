using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyMockLib.Models
{
    public class Body
    {
        public string Content { get; set; }
        public int StartLineNumber { get; set; }
        public int EndLineNumber { get; set; }
        public object ContentObject { get; set; }
    }
}
