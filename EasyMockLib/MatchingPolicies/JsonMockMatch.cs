using EasyMockLib.Models;
using Newtonsoft.Json.Linq;

namespace EasyMockLib.MatchingPolicies
{
    public class JsonMockMatch : BaseMockMatch, IMatch
    {
        public MockNode? MatchMock(string requestContent, string url, IEnumerable<MockNode> mockNodes, List<string>? elementsToCompare)
        {
            var mocks = MatchByQuery(mockNodes, url);

            if (mocks.Any())
            {
                if (mocks.Count() == 1 || elementsToCompare == null || elementsToCompare.Count == 0)
                {
                    return mocks.ElementAt(0);
                }
                JObject jIncomingRequest = JObject.Parse(requestContent);
                foreach (var mock in mocks)
                {
                    var jMockRequest = JObject.Parse(mock.Request.RequestBody.Content);
                    bool match = true;
                    foreach (var elementName in elementsToCompare)
                    {
                        var element1 = jIncomingRequest.SelectToken(elementName);
                        var element2 = jMockRequest.SelectToken(elementName);
                        if (!JToken.DeepEquals(element1, element2))
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        return mock;
                    }
                }
                return mocks.ElementAt(0);
            }
            return null;
        }
    }
}
