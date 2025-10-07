using EasyMockLib.MatchingPolicies;
using EasyMockLib.Models;

namespace EasyMockTests
{
    public class MockRepositoryTests
    {
        private readonly MockRepository _mockRepository = new([], []);
        private MockNode CreateMockNode(string url, string description)
        {
            return new MockNode
            {
                Url = url,
                MethodName = "GET",
                ServiceType = ServiceType.REST,
                Description = description
            };
        }

        [SetUp]
        public void Setup() 
        {
            MockFileNode mockFileNode = new MockFileNode("dummy.json");
            mockFileNode.Nodes.Add(CreateMockNode("/api/test", "testcase1"));
            mockFileNode.Nodes.Add(CreateMockNode("/api/test1?a=111&b=222", "testcase2"));
            mockFileNode.Nodes.Add(CreateMockNode("/api/(*)/test", "testcase3"));
            mockFileNode.Nodes.Add(CreateMockNode("/api/(*)/test1?a=111&b=222", "testcase4"));
            mockFileNode.Nodes.Add(CreateMockNode("/api/test1?a=(*)&b=222", "testcase5"));
            _mockRepository.BuildRepository([mockFileNode]);
        }

        [Test]
        public void GetMock_ReturnsNull_WhenNoMatch()
        {
            var jsonMatch = new JsonMockMatch();
            var result = _mockRepository.GetMock(ServiceType.REST, "/api/testx", "GET", "");
            Assert.IsNull(result);
        }
        [Test]
        public void GetMock_ReturnsSingleMatch()
        {
            var jsonMatch = new JsonMockMatch();
            var result = _mockRepository.GetMock(ServiceType.REST, "/api/test", "GET", "");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Description, Is.EqualTo("testcase1"));
        }

        //[Test]
        //public void GetMock_UsesMatchingPolicy_WhenMultipleMatches_AndReturnsNonNull()
        //{
        //    var node = new MockFileNode();
        //    var mock1 = CreateMockNode("/api/test", "GET", ServiceType.REST);
        //    var mock2 = CreateMockNode("/api/test", "GET", ServiceType.REST);
        //    node.Nodes.AddRange([mock1, mock2]);
        //    var policy = new DummyMatchingPolicy { Result = mock2 };
        //    var result = node.GetMock(ServiceType.REST, "/api/test", "GET", "{}", policy);
        //    Assert.That(result, Is.SameAs(mock2));
        //    Assert.That(policy.WasCalled, Is.True);
        //}
        //[Test]
        //public void GetMock_UsesFirstMock_WhenMultipleMatches_AndPolicyReturnsNull()
        //{
        //    var node = new MockFileNode();
        //    var mock1 = CreateMockNode("/api/test", "GET", ServiceType.REST);
        //    var mock2 = CreateMockNode("/api/test", "GET", ServiceType.REST);
        //    node.Nodes.AddRange([mock1, mock2]);
        //    var policy = new DummyMatchingPolicy { Result = null };
        //    var result = node.GetMock(ServiceType.REST, "/api/test", "GET", "{}", policy);
        //    Assert.That(result, Is.SameAs(mock1));
        //    Assert.That(policy.WasCalled, Is.True);
        //}
        //[Test]
        //public void MatchUrl_ExactMatch_ReturnsTrue()
        //{
        //    var node = new MockFileNode();
        //    var mock = CreateMockNode("/api/test", "GET", ServiceType.REST);
        //    node.Nodes.Add(mock);
        //    var policy = new DummyMatchingPolicy();
        //    var result = node.GetMock(ServiceType.REST, "/api/test", "GET", "{}", policy);
        //    Assert.That(result, Is.SameAs(mock));
        //}
        [Test]
        public void MatchUrl_QueryStringMatch_OrderInsensitive_ReturnsTrue()
        {
            var jsonMatch = new JsonMockMatch();
            var result = _mockRepository.GetMock(ServiceType.REST, "/api/test1?a=111&b=222&c=333", "GET", "");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Description, Is.EqualTo("testcase2"));
        }
        [Test]
        public void MatchWildcardUrl_ReturnsResult()
        {
            var jsonMatch = new JsonMockMatch();
            var result = _mockRepository.GetMock(ServiceType.REST, "/api/dummy/test", "GET", "");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Description, Is.EqualTo("testcase3"));
        }
        [Test]
        public void MatchWildcardUrlWithQuery_ReturnsResult()
        {
            var jsonMatch = new JsonMockMatch();
            var result = _mockRepository.GetMock(ServiceType.REST, "/api/dummy/test1?a=111&b=222&c=333", "GET", "");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Description, Is.EqualTo("testcase4"));
            result = _mockRepository.GetMock(ServiceType.REST, "/api/dummy2/test1?a=111&b=222&c=333", "GET", "");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Description, Is.EqualTo("testcase4"));
        }
        [Test]
        public void MatchWildcardUrl_ReturnsNull()
        {
            var jsonMatch = new JsonMockMatch();
            var result = _mockRepository.GetMock(ServiceType.REST, "/api/dummy/dummy2/test?a=111", "GET", "");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void MatchWildcardQuery_ReturnsResult()
        {
            var jsonMatch = new JsonMockMatch();
            var result = _mockRepository.GetMock(ServiceType.REST, "/api/test1?a=222&b=222", "GET", "");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Description, Is.EqualTo("testcase5"));
            result = _mockRepository.GetMock(ServiceType.REST, "/api/test1?a=333&b=222", "GET", "");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Description, Is.EqualTo("testcase5"));
        }
    }
}