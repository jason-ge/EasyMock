using EasyMockLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyMockLib.MatchingPolicies
{
    internal interface IMatchingPolicy
    {
        MockNode Apply(string requestContent, IEnumerable<MockNode> mocks);
    }
}
