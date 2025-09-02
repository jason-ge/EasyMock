//using EasyMockLib.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace EasyMockLib.MatchingPolicies
//{
//    internal class HttpGetMatchingPolicy : IMatchingPolicy
//    {
//        public MockNode Apply(string requestContent, IEnumerable<MockNode> mocks)
//        {
//            // The requestContent passed in should be the AbsolutePath like /api/foo?test=true&test2=false
//            var incomingQuery = ConvertQueryToDictionary(requestContent);
//            MockNode matchingMock = null;
//            foreach (var mock in mocks)
//            {
//                var mockQuery = ConvertQueryToDictionary(mock.Url);
//                if (mockQuery.Keys.Count == 0 && matchingMock == null)
//                {
//                    // Make it default mock if there is no query string
//                    matchingMock = mock;
//                }
//                if (mockQuery.Keys.Count == incomingQuery.Keys.Count)
//                {
//                    if (mockQuery.Keys.SequenceEqual(incomingQuery.Keys))
//                    {
//                        // Have the same set of query string keys
//                        matchingMock = mock;
//                        if (mockQuery.Values.SequenceEqual(incomingQuery.Values))
//                        {
//                            // exact match of all query string keys and values
//                            return matchingMock;
//                        }
//                    }
//                }
//            }
//            return matchingMock;
//        }
//        private Dictionary<string, string> ConvertQueryToDictionary(string query)
//        {
//            if (query.IndexOf('?') == -1)
//            {
//                return new Dictionary<string, string>();
//            }
//            else
//            {
//                return query.Substring(query.IndexOf('?') + 1).Split('&').ToDictionary(x => x.Split('=', StringSplitOptions.RemoveEmptyEntries)[0], x => x.Split('=', StringSplitOptions.RemoveEmptyEntries)[1]);
//            }
//        }
//    }
//}
