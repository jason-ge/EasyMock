using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace EasyMockLib.Models
{
    public class Body
    {
        [XmlElement("Content")]
        public XmlCDataSection ContentCData
        {
            get
            {
                XmlDocument document = new XmlDocument();
                return document.CreateCDataSection(Content);
            }
            set
            {
                Content = value?.Value ?? string.Empty;
            }
        }
        [XmlIgnore]
        public string Content { get; set; }

        [XmlIgnore]
        public object ContentObject { get; set; }
    }
}