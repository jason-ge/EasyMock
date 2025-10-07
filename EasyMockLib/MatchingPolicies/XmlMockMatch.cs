using EasyMockLib.Models;
using System.Xml.Linq;

namespace EasyMockLib.MatchingPolicies
{
    public class XmlMockMatch : BaseMockMatch, IMatch
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
                XElement xRequestContent = XElement.Parse(requestContent);
                foreach (var mock in mocks)
                {
                    XElement xMockRequest = XElement.Parse(mock.Request.RequestBody.Content);
                    bool match = true;
                    foreach (var elementName in elementsToCompare)
                    {
                        var element1 = FindNode(xRequestContent, elementName);
                        var element2 = FindNode(xMockRequest, elementName);
                        if (element1 == null || element2 == null)
                        {
                            match = false; break;
                        }
                        else if (!XNode.DeepEquals(element1, element2))
                        {
                            match = false; break;
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

        private static XElement? FindNode(XElement root, string elementPath)
        {
            if (string.IsNullOrEmpty(elementPath))
            {
                throw new ArgumentNullException(nameof(elementPath));
            }
            var elementNames = elementPath.Trim().Split('/');
            var elementsLevel1 = root.Descendants().Where(e => e.Name.LocalName.Equals(elementNames[0], StringComparison.OrdinalIgnoreCase));
            foreach (var element in elementsLevel1)
            {
                XElement? elementNext = element;
                int i;
                for (i = 1; i < elementNames.Length; i++)
                {
                    elementNext = elementNext.Elements().Where(e => e.Name.LocalName.Equals(elementNames[i], StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    if (elementNext == null)
                    {
                        break;
                    }
                }
                if (i == elementNames.Length)
                {
                    return elementNext;
                }
            }
            return null;
        }
    }
}
