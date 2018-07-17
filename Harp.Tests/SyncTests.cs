using Harp.Core.Models;
using Harp.Core.Services;
using Harp.Core.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Text;

namespace Harp.Tests
{
    [TestClass]
    public class SyncTests
    {
        [TestMethod]
        public void BasicSyncTest()
        {
            // Arrange

            StringBuilder trace;

            var sqlMock = new Mock<ISql>();
            sqlMock.Setup(m => m.GetAllTables())
                   .Returns(() => new List<(string fullName, int objectId)>
                   {
                       ("dbo.Dogs", 1),
                       ("dbo.Cats", 2)
                   });
            sqlMock.Setup(m => m.GetStoredProcsThatRefEntity(It.IsAny<string>()))
                   .Returns(() => new List<(string fullName, int objectId)>
                   {
                       ("dbo.GetDogsById", 3),
                       ("dbo.DogsSave", 4),
                       ("dbo.DogsUpdate", 5)
                   });
            sqlMock.Setup(m => m.GetColumnNames(It.IsAny<int>()))
                   .Returns(() => new string[]
                   {
                       "id",
                       "name",
                       "thing"
                   });
            sqlMock.Setup(m => m.GetTableObjectId(It.IsAny<string>()))
                   .Returns(() => new int?(1));

            var sync = new HarpSynchronizer(sqlMock.Object);

            var harpFile = new HarpFile();
            harpFile.Entities.Add(new Entity
            {
                EntityName = "Dogs",
                TableName = "",                 // blank
                Properties = new List<Property>
                {
                    new Property
                    {
                        Name = "ID",
                        ColumnName = ""         // blank
                    },
                    new Property
                    {
                        Name = "Name",
                        ColumnName = ""         // blank
                    }
                },
                Behaviors = new List<Behavior>
                {
                    new Behavior
                    {
                        Name = "Get by id",
                        StoredProcName = ""     // blank
                    }
                }

            });


            // Act
            var results = sync.Synchronize(harpFile, out trace);

            // Assert
            Assert.IsTrue(harpFile.IsFullyMapped);

        }
    }
}
