using System.Linq;

using Xbim.Common.Step21;
using Xbim.Common.Geometry;
using Xbim.Ifc4.Interfaces;

using NUnit.Framework;

namespace Bitub.Xbim.Ifc.Tests
{
    [TestFixture]
    public class IfcBuilderTests : TRexTest<IfcBuilderTests>
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

            Assert.AreEqual(4, builder.Model.Instances.OfType<IIfcProduct>().Count());
            Assert.AreEqual(1, builder.Model.Instances.OfType<IIfcLocalPlacement>().Count());
            Assert.AreEqual(1, builder.Model.Instances.OfType<IIfcRelContainedInSpatialStructure>().Count());
            Assert.AreEqual(3, builder.Model.Instances.OfType<IIfcRelAggregates>().Count());
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

            Assert.AreEqual(4, builder.Model.Instances.OfType<IIfcProduct>().Count());
            Assert.AreEqual(1, builder.Model.Instances.OfType<IIfcLocalPlacement>().Count());
            Assert.AreEqual(1, builder.Model.Instances.OfType<IIfcRelContainedInSpatialStructure>().Count());
            Assert.AreEqual(3, builder.Model.Instances.OfType<IIfcRelAggregates>().Count());
        }

        [Test]
        public void BuildingIFC4_Relations()
        {
            var builder = IfcBuilder.WithNewProject("Test", EditorCredentials, XbimSchemaVersion.Ifc2X3, LoggerFactory);
            Assert.IsNotNull(builder.NewSite("A site"));
            var ifcBuilding = builder.NewBuilding("A building");
            Assert.IsNotNull(ifcBuilding);
            var ifcBuildingStorey = builder.NewStorey("A storey");
            Assert.IsNotNull(ifcBuildingStorey);

            var globalPlacement = builder.NewLocalPlacement(new XbimVector3D());
            var fixture1 = builder.NewProduct<IIfcWallStandardCase>(globalPlacement);
            
            Assert.IsNotNull(fixture1);

            var aggRelation = ifcBuilding.IsDecomposedBy.ToArray();
            Assert.That(aggRelation.Count(), Is.EqualTo(1));
            
            var superSet = ifcBuildingStorey.ContainsElements
                .Select(x => x.RelatingStructure)
                .ToArray();
            Assert.That(superSet.Length, Is.EqualTo(1));
            Assert.That(superSet[0], Is.EqualTo(ifcBuildingStorey));
            
            var subSet = ifcBuildingStorey.ContainsElements
                .SelectMany(x => x.RelatedElements)
                .ToArray();
            Assert.That(subSet.Length, Is.EqualTo(1));
            Assert.That(subSet[0], Is.EqualTo(fixture1));

            Assert.That(fixture1.ContainedInStructure.Count(), Is.EqualTo(1));

            builder.Transactive(m =>
            {
                var fixture2 = builder.IfcEntityScope.New<IIfcWallStandardCase>(fixture1.GetType(), e =>
                {
                    e.ObjectPlacement = globalPlacement;
                });

                Assert.IsNull(fixture2.IsContainedIn);
                Assert.IsTrue(!fixture2.ContainedInStructure.Any());

                fixture2 = fixture2.CreateSameRelationshipsLike(fixture1);
                Assert.IsNotNull(fixture2);

                Assert.IsTrue(fixture2.ContainedInStructure.Count() == 1);
                Assert.AreEqual(fixture1.ContainedInStructure.First().RelatingStructure, fixture2.ContainedInStructure.First().RelatingStructure);
            });            
        }
    }
}
