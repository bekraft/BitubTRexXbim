using System;
using System.Linq;
using System.Threading.Tasks;
using Bitub.Dto;
using Bitub.Dto.Scene;
using Bitub.Dto.Spatial;
using NUnit.Framework;

using Bitub.Xbim.Ifc.Transform;
using Bitub.Xbim.Ifc.Validate;
using Xbim.Ifc4.Interfaces;

namespace Bitub.Xbim.Ifc.Tests.Transform;

[TestFixture]
public class MapConversionAdvancedTransformTests : TRexTest<MapConversionTransform>
{
    private MapConversionTransform? _mapTransformFixture;

    [SetUp]
    public void Setup()
    {
        var crsPrefs = new MapConversionCrsPrefs(
            Name: "EPSG:25832",
            Description: "UTM in band 32",
            GeodeticDatum: "ETRS89",
            VerticalDatum: null,
            MapProjection: "UTM",
            "UTM32",
            MapUnitScale: null,
            OffsetAndHeight: new XYZ(0, 0, 113.5),
            MapRotation: new UV() { U = 0, V = 0 },
            Scale: null
        );
        var prefs = new MapConversionPrefs(
            UsePlacementOffsetAsTargetRef: true,
            ContextIdentifers: [],
            ContextTypes: ["Model".ToQualifier()]
        );

        _mapTransformFixture = new MapConversionTransform(crsPrefs, prefs, LoggerFactory,Enum.GetValues<TransformActionResult>());
        _mapTransformFixture.EditorCredentials = EditorCredentials;
    }
    
    [Test]
    public async Task RunMapConversionTransformOnPureModelWithOffset()
    {
        var result = await _mapTransformFixture.Run(
            ReadIfcModel("Ifc4-MapConversion-Pure.ifc"), 
            NewProgressMonitor(false));
        
        Assert.IsNotNull(result);
        Assert.That(result.ResultCode, Is.EqualTo(TransformResult.Code.Finished));

        var log = result.Log.ToList();
        Assert.That(log.Count, Is.EqualTo(83));
        Assert.That(log.Count(e => e.Performed == TransformActionResult.Added), Is.EqualTo(3));
        Assert.That(log.Count(e => e.Performed == TransformActionResult.Modified), Is.EqualTo(1));
        Assert.That(result.Target.Instances.OfType<IIfcProjectedCRS>().Count, Is.EqualTo(1));
        Assert.That(result.Target.Instances.OfType<IIfcMapConversion>().Count, Is.EqualTo(1));
        
        var mapConversion = result.Target.Instances.OfType<IIfcMapConversion>().Single();
        Assert.That(mapConversion.Eastings.Value, Is.EqualTo(458657.30).Within(0.25));
        Assert.That(mapConversion.Northings.Value, Is.EqualTo(5438232.25).Within(0.25));
        Assert.That(mapConversion.OrthogonalHeight.Value, Is.EqualTo(113.5).Within(1e-1));
        
        var validator = result.Target.ToSchemeValidator();
        Assert.IsTrue(validator.IsCompliantToSchema);
    }
    
    [Test]
    public async Task RunMapConversionTransformOnTransformedModelWithOffset()
    {
        var result = await _mapTransformFixture.Run(
            ReadIfcModel("Ifc4-MapConversion-Transformed.ifc"), 
            NewProgressMonitor(false));
        
        Assert.IsNotNull(result);
        Assert.That(result.ResultCode, Is.EqualTo(TransformResult.Code.Finished));

        var log = result.Log.ToList();
        Assert.That(log.Count, Is.EqualTo(44));
        Assert.That(log.Count(e => e.Performed == TransformActionResult.Added), Is.EqualTo(3));
        Assert.That(log.Count(e => e.Performed == TransformActionResult.Skipped), Is.EqualTo(3));
        Assert.That(log.Count(e => e.Performed == TransformActionResult.Modified), Is.EqualTo(1));
        Assert.That(result.Target.Instances.OfType<IIfcProjectedCRS>().Count, Is.EqualTo(1));
        Assert.That(result.Target.Instances.OfType<IIfcMapConversion>().Count, Is.EqualTo(1));
        
        var mapConversion = result.Target.Instances.OfType<IIfcMapConversion>().Single();
        Assert.That(mapConversion.Eastings.Value, Is.EqualTo(458657.30).Within(0.25));
        Assert.That(mapConversion.Northings.Value, Is.EqualTo(5438232.25).Within(0.25));
        Assert.That(mapConversion.OrthogonalHeight.Value, Is.EqualTo(113.5).Within(1e-1));
        
        var validator = result.Target.ToSchemeValidator();
        Assert.IsTrue(validator.IsCompliantToSchema);
    }
}