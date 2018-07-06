using System;
using System.Collections.Generic;
using System.Text;
using Humanizer;

namespace Harp.Core.Utilities
{
    public class StringMatcher
    {
        public static bool IsAFuzzyMatch(string fuzzy, string matchTo)
        {
            var pluralized = fuzzy.Pluralize();
            var singularized = fuzzy.Singularize();

            return (string.Equals(pluralized, matchTo, StringComparison.InvariantCultureIgnoreCase)
                 || string.Equals(singularized, matchTo, StringComparison.InvariantCultureIgnoreCase));
        }

    }
}
