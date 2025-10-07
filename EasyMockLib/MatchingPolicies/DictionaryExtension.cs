namespace EasyMockLib.MatchingPolicies
{
    internal static class DictionaryExtenstion
    {
        public static bool ContainsEqual(this Dictionary<string, string> container, Dictionary<string, string> containee)
        {
            if (containee == null || containee.Count == 0)
            {
                return true;
            }
            if (containee.Count > container.Count)
            {
                return false;
            }
            foreach (var key in containee.Keys)
            {
                if (container.TryGetValue(key, out string? value))
                {
                    if (containee[key].Equals("(*)") || string.Equals(value, containee[key], StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }
                return false;
            }
            return true;
        }
    }
}