using System.Linq;

using Xbim.Common.Step21;
using Xbim.Common.Geometry;
using Xbim.Ifc4.Interfaces;

using NUnit.Framework;

namespace Bitub.Xbim.Ifc.Tests
{
    [TestFixture]
    public class IfcModelBuildingTests : TestBase<IfcModelBuildingTests>
    {
        [Test]
        public void BuildingIFC2x3()
        {
            var builder = IfcBuilder.WithNewProject("Test", EditorCredentials, XbimSchemaVersion.Ifc2X3, LoggerFactory);
            Assert.IsNotNull(builder.NewSite("Some building"));
            Assert.IsNotNull(builder.NewBuilding("Some building"));
            Assert.IsNotNull(builder.NewStorey("Some building"));

            var globalPlacement = builder.NewLocalPlacement(new XbimVector3D());
            Assert.IsNotNull(builder.NewProduct<IIfcWallStandardCase>(globalPlacement));

            Assert.AreEqual(4, builder.model.Instances.OfType<IIfcProduct>().Count());
            Assert.AreEqual(1, builder.model.Instances.OfType<IIfcLocalPlacement>().Count());
            Assert.AreEqual(1, builder.model.Instances.OfType<IIfcRelContainedInSpatialStructure>().Count());
            Assert.AreEqual(3, builder.model.Instances.OfType<IIfcRelAggregates>().Count());
        }

        [Test]
        public void BuildingIFC4()
        {
            var builder = IfcBuilder.WithNewProject("Test", EditorCredentials, XbimSchemaVersion.Ifc4, LoggerFactory);
            Assert.IsNotNull(builder.NewSite("Some building"));
            Assert.IsNotNull(builder.NewBuilding("Some building"));
            Assert.IsNotNull(builder.NewStorey("Some building"));

            var globalPlacement = builder.NewLocalPlacement(new XbimVector3D());
            Assert.IsNotNull(builder.NewProduct<IIfcWallStandardCase>(globalPlacement));

            Assert.AreEqual(4, builder.model.Instances.OfType<IIfcProduct>().Count());
            Assert.AreEqual(1, builder.model.Instances.OfType<IIfcLocalPlacement>().Count());
            Assert.AreEqual(1, builder.model.Instances.OfType<IIfcRelContainedInSpatialStructure>().Count());
            Assert.AreEqual(3, builder.model.Instances.OfType<IIfcRelAggregates>().Count());
        }

        [Test]
        public void BuildingIFC4_Relations()
        {
            var builder = IfcBuilder.WithNewProject("Test", EditorCredentials, XbimSchemaVersion.Ifc2X3, LoggerFactory);
            Assert.IsNotNull(builder.NewSite("Some building"));
            Assert.IsNotNull(builder.NewBuilding("Some building"));
            Assert.IsNotNull(builder.NewStorey("Some building"));

            var globalPlacement = builder.NewLocalPlacement(new XbimVector3D());
            var fixture1 = builder.NewProduct<IIfcWallStandardCase>(globalPlacement);
            Assert.IsNotNull(fixture1);

            Assert.IsTrue(fixture1.ContainedInStructure.Count() == 1);

            builder.Transactively(m =>
            {
                var fixture2 = builder.ifcEntityScope.New<IIfcWallStandardCase>(fixture1.GetType(), e =>
                {
                    e.ObjectPlacement = globalPlacement;
                });

                Assert.IsNull(fixture2.IsContainedIn);
                Assert.IsTrue(fixture2.ContainedInStructure.Count() == 0);

                fixture2 = fixture2.CreateSameRelationshipsLike(fixture1);
                Assert.IsNotNull(fixture2);

                Assert.IsTrue(fixture2.ContainedInStructure.Count() == 1);
                Assert.AreEqual(fixture1.ContainedInStructure.First().RelatingStructure, fixture2.ContainedInStructure.First().RelatingStructure);
            });            
        }
    }
}
