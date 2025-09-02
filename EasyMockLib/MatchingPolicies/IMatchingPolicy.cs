using EasyMockLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyMockLib.MatchingPolicies
{
    public interface IMatchingPolicy
    {
        MockNode Apply(string requestContent, IEnumerable<MockNode> mocks, string service, string method);
    }
}
