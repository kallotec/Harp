using Harp.Core.Utilities;
using Harp.Core.Models;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text;
using Humanizer;
using Harp.Core.Infrastructure;

namespace Harp.Core.Services
{
    public class HarpSynchronizer
    {
        public HarpSynchronizer(ISql sql, StringBuilder trace)
        {
            this.sql = sql;
            this.trace = trace;
        }

        ISql sql;
        StringBuilder trace;


        public SyncResults Synchronize(HarpFile mapFile)
        {
            var results = new SyncResults();

            var isValid = sql.ConfigureAndTest(mapFile.Config.SqlConnectionString);
            if (!isValid)
            {
                results.Code = SynchronizeResultCode.InvalidSqlConnectionString;
                return results;
            }

            try
            {
                foreach (var entry in mapFile.Entities)
                {
                    int? tableId;
                    var entity = entry.Value;

                    // Table
                    if (string.IsNullOrWhiteSpace(entity.Table))
                    {
                        // Try match to an existing table
                        var tables = sql.GetAllTables();

                        var matches = tables.Where(
                            t => StringMatcher.IsAFuzzyMatch(
                                getObjectName(t.fullName), entity.Name
                            ));

                        if (matches.Count() > 1)
                        {
                            // Error: too many matches, can't decide.
                            results.Code = SynchronizeResultCode.EntityNameMatchesTooManyTables;
                            return results;
                        }
                        else if (matches.Count() == 0)
                        {
                            // Error: no matches
                            results.Code = SynchronizeResultCode.EntityNameMatchesNoTables;
                            return results;
                        }

                        var tableMatch = matches.Single();

                        // Capture table name on entity
                        entity.Table = tableMatch.fullName;
                        results.WasUpdated = true;
                        trace.AppendLine($"Table match: {entity.Table}");
                    }

                    tableId = sql.GetTableObjectId(entity.Table);
                    if (tableId == null)
                    {
                        results.Code = SynchronizeResultCode.MatchedTableDoesNotExist;
                        return results;
                    }

                    var columnsForEntity = sql.GetColumnNames(tableId.Value);

                    // Columns, unmapped
                    if (entity.Properties.Any(p => string.IsNullOrWhiteSpace(p.Value)))
                    {
                        for (int x = 0; x < entity.Properties.Count; x++)
                        {
                            var propEntry = entity.Properties.ElementAt(x);

                            // Excludes any already mapped
                            if (propEntry.Value != null)
                                continue;

                            var prop = propEntry.Value;

                            var availableMatches = columnsForEntity.Where(c => !entity.Properties.Any(p => p.Value == c));
                            var matches = availableMatches.Where(c => StringMatcher.IsAFuzzyMatch(c, propEntry.Key));

                            if (matches.Any())
                            {
                                var match = matches.First();
                                entity.Properties[propEntry.Key] = match;
                                results.WasUpdated = true;
                                trace.AppendLine($"Property match: {propEntry.Key} = {match}");
                            }
                            else
                            {
                                // Error: No matches for column
                                results.Code = SynchronizeResultCode.ColumnMatchingError;
                                return results;
                            }

                        }

                        // Track the unmapped
                        var unmappedCols = columnsForEntity.Except(entity.Properties.Select(p => p.Value));
                        results.UnmappedTableColumns.AddRange(unmappedCols);
                    }
                    else if (entity.Properties.Count() == 0)
                    {
                        // Autopopulate property list with all available columns
                        foreach (var column in columnsForEntity)
                        {
                            var humanName = column.Humanize();
                            entity.AddProperty(humanName, column);

                            results.WasUpdated = true;
                            trace.AppendLine($"Property match: {humanName} = {column}");
                        }
                    }

                    // Columns, mapped
                    var allExistingMappedColumnsExist = entity.Properties.All(p => columnsForEntity.Any(c => string.Equals(c, p.Value, StringComparison.OrdinalIgnoreCase)));
                    if (!allExistingMappedColumnsExist)
                    {
                        results.Code = SynchronizeResultCode.MatchedColumnDoesNotExist;
                        return results;
                    }


                    var procsForEntity = sql.GetStoredProcsThatRefEntity(entity.Table);

                    // Behaviours, unmapped
                    if (entity.Behaviors.Any(b => string.IsNullOrWhiteSpace(b.Value)))
                    {
                        for (var x = 0; x < entity.Behaviors.Count; x++)
                        {
                            var behaveEntry = entity.Behaviors.ElementAt(x);

                            // ignore already mapped behaviors
                            if (behaveEntry.Value != null)
                                continue;

                            var behavior = behaveEntry.Value;

                            // Excludes any already mapped
                            var availableMatches = procsForEntity.Where(p => !entity.Behaviors.Any(b => b.Value == p.fullName));

                            var matches = availableMatches.Select(p => new ProcName(p.fullName))
                                                          .OrderByDescending(p =>
                                                          {
                                                              // Remove entity name from proc's name, to make it more likely to get matched
                                                              // since there's less character changes required for a total match.
                                                              // e.g. 1 = get dogs by id = get by id (becomes the most closest match)
                                                              //      2 = get cats by id = get cats by id
                                                              var processed = p.HumanizedName.Replace((" " + entity.Name + " "), string.Empty);
                                                              return stringCompareScore(behaveEntry.Key, processed);
                                                          }).ToArray();
                            if (matches.Any())
                            {
                                var match = matches.First().FullName;
                                entity.Behaviors[behaveEntry.Key] = match;
                                results.WasUpdated = true;
                                trace.AppendLine($"Behavior match: {behaveEntry.Key} = {match}");
                            }
                        }

                        // Track the unmapped
                        var unmappedCols = procsForEntity.Select(p => p.fullName)
                                                .Except(entity.Behaviors.Select(b => b.Value));
                        results.UnmappedStoredProcs.AddRange(unmappedCols);

                        // Ensure all stored procs that have been matched 
                        // (either from this process or manually) exist.
                        var allExistingMappedProcsExist = entity.Behaviors.All(b => procsForEntity.Any(pr => string.Equals(pr.fullName, b.Value, StringComparison.OrdinalIgnoreCase)));
                        if (!allExistingMappedProcsExist)
                        {
                            results.Code = SynchronizeResultCode.MatchedProcDoesNotExist;
                            return results;
                        }

                    }
                    else if (entity.Behaviors.Count() == 0)
                    {
                        // Autopopulate behavior list with all available procs
                        foreach (var proc in procsForEntity)
                        {
                            var humanName = removeWord(entity.Name, getObjectName(proc.fullName).Humanize()).Humanize();

                            if (string.IsNullOrWhiteSpace(humanName))
                            {
                                results.Code = SynchronizeResultCode.ProcMatchingError;
                                return results;
                            }

                            entity.AddBehavior(humanName, proc.fullName);
                            results.WasUpdated = true;
                            trace.AppendLine($"Behavior match: {humanName} = {proc}");
                        }
                    }

                }

                // Validate
                foreach (var entity in mapFile.Entities.Select(e => e.Value))
                {
                    if (!entity.Behaviors.Any())
                    {
                        results.Code = SynchronizeResultCode.AtLeastOneBehaviorRequired;
                        return results;
                    }
                }

                results.Code = mapFile.Entities.All(e => e.Value.IsFullyMapped)
                    ? SynchronizeResultCode.OK
                    : SynchronizeResultCode.NotAllMapped;
            }
            catch (Exception ex)
            {
                trace.AppendLine($"{ex.GetType().Name}: {ex.ToString()}");
                results.Code = SynchronizeResultCode.UnknownError;
            }

            return results;
        }

        string generateEntityClass(string tableName, string rootNamespace, string[] columnNames)
        {
            var className = translateTableNameToClassName(tableName);

            var symbolProps = "{{{properties}}}";
            var template = @"
using System;
namespace " + rootNamespace + @"
{
    public class " + className + @"
    {
        " + symbolProps + @"
    }
}
";
            var propertyNames = columnNames.Select(translateColumnToPropName);
            var propertyDefinitions = propertyNames.Select(translateNameToPropDefinition);

            var classDefinition = template.Replace(symbolProps, string.Join(Environment.NewLine + "\t\t", propertyDefinitions));

            return classDefinition;
        }

        string translateTableNameToClassName(string tableName)
        {
            return tableName.Humanize(LetterCasing.Title).Singularize().Dehumanize();
        }

        string translateColumnToPropName(string columnName)
        {
            return columnName.Humanize(LetterCasing.Title).Dehumanize();
        }

        string translateNameToPropDefinition(string propertyName)
        {
            return "public string " + propertyName + " { get; set; }";
        }

        string getObjectName(string fullTableName)
        {
            if (!fullTableName.Contains("."))
                return fullTableName;

            var components = fullTableName.Split(".", StringSplitOptions.RemoveEmptyEntries);
            return components.Last();
        }

        string removeWord(string needle, string haystack)
        {
            var variations = new[] {
                needle.Pluralize(),
                needle.Singularize(),
                needle
            };

            foreach (var variation in variations.Where(v => !string.IsNullOrWhiteSpace(v)))
            {
                if (haystack.StartsWith(variation + " ", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(haystack))
                    haystack = haystack.Substring(variation.Length);

                if (haystack.EndsWith(" " + variation, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(haystack))
                    haystack = haystack.Substring(0, (haystack.Length - (variation.Length + 1)));

                if (!string.IsNullOrWhiteSpace(haystack))
                    haystack = haystack.Replace(variation, " ", StringComparison.OrdinalIgnoreCase);
            }

            return haystack;
        }


        double stringCompareScore(string word, string abbrv, double fuzzines = 0)
        {
            double total_char_score = 0, abbrv_size = abbrv.Length,
                fuzzies = 1, final_score, abbrv_score;
            int word_size = word.Length;
            bool start_of_word_bonus = false;

            //If strings are equal, return 1.0
            if (word == abbrv) return 1.0;

            int index_in_string,
                index_char_lowercase,
                index_char_uppercase,
                min_index;
            double char_score;
            string c;
            for (int i = 0; i < abbrv_size; i++)
            {
                c = abbrv[i].ToString();
                index_char_uppercase = word.IndexOf(c.ToUpper());
                index_char_lowercase = word.IndexOf(c.ToLower());
                min_index = Math.Min(index_char_lowercase, index_char_uppercase);

                //Finds first valid occurrence
                //In upper or lowercase
                index_in_string = min_index > -1 ?
                    min_index : Math.Max(index_char_lowercase, index_char_uppercase);

                //If no value is found
                //Check if fuzzines is allowed
                if (index_in_string == -1)
                {
                    if (fuzzines > 0)
                    {
                        fuzzies += 1 - fuzzines;
                        continue;
                    }
                    else return 0;
                }
                else
                    char_score = 0.1;

                //Check if current char is the same case
                //Then add bonus
                if (word[index_in_string].ToString() == c) char_score += 0.1;

                //Check if char matches the first letter
                //And add bonnus for consecutive letters
                if (index_in_string == 0)
                {
                    char_score += 0.6;

                    //Check if the abbreviation
                    //is in the start of the word
                    start_of_word_bonus = i == 0;
                }
                else
                {
                    // Acronym Bonus
                    // Weighing Logic: Typing the first character of an acronym is as if you
                    // preceded it with two perfect character matches.
                    if (word.ElementAtOrDefault(index_in_string - 1).ToString() == " ") char_score += 0.8;
                }


                //Remove the start of string, so we don't reprocess it
                word = word.Substring(index_in_string + 1);

                //sum chars scores
                total_char_score += char_score;
            }

            abbrv_score = total_char_score / abbrv_size;

            //Reduce penalty for longer words
            final_score = ((abbrv_score * (abbrv_size / word_size)) + abbrv_score) / 2;

            //Reduce using fuzzies;
            final_score = final_score / fuzzies;

            //Process start of string bonus
            if (start_of_word_bonus && final_score <= 0.85)
                final_score += 0.15;

            return final_score;
        }


        class ProcName
        {
            public ProcName(string fullName)
            {
                FullName = fullName;

                if (FullName.Contains("."))
                {
                    var components = FullName.Split(".", StringSplitOptions.RemoveEmptyEntries);
                    ShortName = components.Last();
                }
                else
                {
                    ShortName = FullName;
                }

                HumanizedName = ShortName.Humanize();

            }

            public string FullName { get; set; }
            public string ShortName { get; set; }
            public string HumanizedName { get; set; }
        }

        public enum SynchronizeResultCode
        {
            UnknownError,
            OK,
            NotAllMapped,
            InvalidSqlConnectionString,
            EntityNameMatchesTooManyTables,
            EntityNameMatchesNoTables,
            MatchedTableDoesNotExist,
            MatchedColumnDoesNotExist,
            MatchedProcDoesNotExist,
            ColumnMatchingError,
            ProcMatchingError,
            AtLeastOneBehaviorRequired,
        }

        public class SyncResults
        {
            public SyncResults()
            {
                UnmappedTableColumns = new List<string>();
                UnmappedStoredProcs = new List<string>();
            }
            public SyncResults(SynchronizeResultCode Code) : this()
            {
                this.Code = Code;
            }

            public SynchronizeResultCode Code { get; set; }
            public bool WasUpdated { get; set; }
            public List<string> UnmappedTableColumns { get; private set; }
            public List<string> UnmappedStoredProcs { get; private set; }

            public static SyncResults Create(SynchronizeResultCode Code)
            {
                return new SyncResults(Code);
            }

        }

    }

}
