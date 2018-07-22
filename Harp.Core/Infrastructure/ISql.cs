using System.Collections.Generic;

namespace Harp.Core.Infrastructure
{
    public interface ISql
    {
        bool ConfigureAndTest(string connectionString);
        List<(string fullName, int objectId)> GetAllTables();
        int? GetTableObjectId(string fullTableName);
        string[] GetColumnNames(int tableObjectId);
        List<(string fullName, int objectId)> GetStoredProcsThatRefEntity(string tableName);
    }
}