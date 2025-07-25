using System.Linq;
using System.Threading.Tasks;

using Xbim.Ifc4.Interfaces;
using Xbim.IO;

using Bitub.Xbim.Ifc.Transform;
using Bitub.Xbim.Ifc.Validate;

using Bitub.Dto;
using Bitub.Dto.Spatial;

using NUnit.Framework;

using Microsoft.Extensions.Logging;

namespace Bitub.Xbim.Ifc.Tests.Transform;


[TestFixture]
public class ModelPlacementTransformServicesTests : TRexWithGeometryServicesTest<ModelPlacementTransformServicesTests>
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

            var request = new ModelPlacementTransformServices(LoggerFactory)                
            {
                AxisAlignment = testConfig,
                PlacementStrategy = ModelPlacementStrategy.ChangeRootPlacements,
                // Common config
                TargetStoreType = XbimStoreType.InMemoryModel,
                EditorCredentials = EditorCredentials
            };

            using (var cp = new CancelableProgressing(true))
            {
                cp.OnProgressChange += (s, o) => Logger.LogDebug($"State {o.State}: Percentage = {o.Percentage}; State object = {o.StateObject}");

                using (var result = await request.Run(source, cp))
                {
                    if (null != result.Cause)
                        Logger?.LogError("Exception: {0}, {1}, {2}", result.Cause, result.Cause.Message, result.Cause.StackTrace);

                    //Assert.AreEqual(TransformResult.Code.Finished, result.ResultCode);
                    // TODO Specific tests

                    var stampAfter = result.Target.ToSchemeValidator();
                    //Assert.AreEqual(stampBefore, stampAfter);
                }
            }
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

            var request = new ModelPlacementTransformServices(LoggerFactory)
            {
                AxisAlignment = testConfig,
                PlacementStrategy = ModelPlacementStrategy.ChangeRootPlacements,
                // Common config
                TargetStoreType = XbimStoreType.InMemoryModel,
                EditorCredentials = EditorCredentials
            };

            using (var cp = new CancelableProgressing(true))
            {
                cp.OnProgressChange += (s, o) => Logger.LogDebug($"State {o.State}: Percentage = {o.Percentage}; State object = {o.StateObject}");

                using (var result = await request.Run(source, cp))
                {
                    if (null != result.Cause)
                        Logger?.LogError("Exception: {0}, {1}, {2}", result.Cause, result.Cause.Message, result.Cause.StackTrace);

                    var rootPlacement = result.Target.Instances.OfType<IIfcLocalPlacement>().Where(i => i.PlacementRelTo == null).FirstOrDefault();
                    Assert.IsNotNull(rootPlacement.PlacesObject);
                    Assert.IsTrue(rootPlacement.PlacesObject.Any(), "Root has objects");

                    //Assert.AreEqual(TransformResult.Code.Finished, result.ResultCode);
                    // TODO Specific tests

                    var stampAfter = result.Target.ToSchemeValidator();
                    //Assert.AreEqual(stampBefore, stampAfter);
                }
            }
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

            var request = new ModelPlacementTransformServices(LoggerFactory)
            {
                AxisAlignment = testConfig,
                PlacementStrategy = ModelPlacementStrategy.NewRootPlacement,
                // Common config
                TargetStoreType = XbimStoreType.InMemoryModel,
                EditorCredentials = EditorCredentials
            };

            using (var cp = new CancelableProgressing(true))
            {
                cp.OnProgressChange += (s, o) => Logger.LogDebug($"State {o.State}: Percentage = {o.Percentage}; State object = {o.StateObject}");

                using (var result = await request.Run(source, cp))
                {
                    if (null != result.Cause)
                        Logger?.LogError("Exception: {0}, {1}, {2}", result.Cause, result.Cause.Message, result.Cause.StackTrace);

                    var rootPlacement = result.Target.Instances.OfType<IIfcLocalPlacement>().Where(i => i.PlacementRelTo == null).FirstOrDefault();
                    Assert.IsNotNull(rootPlacement.PlacesObject);
                    Assert.IsFalse(rootPlacement.PlacesObject.Any(), "Root has no objects");

                    //Assert.AreEqual(TransformResult.Code.Finished, result.ResultCode);
                    // TODO Specific tests

                    var stampAfter = result.Target.ToSchemeValidator();
                    //Assert.AreEqual(stampBefore, stampAfter);
                }
            }
        }
    }

    public void Report(ProgressStateToken value)
    {
        Logger.LogDebug($"State {value.State}: Percentage = {value.Percentage}; State object = {value.StateObject}");
    }
}
