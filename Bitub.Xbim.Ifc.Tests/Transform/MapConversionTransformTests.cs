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
public class MapConversionTransformTests : TRexTest<MapConversionTransform>
{
    private MapConversionTransform _mapTransformFixture;

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
            MapUnitName: IfcSIUnitName.METRE,
            OffsetAndHeight: XYZ.Zero,
            MapRotation: new UV() { U = 0, V = 0 },
            Scale: null
        );
        var prefs = new MapConversionPrefs(
            UsePlacementOffsetAsTargetRef: false,
            RepresentationContext: new[] { "Model".ToQualifier() }
        );

        _mapTransformFixture = new MapConversionTransform(crsPrefs, prefs, LoggerFactory,Enum.GetValues<TransformActionResult>());
    }
    
    [Test]
    public async Task RunMapConversionTransformOnPureModel()
    {
        var result = await _mapTransformFixture.Run(
            ReadIfcModel("Ifc4-MapConversion-Pure.ifc"), 
            NewProgressMonitor(false));
        
        Assert.IsNotNull(result);
        Assert.That(result.ResultCode, Is.EqualTo(TransformResult.Code.Finished));

        var log = result.Log.ToList();
        Assert.That(log.Count, Is.EqualTo(39));
        Assert.That(log.Count(e => e.Performed == TransformActionResult.Added), Is.EqualTo(3));
        Assert.That(result.Target.Instances.OfType<IIfcProjectedCRS>().Count, Is.EqualTo(1));
        Assert.That(result.Target.Instances.OfType<IIfcMapConversion>().Count, Is.EqualTo(1));
        
        var validator = result.Target.ToSchemeValidator();
        Assert.IsTrue(validator.IsCompliantToSchema);
    }
    
    [Test]
    public async Task RunMapConversionTransformOnTransformedModel()
    {
        var result = await _mapTransformFixture.Run(
            ReadIfcModel("Ifc4-MapConversion-Transformed.ifc"), 
            NewProgressMonitor(false));
        
        Assert.IsNotNull(result);
        Assert.That(result.ResultCode, Is.EqualTo(TransformResult.Code.Finished));

        var log = result.Log.ToList();
        Assert.That(log.Count, Is.EqualTo(43));
        Assert.That(log.Count(e => e.Performed == TransformActionResult.Added), Is.EqualTo(3));
        Assert.That(log.Count(e => e.Performed == TransformActionResult.Skipped), Is.EqualTo(3));
        Assert.That(result.Target.Instances.OfType<IIfcProjectedCRS>().Count, Is.EqualTo(1));
        Assert.That(result.Target.Instances.OfType<IIfcMapConversion>().Count, Is.EqualTo(1));
        
        var validator = result.Target.ToSchemeValidator();
        Assert.IsTrue(validator.IsCompliantToSchema);
    }
}