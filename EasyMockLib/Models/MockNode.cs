using System.Xml.Serialization;

namespace EasyMockLib.Models
{
    public class MockNode
    {
        public Request? Request { get; set; }
        public Response? Response { get; set; }
        public required string Url { get; set; }
        public required string MethodName { get; set; }
        public string? Description { get; set; }
        public ServiceType ServiceType { get; set; }
        [XmlIgnore]
        public MockFileNode? Parent { get; set; }
    }
}
