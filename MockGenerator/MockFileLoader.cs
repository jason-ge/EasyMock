using EasyMockLib.Models;
using System.Xml.Serialization;

namespace MockGenerator
{
    internal class MockFileLoader
    {
        public MockFileLoader() { }

        public MockFileNode LoadMockFile(string path)
        {
            var mockFileNode = new MockFileNode()
            {
                MockFile = path,
                Nodes = ParseMockXml(path)
            };

            return mockFileNode;
        }

        private List<MockNode> ParseMockXml(string path)
        {
            var serializer = new XmlSerializer(typeof(List<MockNode>));
            using (var xml = File.OpenRead(path))
            {
                return (List<MockNode>)serializer.Deserialize(xml);
            }
        }
    }
}
