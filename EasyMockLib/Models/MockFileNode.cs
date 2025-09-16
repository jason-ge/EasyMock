using EasyMockLib.MatchingPolicies;

namespace EasyMockLib.Models
{
    public class MockFileNode
    {
        public MockFileNode()
        {
            Nodes = new List<MockNode>();
        }

        public string MockFile { get; set; }
        public List<MockNode> Nodes { get; set; }

        public MockNode GetMock(ServiceType serviceType, string url, 
            string method, string requestContent, IMatchingPolicy matchingPolicy)
        {
            var mocks = this.Nodes.Where(m =>
            m.ServiceType == serviceType &&
            m.MethodName.Equals(method, StringComparison.OrdinalIgnoreCase) &&
            MatchUrl(m, url));

            if (mocks.Any())
            {
                if (mocks.Count() == 1)
                {
                    return mocks.ElementAt(0);
                }

                MockNode? matchingMock = matchingPolicy.Apply(requestContent, mocks, url, method);
                if (matchingMock != null)
                {
                    return matchingMock;
                }
                else
                {
                    return mocks.First();
                }
            }
            return null!;
        }

        private bool MatchUrl(MockNode mock, string url)
        {
            if (mock.Url.Equals(url, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else if (url.IndexOf('?') > -1)
            {
                var incomingQuery = ConvertQueryToDictionary(url);
                var mockQuery = ConvertQueryToDictionary(mock.Url);
                return incomingQuery.ContainsEqual(mockQuery);
            }
            else
            {
                return false;
            }
        }
        private Dictionary<string, string> ConvertQueryToDictionary(string query)
        {
            if (query.IndexOf('?') == -1)
            {
                return [];
            }
            else
            {
                return query.Substring(query.IndexOf('?') + 1).Split('&').ToDictionary(
                x => x.Split('=', StringSplitOptions.RemoveEmptyEntries)[0],
                x => x.Split('=', StringSplitOptions.RemoveEmptyEntries)[1],
                StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
