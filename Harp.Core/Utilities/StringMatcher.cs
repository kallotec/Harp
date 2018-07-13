using System;
using System.Collections.Generic;
using System.Text;
using Humanizer;

namespace Harp.Core.Utilities
{
    internal class StringMatcher
    {
        public static bool IsAFuzzyMatch(string fuzzy, string matchTo)
        {
            var pluralized = fuzzy.Pluralize();
            var singularized = fuzzy.Singularize();

            return (string.Equals(pluralized, matchTo, StringComparison.OrdinalIgnoreCase)
                 || string.Equals(singularized, matchTo, StringComparison.OrdinalIgnoreCase));
        }

    }
}
