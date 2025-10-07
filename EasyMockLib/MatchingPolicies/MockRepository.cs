using EasyMockLib.Models;

namespace EasyMockLib.MatchingPolicies
{
    public class MockRepository
    {
        private readonly Dictionary<string, List<MockNode>> _staticMockNodeLookup = [];
        private readonly Dictionary<string, List<MockNode>> _dynamicMockNodeLookup = [];
        private readonly Dictionary<string, Dictionary<string, List<string>>> _jsonMatchConfig;
        private readonly Dictionary<string, Dictionary<string, List<string>>> _xmlMatchConfig;
        private readonly JsonMockMatch _jsonMatch = new();
        private readonly XmlMockMatch _xmlMatch = new();
        public MockRepository(Dictionary<string, Dictionary<string, List<string>>> jsonMatchConfig, Dictionary<string, Dictionary<string, List<string>>> xmlMatchConfig)
        {
            _jsonMatchConfig = jsonMatchConfig;
            _xmlMatchConfig = xmlMatchConfig;
        }

        public void BuildRepository(IEnumerable<MockFileNode> mockFileNodes)
        {
            _staticMockNodeLookup.Clear();
            _dynamicMockNodeLookup.Clear();
            foreach (var node in mockFileNodes)
            {
                UpdateMockLookup(node);
            }
        }

        public bool IsMockExist(string url, string method)
        {
            var urlLower = url.ToLower();
            var urlLowerPath = urlLower.IndexOf('?') > -1 ? urlLower.Substring(0, urlLower.IndexOf("?")) : urlLower;
            lock (_staticMockNodeLookup)
            {
                if (_staticMockNodeLookup.ContainsKey(urlLowerPath))
                {
                    if (_staticMockNodeLookup[urlLowerPath].Any(m => m.MethodName.Equals(method, StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }
            }
            lock (_dynamicMockNodeLookup)
            {
                foreach (var dynamicUrl in _dynamicMockNodeLookup.Keys)
                {
                    if (IsDynamicUrlMatch(dynamicUrl, urlLowerPath))
                    {
                        if (_dynamicMockNodeLookup[dynamicUrl].Any(m => m.MethodName.Equals(method, StringComparison.OrdinalIgnoreCase)))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public MockNode? GetMock(ServiceType serviceType, string url, string method, string requestContent)
        {
            var elementsToMatch = GetElementsToMatch(serviceType == ServiceType.REST ? _jsonMatchConfig : _xmlMatchConfig, url, method);
            var urlLower = url.ToLower();
            var urlLowerPath = urlLower.IndexOf('?') > -1 ? urlLower.Substring(0, urlLower.IndexOf("?")) : urlLower;
            bool foundInStaticLookup = false;
            lock (_staticMockNodeLookup)
            {
                if (_staticMockNodeLookup.ContainsKey(urlLowerPath))
                {
                    foundInStaticLookup = true;
                    var mocks = _staticMockNodeLookup[urlLowerPath]
                        .Where(m => m.ServiceType == serviceType && m.MethodName.Equals(method, StringComparison.OrdinalIgnoreCase));
                    {
                        if (serviceType == ServiceType.REST)
                        {
                            return _jsonMatch.MatchMock(requestContent, urlLower, mocks, elementsToMatch);
                        }
                        else
                        {
                            return _xmlMatch.MatchMock(requestContent, urlLower, mocks, elementsToMatch);
                        }
                    }
                }
            }
            if (!foundInStaticLookup)
            {
                lock (_dynamicMockNodeLookup)
                {
                    foreach (var dynamicUrl in _dynamicMockNodeLookup.Keys)
                    {
                        if (IsDynamicUrlMatch(dynamicUrl, urlLowerPath))
                        {
                            var mocks = _dynamicMockNodeLookup[dynamicUrl]
                                .Where(m => m.ServiceType == serviceType && m.MethodName.Equals(method, StringComparison.OrdinalIgnoreCase));
                            {
                                if (serviceType == ServiceType.REST)
                                {
                                    return _jsonMatch.MatchMock(requestContent, urlLower, mocks, elementsToMatch);
                                }
                                else
                                {
                                    return _xmlMatch.MatchMock(requestContent, urlLower, mocks, elementsToMatch);
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        #region Private methods
        private bool IsDynamicUrlMatch(string dynamicUrl, string requestUrl)
        {
            var dynamicUrlParts = dynamicUrl.Split("/");
            var requestUrlParts = requestUrl.Split("/");
            if (dynamicUrlParts.Length != requestUrlParts.Length)
            {
                return false;
            }
            foreach (var (part, index) in dynamicUrlParts.Select((value, i) => (value, i)))
            {
                if (part.Equals("(*)", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (!part.Equals(requestUrlParts[index], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true;
        }

        private List<string>? GetElementsToMatch(Dictionary<string, Dictionary<string, List<string>>> matchConfig, string url, string method)
        {
            if (matchConfig.ContainsKey(url) && matchConfig[url].ContainsKey(method))
            {
                return matchConfig[url][method];
            }
            return null;
        }

        private void UpdateStaticMockLookup(MockNode mockNode, string urlLowerPath)
        {
            lock (_staticMockNodeLookup)
            {
                if (!_staticMockNodeLookup.ContainsKey(urlLowerPath))
                {
                    _staticMockNodeLookup.Add(urlLowerPath, [mockNode]);
                }
                else
                {
                    _staticMockNodeLookup[urlLowerPath].Add(mockNode);
                }
            }
        }

        private void UpdateDynamicMockLookup(MockNode mockNode, string urlLowerPath)
        {
            lock (_dynamicMockNodeLookup)
            {
                if (!_dynamicMockNodeLookup.ContainsKey(urlLowerPath))
                {
                    _dynamicMockNodeLookup.Add(urlLowerPath, [mockNode]);
                }
                else
                {
                    _dynamicMockNodeLookup[urlLowerPath].Add(mockNode);
                }
            }
        }

        private void UpdateMockLookup(MockFileNode mockFileNode)
        {
            if (mockFileNode == null)
            {
                throw new ArgumentNullException($"{nameof(mockFileNode)} is null");
            }
            foreach (var mockNode in mockFileNode.Nodes)
            {
                var urlLower = mockNode.Url.ToLower();
                var urlLowerPath = urlLower.IndexOf('?') > -1 ? urlLower.Substring(0, urlLower.IndexOf("?")) : urlLower;
                if (urlLowerPath.Contains("(*)", StringComparison.OrdinalIgnoreCase))
                {
                    UpdateDynamicMockLookup(mockNode, urlLowerPath);
                }
                else
                {
                    UpdateStaticMockLookup(mockNode, urlLowerPath);
                }
            }
        }
        #endregion
    }
}
