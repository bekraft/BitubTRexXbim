using System.Linq;
using System.Threading.Tasks;

using Xbim.Ifc4.Interfaces;
using Xbim.IO;

using Bitub.Xbim.Ifc.Transform;
using Bitub.Xbim.Ifc.Validate;

using Bitub.Dto.Spatial;

using NUnit.Framework;

namespace Bitub.Xbim.Ifc.Tests.Transform;

[TestFixture]
public class ModelPlacementTransformTests : TRexWithGeometryServicesTest<ModelPlacementTransformTests>
{
    [Test]
    public void AxisAlignmentSerializationTest()
    {
        var axis1 = new IfcAxisAlignment()
        {
            SourceReferenceAxis = new IfcAlignReferenceAxis(new XYZ(), new XYZ { X = 1 }),
            TargetReferenceAxis = new IfcAlignReferenceAxis(new XYZ(), new XYZ { Y = 1 })
        };

        axis1.SaveToFile("TestAxisAlignment.xml");

        var axis2 = IfcAxisAlignment.LoadFromFile("TestAxisAlignment.xml");

        Assert.IsNotNull(axis2);
        Assert.IsNotNull(axis2.SourceReferenceAxis);
        Assert.IsTrue(axis2.SourceReferenceAxis.Offset.IsAlmostEqualTo(axis1.SourceReferenceAxis.Offset, Precision));
        Assert.IsTrue(axis2.SourceReferenceAxis.Target.IsAlmostEqualTo(axis1.SourceReferenceAxis.Target, Precision));
        Assert.IsNotNull(axis2.TargetReferenceAxis);
        Assert.IsTrue(axis2.TargetReferenceAxis.Offset.IsAlmostEqualTo(axis1.TargetReferenceAxis.Offset, Precision));
        Assert.IsTrue(axis2.TargetReferenceAxis.Target.IsAlmostEqualTo(axis1.TargetReferenceAxis.Target, Precision));
    }

    [Test]
    public async Task OffsetShiftAndRotateTest1()
    {
        using (var source = ReadIfcModel("Ifc4-RotatedStorey-Slab.ifc"))
        {
            var stampBefore = source.ToSchemeValidator();

            var testConfig = IfcAxisAlignment.Load(ReadEmbeddedFileStream("AlignmentAxis1.xml"));
            Assert.IsNotNull(testConfig);
            Assert.IsNotNull(testConfig.SourceReferenceAxis);
            Assert.IsNotNull(testConfig.TargetReferenceAxis);

            var request = new ModelPlacementTransform(LoggerFactory)                
            {
                AxisAlignment = testConfig,
                PlacementStrategy = ModelPlacementStrategy.ChangeRootPlacements,
                // Common config
                TargetStoreType = XbimStoreType.InMemoryModel,
                EditorCredentials = EditorCredentials
            };

            var result = await request.Run(source, NewProgressMonitor(true));
            
            Assert.IsNotNull(result);
            Assert.That(result.ResultCode, Is.EqualTo(TransformResult.Code.Finished));
            var validator = result.Target.ToSchemeValidator();
            Assert.IsTrue(validator.IsCompliantToSchema);
        }
    }

    [Test]
    public async Task OffsetShiftAndRotateTest2_Change()
    {
        using (var source = ReadIfcModel("Ifc4-SampleHouse.ifc"))
        {
            var stampBefore = source.ToSchemeValidator();

            var testConfig = IfcAxisAlignment.Load(ReadEmbeddedFileStream("AlignmentAxis2.xml"));
            Assert.IsNotNull(testConfig);
            Assert.IsNotNull(testConfig.SourceReferenceAxis);
            Assert.IsNotNull(testConfig.TargetReferenceAxis);

            var request = new ModelPlacementTransform(LoggerFactory)
            {
                AxisAlignment = testConfig,
                PlacementStrategy = ModelPlacementStrategy.ChangeRootPlacements,
                // Common config
                TargetStoreType = XbimStoreType.InMemoryModel,
                EditorCredentials = EditorCredentials
            };
            
            var result = await request.Run(source, NewProgressMonitor(true));
            
            Assert.IsNotNull(result);
            Assert.That(result.ResultCode, Is.EqualTo(TransformResult.Code.Finished));
            
            var rootPlacement = result.Target.Instances.OfType<IIfcLocalPlacement>().FirstOrDefault(i => i.PlacementRelTo == null);
            Assert.IsNotNull(rootPlacement?.PlacesObject);
            Assert.That(rootPlacement.PlacesObject.Any(), Is.True, "Root has objects");
            
            var validator = result.Target.ToSchemeValidator();
            Assert.IsTrue(validator.IsCompliantToSchema);
        }
    }

    [Test]
    public async Task OffsetShiftAndRotateTest2_New()
    {
        using (var source = ReadIfcModel("Ifc4-SampleHouse.ifc"))
        {
            var stampBefore = source.ToSchemeValidator();

            var testConfig = IfcAxisAlignment.Load(ReadEmbeddedFileStream("AlignmentAxis2.xml"));
            Assert.IsNotNull(testConfig);
            Assert.IsNotNull(testConfig.SourceReferenceAxis);
            Assert.IsNotNull(testConfig.TargetReferenceAxis);

            var request = new ModelPlacementTransform(LoggerFactory)
            {
                AxisAlignment = testConfig,
                PlacementStrategy = ModelPlacementStrategy.NewRootPlacement,
                // Common config
                TargetStoreType = XbimStoreType.InMemoryModel,
                EditorCredentials = EditorCredentials
            };

            var result = await request.Run(source, NewProgressMonitor(true));
            
            Assert.IsNotNull(result);
            Assert.That(result.ResultCode, Is.EqualTo(TransformResult.Code.Finished));
            
            var rootPlacement = result.Target.Instances.OfType<IIfcLocalPlacement>().FirstOrDefault(i => i.PlacementRelTo == null);
            Assert.IsNotNull(rootPlacement?.PlacesObject);
            Assert.That(rootPlacement.PlacesObject.Any(), Is.False, "Root has objects");
            
            var validator = result.Target.ToSchemeValidator();
            Assert.IsTrue(validator.IsCompliantToSchema);
        }
    }
}
