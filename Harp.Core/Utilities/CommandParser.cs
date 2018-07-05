using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Humanizer;

namespace Harp.Core.Utilities
{
    /*
     * Todo:
     * - Alotta refactoring
     */

    public static class CommandParser
    {
        public static Command Parse(string command)
        {
            var cmd = new Command();

            var matches = commandRequestTypeFormatMap
                .Select(e => new { regex = Regex.Match(command, e.Key, RegexOptions.IgnoreCase), requestType = e.Value })
                .Where(m => m.regex.Success)
                .ToArray();

            // Validation
            if (matches.Length == 0)
                throw new NoCommandMatchesException();
            else if (matches.Length > 1)
                throw new TooManyCommandMatchesException();

            var match = matches.Single();

            // Request type
            cmd.RequestType = match.requestType;

            var fuzzyEntityName = match.regex.Groups[1].Value;
            var entity = getEntity(fuzzyEntityName);

            // Entity type
            cmd.EntityType = entity;

            // By column (optional)
            // ASSUMPTION: the second command is the 'by-column' part, this'll need to change at some point
            if (match.regex.Groups.Count > 2)
                cmd.ByColumn = match.regex.Groups[2].Value;

            return cmd;
        }

        static Dictionary<string, RequestType> commandRequestTypeFormatMap = new Dictionary<string, RequestType>
        {
            { "get all (.+)", RequestType.GetAll },
            { "get a (.+)", RequestType.GetSingle },
            { "get (.+) by (.+)", RequestType.GetSingle },
        };

        static EntityType getEntity(string fuzzyEntityName)
        {
            var processedFuzzyName = fuzzyEntityName.Trim();
            var namePluralized = processedFuzzyName.Pluralize();
            var nameSingularized = processedFuzzyName.Singularize();

            var matches = new List<EntityType>();

            foreach (var value in Enum.GetValues(typeof(EntityType)))
            {
                var e = ((EntityType)value);
                var name = e.ToString();

                if (string.Equals(name, namePluralized, StringComparison.InvariantCultureIgnoreCase)
                    || string.Equals(name, nameSingularized, StringComparison.InvariantCultureIgnoreCase))
                {
                    matches.Add(e);
                }

            }

            if (matches.Count == 1)
                return matches.First();
            else if (matches.Count > 1)
                throw new TooManyEntityMatchesException();
            else
                throw new NoEntityMatchesException();

        }

    }

    public class Command
    {
        public RequestType RequestType { get; set; }
        public EntityType EntityType { get; set; }
        public string ByColumn { get; set; }
    }

    public enum EntityType
    {
        Dogs
    }

    public enum RequestType
    {
        GetAll,
        GetSingle,
        GetFiltered,
    }


    public class TooManyEntityMatchesException : Exception { }
    public class NoEntityMatchesException : Exception { }

    public class TooManyCommandMatchesException : Exception { }
    public class NoCommandMatchesException : Exception { }

}
