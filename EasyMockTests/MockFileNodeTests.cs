using NUnit.Framework;
using EasyMockLib.Models;
using EasyMockLib.MatchingPolicies;
using System.Collections.Generic;
using System.Linq;
namespace EasyMockTests.Models
{
    public class MockFileNodeTests
    {
        private class DummyMatchingPolicy : IMatchingPolicy
        {
            public MockNode? Result { get; set; }
            public bool WasCalled { get; private set; }
            public MockNode Apply(string requestContent, IEnumerable<MockNode> mocks, string service, string method)
            {
                WasCalled = true;
                return Result!;
            }
        }
        private MockNode CreateMockNode(string url, string method, ServiceType serviceType)
        {
            return new MockNode
            {
                Url = url,
                MethodName = method,
                ServiceType = serviceType
            };
        }
        [Test]
        public void GetMock_ReturnsNull_WhenNoMatch()
        {
            var node = new MockFileNode();
            var policy = new DummyMatchingPolicy();
            var result = node.GetMock(ServiceType.REST, "/api/test", "GET", "{}", policy);
            Assert.IsNull(result);
        }
        [Test]
        public void GetMock_ReturnsSingleMatch()
        {
            var node = new MockFileNode();
            var mock = CreateMockNode("/api/test", "GET", ServiceType.REST);
            node.Nodes.Add(mock);
            var policy = new DummyMatchingPolicy();
            var result = node.GetMock(ServiceType.REST, "/api/test", "GET", "{}", policy);
            Assert.That(result, Is.SameAs(mock));
        }
        [Test]
        public void GetMock_UsesMatchingPolicy_WhenMultipleMatches_AndReturnsNonNull()
        {
            var node = new MockFileNode();
            var mock1 = CreateMockNode("/api/test", "GET", ServiceType.REST);
            var mock2 = CreateMockNode("/api/test", "GET", ServiceType.REST);
            node.Nodes.AddRange([mock1, mock2]);
            var policy = new DummyMatchingPolicy { Result = mock2 };
            var result = node.GetMock(ServiceType.REST, "/api/test", "GET", "{}", policy);
            Assert.That(result, Is.SameAs(mock2));
            Assert.That(policy.WasCalled, Is.True);
        }
        [Test]
        public void GetMock_UsesFirstMock_WhenMultipleMatches_AndPolicyReturnsNull()
        {
            var node = new MockFileNode();
            var mock1 = CreateMockNode("/api/test", "GET", ServiceType.REST);
            var mock2 = CreateMockNode("/api/test", "GET", ServiceType.REST);
            node.Nodes.AddRange([mock1, mock2]);
            var policy = new DummyMatchingPolicy { Result = null };
            var result = node.GetMock(ServiceType.REST, "/api/test", "GET", "{}", policy);
            Assert.That(result, Is.SameAs(mock1));
            Assert.That(policy.WasCalled, Is.True);
        }
        [Test]
        public void MatchUrl_ExactMatch_ReturnsTrue()
        {
            var node = new MockFileNode();
            var mock = CreateMockNode("/api/test", "GET", ServiceType.REST);
            node.Nodes.Add(mock);
            var policy = new DummyMatchingPolicy();
            var result = node.GetMock(ServiceType.REST, "/api/test", "GET", "{}", policy);
            Assert.That(result, Is.SameAs(mock));
        }
        [Test]
        public void MatchUrl_QueryStringMatch_OrderInsensitive_ReturnsTrue()
        {
            var node = new MockFileNode();
            var mock = CreateMockNode("/api/test?a=1&b=2", "GET", ServiceType.REST);
            node.Nodes.Add(mock);
            var policy = new DummyMatchingPolicy();
            var result = node.GetMock(ServiceType.REST, "/api/test?b=2&a=1", "GET", "{}", policy);
            Assert.That(result, Is.SameAs(mock));
        }
        [Test]
        public void MatchUrl_NoMatch_ReturnsNull()
        {
            var node = new MockFileNode();
            var mock = CreateMockNode("/api/other", "GET", ServiceType.REST);
            node.Nodes.Add(mock);
            var policy = new DummyMatchingPolicy();
            var result = node.GetMock(ServiceType.REST, "/api/test", "GET", "{}", policy);
            Assert.IsNull(result);
        }
        [Test]
        public void MatchUrl_QueryStringPartialMatch_ReturnsFalse()
        {
            var node = new MockFileNode();
            var mock = CreateMockNode("/api/test?a=1&b=2", "GET", ServiceType.REST);
            node.Nodes.Add(mock);
            var policy = new DummyMatchingPolicy();
            var result = node.GetMock(ServiceType.REST, "/api/test?a=1", "GET", "{}", policy);
            Assert.IsNull(result);
        }
        [Test]
        public void MatchUrl_QueryStringPartialMatch_ReturnsTrue()
        {
            var node = new MockFileNode();
            var mock = CreateMockNode("/api/test?a=1&b=2", "GET", ServiceType.REST);
            node.Nodes.Add(mock);
            var policy = new DummyMatchingPolicy();
            var result = node.GetMock(ServiceType.REST, "/api/test?a=1&c=3&b=2", "GET", "{}", policy);
            Assert.That(result, Is.SameAs(mock));
        }
        [Test]
        public void MatchUrl_NoQueryStringInMockMatch_ReturnsTrue()
        {
            var node = new MockFileNode();
            var mock = CreateMockNode("/api/test", "GET", ServiceType.REST);
            node.Nodes.Add(mock);
            var policy = new DummyMatchingPolicy();
            var result = node.GetMock(ServiceType.REST, "/api/test?a=1&c=3&b=2", "GET", "{}", policy);
            Assert.That(result, Is.SameAs(mock));
        }
    }
}