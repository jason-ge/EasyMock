namespace EasyMockLib.Models
{
    public class MockFileNode
    {
        public MockFileNode(string fileName)
        {
            MockFile = fileName;
            Nodes = new List<MockNode>();
        }

        public string MockFile { get; }
        public List<MockNode> Nodes { get; set; }
    }
}
