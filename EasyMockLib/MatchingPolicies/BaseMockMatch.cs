using EasyMockLib.Models;

namespace EasyMockLib.MatchingPolicies
{
    public abstract class BaseMockMatch
    {
        protected IEnumerable<MockNode> MatchByQuery(IEnumerable<MockNode> mocks, string url)
        {
            if (!url.Contains('?'))
            {
                return mocks;
            }
            return mocks.Where(m => MatchQuery(m, url)).ToArray();
        }

        protected bool MatchQuery(MockNode mock, string url)
        {
            var incomingQuery = ConvertQueryToDictionary(url);
            var mockQuery = ConvertQueryToDictionary(mock.Url);
            return incomingQuery.ContainsEqual(mockQuery);
        }

        protected Dictionary<string, string> ConvertQueryToDictionary(string query)
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