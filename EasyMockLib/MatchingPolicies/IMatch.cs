using EasyMockLib.Models;

namespace EasyMockLib.MatchingPolicies
{
    public interface IMatch
    {
        MockNode? MatchMock(string requestContent, string url, IEnumerable<MockNode> mocks, List<string>? elementsToMatch);
    }
}
