using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.RepresentationModel;
using System.IO;
using Harp.Core.Models;

namespace Harp.Core.Services
{
    public class HarpFileWriter
    {
        public WriteResult Write(HarpFile map, string outputFilePath, out StringBuilder trace)
        {
            trace = new StringBuilder();

            var fragments = map.Entities.Select(e => e.GenerateHarpFileFragment());
            var fileContents = string.Join(Environment.NewLine + Environment.NewLine + Environment.NewLine, fragments);

            if (!File.Exists(outputFilePath))
                return WriteResult.CouldNotFindFile;

            File.WriteAllText(outputFilePath, fileContents);

            return WriteResult.OK;
        }

        public enum WriteResult { UnknownError, OK, CouldNotFindFile, CouldNotWriteToFile }

    }
}
