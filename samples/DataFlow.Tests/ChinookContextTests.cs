using DataFlow.Infrastucture;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

namespace DataFlow.Tests
{
    public class ChinookContextTests
    {
        [Fact]
        public void load_the_contect_from_xml()
        {
            var context = new Mock<TestContext>();

            var test = new Mock<DbSet<Test>>();
            context.SetupGet(x => x.Test).Returns(test.Object);

            test.Setup(x => x.Add(It.IsAny<Test>()));

            var xml = @"<TestDataSet><Test><TestId>1</TestId><Name>Test</Name></Test></TestDataSet>";

            context.Object.PopulateContext(XmlReader.Create(new StringReader(xml)));

            test.Verify(x => x.Add(It.Is<Test>(t => t.TestId == 1 && t.Name == "Test")), Times.Once);
        }

        public class TestContext : DbContext
        {
            public virtual DbSet<Test> Test { get; set; }
        }

        public class Test
        {
            public int TestId { get; set; }

            public string Name { get; set; }
        }
    }
}
