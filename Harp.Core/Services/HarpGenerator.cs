using Harp.Core.Utilities;
using Harp.Core.Models;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text;
using Humanizer;

namespace Harp.Core.Services
{
    public class HarpGenerator
    {
        public HarpGenerator()
        {
        }


        public GenerateResults Generate(HarpFile mapFile, out StringBuilder trace)
        {
            trace = new StringBuilder();
            return new GenerateResults(GenerateResultCode.UnknownError);
        }

        string generateEntityClass(string entityName, string rootNamespace, string[] columnNames)
        {
            var symbolProps = "{{{properties}}}";
            var template = @"
using System;
namespace " + rootNamespace + @"
{
    public class " + entityName + @"
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

        string translateColumnToPropName(string columnName)
        {
            return columnName.Humanize(LetterCasing.Title).Dehumanize();
        }

        string translateNameToPropDefinition(string propertyName)
        {
            return "public string " + propertyName + " { get; set; }";
        }

        public enum GenerateResultCode
        {
            UnknownError,
            OK,
        }

        public class GenerateResults
        {
            public GenerateResults()
            {
                UnmappedTableColumns = new List<string>();
                UnmappedStoredProcs = new List<string>();
            }
            public GenerateResults(GenerateResultCode Code) : this()
            {
                this.Code = Code;
            }

            public GenerateResultCode Code { get; set; }
            public List<string> UnmappedTableColumns { get; private set; }
            public List<string> UnmappedStoredProcs { get; private set; }
        }

    }

}
