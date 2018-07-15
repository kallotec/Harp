using System.Collections.Generic;

namespace Harp.Core.Utilities
{
    public interface ISql
    {
        List<(string fullName, int objectId)> GetAllTables();
        int? GetTableObjectId(string fullTableName);
        string[] GetColumnNames(int tableObjectId);
        List<(string fullName, int objectId)> GetStoredProcsThatRefEntity(string tableName);
    }
}