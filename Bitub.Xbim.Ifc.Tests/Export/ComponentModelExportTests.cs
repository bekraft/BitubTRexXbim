using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Google.Protobuf;

using Bitub.Dto;
using Bitub.Xbim.Ifc.Export;

using NUnit.Framework;

using Xbim.Common;

using Microsoft.Extensions.Logging;

namespace Bitub.Xbim.Ifc.Tests.Export
{
    [TestFixture]
    public class ComponentModelExportTests : TestBase<ComponentModelExportTests>
    {
        private readonly ExportPreferences testPreferences = new ExportPreferences
        {
            BodyExportType = SceneBodyExportType.FaceBody
        };

        private Task RunIfc4Export(string resourceName, ExportPreferences settings)
            => RunExport(resourceName, settings, ReadIfc4Model);

        private Task RunIfc2x3Export(string resourceName, ExportPreferences settings)
            => RunExport(resourceName, settings, ReadIfc2x3Model);

        private async Task RunExport(string resourceName, ExportPreferences settings, Func<String, IModel> reader)
        {
            Dto.Scene.ComponentScene result;
            using (var store = reader(resourceName))
            {
                var exporter = new ComponentModelExporter(new XbimTesselationContext(LoggerFactory), LoggerFactory);
                exporter.Preferences = settings;

                using (var monitor = new CancelableProgressing(true))
                {
                    result = await exporter.RunExport(store, monitor);
                }
            }

            Assert.IsNotNull(result, "Result exists");
            Assert.IsTrue(result.Components.Count > 0, "There are exported components");
            Assert.IsTrue(result.Components.SelectMany(c => c.Shapes).All(s => null != s.ShapeBody && null != s.Material), "All shapes have bodies and materials");
            Assert.IsTrue(result.ShapeBodies.All(r => r.Bodies.SelectMany(b => r.Bodies).All(b => b.FaceBody.Faces.Count > 0)), "All bodies have faces");
            // Show default values too
            var formatter = new JsonFormatter(JsonFormatter.Settings.Default.WithFormatDefaultValues(true));

            using (var jsonStream = File.CreateText($"{Path.GetFileNameWithoutExtension(resourceName)}.json"))
            {
                var json = formatter.Format(result);
                jsonStream.WriteLine(json);
                jsonStream.Close();
                logger.LogInformation($"JSON example has been written.");
            }

            using (var binStream = File.Create($"{Path.GetFileNameWithoutExtension(resourceName)}.scene"))
            {
                var binScene = result.ToByteArray();
                binStream.Write(binScene, 0, binScene.Length);
                logger.LogInformation($"Binary scene of {binScene.Length} bytes has been written.");
            }
        }

        [Test]
        public async Task BooleanResultCorrectionQuaternion()
        {
            await RunIfc2x3Export(
                @"Ifc2x3-Slab-BooleanResult.ifc",
                new ExportPreferences(testPreferences) 
                { 
                    Transforming = SceneTransformationStrategy.Quaternion 
                });
        }

        [Test]
        public async Task SampleHouseUserCorrectionQuaternion()
        {
            var filter = new Dto.Concept.CanonicalFilter(Dto.Concept.FilterMatchingType.SubOrEquiv, System.StringComparison.OrdinalIgnoreCase);
            filter.Filter.Add(new string[] { "Other", "Category" }.ToQualifier().ToClassifier());

            await RunIfc4Export(
                @"Ifc4-SampleHouse.ifc",
                new ExportPreferences(testPreferences)
                {
                    Transforming = SceneTransformationStrategy.Quaternion,
                    Positioning = ScenePositioningStrategy.UserCorrection,
                    UserModelCenter = new Dto.Spatial.XYZ(10, 0, 0)
                });
        }

        [Test]
        public async Task StoreyWithWallsNoCorrectionQuaternion()
        {
            await RunIfc4Export(
                @"Ifc4-Storey-With-4Walls.ifc",
                new ExportPreferences(testPreferences) 
                { 
                    Transforming = SceneTransformationStrategy.Quaternion 
                });
        }

        [Test]
        public async Task SlabIfcSiteRotatedMostExtendedRegionCorrectionQuaternion()
        {
            await RunIfc4Export(
                @"Ifc4-Rotated-IfcSite-1st-floor.ifc",
                new ExportPreferences(testPreferences)
                { 
                    Transforming = SceneTransformationStrategy.Quaternion, 
                    Positioning = ScenePositioningStrategy.MostExtendedRegionCorrection
                });
        }

        [Test]
        public async Task WallsMostExtendedRegionCorrectionQuaternion()
        {
            await RunIfc4Export(
                @"Ifc4-Base-Groundfloor.ifc",
                new ExportPreferences(testPreferences)
                {
                    Transforming = SceneTransformationStrategy.Quaternion,
                    Positioning = ScenePositioningStrategy.MostExtendedRegionCorrection
                });
        }

        [Test]
        public async Task SlabMeanTranslationCorrectionMatrix()
        {
            await RunIfc4Export(
                @"Ifc4-Rotated-1st-floor.ifc",
                new ExportPreferences(testPreferences)
                {
                    Transforming = SceneTransformationStrategy.Matrix,
                    Positioning = ScenePositioningStrategy.MeanTranslationCorrection
                });
        }

        [Test]
        public async Task MultiBodyHouseTranslationCorrectionQuaternion()
        {
            await RunIfc4Export(
                @"Ifc4-Multi-Body-House.ifc",
                new ExportPreferences(testPreferences)
                {
                    Transforming = SceneTransformationStrategy.Quaternion,
                    Positioning = ScenePositioningStrategy.MeanTranslationCorrection                    
                });
        }

        [Test]
        public async Task MappedRepresentationItem()
        {
            await RunIfc4Export(
                @"mapped-shape-with-transformation.ifc",
                new ExportPreferences(testPreferences)
                {
                    Transforming = SceneTransformationStrategy.Quaternion,
                    Positioning = ScenePositioningStrategy.NoCorrection
                });
        }
    }
}