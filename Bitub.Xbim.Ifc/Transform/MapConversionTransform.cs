using System;
using System.Collections.Generic;
using System.Linq;

using Bitub.Dto;
using Bitub.Dto.Scene;
using Bitub.Dto.Spatial;
using Bitub.Xbim.Ifc.TRex;
using Microsoft.Extensions.Logging;

using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace Bitub.Xbim.Ifc.Transform;

/// <summary>
/// Map Conversion CRS preferences.
/// </summary>
/// <param name="VerticalDatum">Name by which the vertical datum is identified.</param>
/// <param name="MapProjection">Name by which the map projection is identified.</param>
/// <param name="MapZone">Name by which the map zone, relating to the MapProjection, is identified.</param>
/// <param name="OffsetAndHeight">XYZ reference coordinates of the target map coordinate reference system.</param>
/// <param name="MapRotation">UV reference vector</param>
/// <param name="Scale">The scale between target and source unit</param>
/// <param name="MapUnitScale">The scale of the map unit as IFC SI prefix</param>
public record MapConversionCrsPrefs(
    string Name,
    string? Description,
    string GeodeticDatum,
    string? VerticalDatum,
    string? MapProjection,
    string? MapZone,
    XYZ OffsetAndHeight,
    UV MapRotation,
    Double? Scale,
    IfcSIPrefix? MapUnitScale)
{
    /// <summary>
    /// Merge non-null arguments into existing preferences and return a new record.
    /// </summary>
    /// <param name="name">A primary name</param>
    /// <param name="description">A description</param>
    /// <param name="verticalDatum">Name by which the vertical datum is identified.</param>
    /// <param name="geodeticDatum">A geodetic CRS identifier or code</param>
    /// <param name="mapProjection">Name by which the map projection is identified.</param>
    /// <param name="mapZone">Name by which the map zone, relating to the MapProjection, is identified.</param>
    /// <param name="offsetAndHeight">XYZ reference coordinates of the target map coordinate reference system.</param>
    /// <param name="mapRotation">UV reference vector</param>
    /// <param name="scale">The scale between target and source unit</param>
    /// <param name="mapUnitScale">The scale of the map unit as IFC SI prefix</param>
    /// <returns>A new record</returns>
    public MapConversionCrsPrefs MergeNonNullTo(
        string? name,
        string? description,
        string? geodeticDatum,
        string? verticalDatum,
        string? mapProjection,
        string? mapZone,
        XYZ? offsetAndHeight,
        UV? mapRotation,
        Double? scale,
        IfcSIPrefix? mapUnitScale)
    {
        return new MapConversionCrsPrefs(
            name ?? Name, 
            description ?? Description, 
            geodeticDatum ?? GeodeticDatum, 
            verticalDatum ?? VerticalDatum,  
            mapProjection ?? MapProjection, 
            mapZone ?? MapZone, 
            offsetAndHeight ?? OffsetAndHeight, 
            mapRotation ?? MapRotation, 
            scale ?? Scale, 
            mapUnitScale ?? MapUnitScale);
    }
}


/// <summary>
/// Conversion transform preferences.
/// </summary>
/// <param name="UsePlacementOffsetAsTargetRef">Use shift of local root placement as map conversion target reference.</param>
/// <param name="ContextIdentifers">Filter by given list of context identifiers</param>
/// <param name="ContextTypes">Filter by given list of context types</param>
public record MapConversionPrefs(
    bool UsePlacementOffsetAsTargetRef,
    Qualifier[] ContextIdentifers,
    Qualifier[] ContextTypes)
{
    /// <summary>
    /// Merge non-null arguments into existing preferences and return a new record.
    /// </summary>
    /// <param name="usePlacementOffsetAsTargetRef">Use shift of local root placement as map conversion target reference.</param>
    /// <param name="contextIdentifiers">Filter by given list of context identifiers</param>
    /// <param name="contextTypes">Filter by given list of context types</param>
    /// <returns>A new record</returns>
    public MapConversionPrefs MergeNonNullTo(
        bool? usePlacementOffsetAsTargetRef,
        Qualifier[]? contextIdentifiers,
        Qualifier[]? contextTypes)
    {
        return new MapConversionPrefs(
            usePlacementOffsetAsTargetRef ?? UsePlacementOffsetAsTargetRef,
            contextIdentifiers ?? ContextIdentifers,
            contextTypes ?? ContextTypes
        );
    }
}

/// <summary>
/// Map conversion transform package.
/// </summary>
public class MapConversionTransformPackage : TransformPackage
{
    #region Private Members
    private readonly List<XbimInstanceHandle> _representationContexts = new ();
    private readonly List<XbimInstanceHandle> _mapConversions = new ();
    private readonly List<XbimInstanceHandle> _projectedCRS = new ();
    private readonly Dictionary<XbimInstanceHandle, IPersistEntity> _referencedUnits = new ();
    private readonly HashSet<XbimInstanceHandle> _units = new ();
    #endregion

    internal readonly IfcBuilder Builder;

    internal MapConversionTransformPackage(IModel source,
        IModel target,
        IfcBuilder builder,
        CancelableProgressing? cancelableProgressing,
        params TransformActionResult[] logFilter)
        : base(source, target, cancelableProgressing, logFilter)
    {
        Builder = builder;
    }
    
    public IReadOnlyCollection<IIfcGeometricRepresentationContext> RepresentationContexts 
        => _representationContexts.Select(h => (IIfcGeometricRepresentationContext)h.Model.Instances[h.EntityLabel]).ToList();
    
    public IReadOnlyCollection<IIfcMapConversion> MapConversions 
        => _mapConversions.Select(h => (IIfcMapConversion)h.Model.Instances[h.EntityLabel]).ToList();
    
    public IReadOnlyCollection<IIfcProjectedCRS> ProjectedCRS 
        => _projectedCRS.Select(h => (IIfcProjectedCRS)h.Model.Instances[h.EntityLabel]).ToList();

    internal TransformActionType DispatchObject(IPersistEntity persistEntity)
    {
        switch (persistEntity)
        {
            case IIfcGeometricRepresentationContext context:
                var handleContext = new XbimInstanceHandle(context);
                if (!_representationContexts.Contains(handleContext))
                    _representationContexts.Add(handleContext);
                return TransformActionType.Copy;
            case IIfcMapConversion mapConversion:
                var handleConversion = new XbimInstanceHandle(mapConversion);
                if (!_mapConversions.Contains(handleConversion))
                    _mapConversions.Add(handleConversion);
                return TransformActionType.Drop;
            case IIfcProjectedCRS projectedCRS:
                var handleProjectedCRS = new XbimInstanceHandle(projectedCRS);
                if (!_projectedCRS.Contains(handleProjectedCRS))
                    _projectedCRS.Add(handleProjectedCRS);
                return TransformActionType.Drop;
            default:
                return TransformActionType.Copy;
        }
    }

    internal void TrackHostForNamedUnit(ExpressMetaProperty property, object hostObject)
    {
        if (hostObject is IPersistEntity persistEntity)
        {
            // Track all units
            if (persistEntity is IIfcUnit unit)
            {
                _units.Add(new XbimInstanceHandle(unit));
            }
            
            // Cross check assigned units
            if (property.PropertyInfo.HasLowerConstraintRelationType<IIfcUnit>())
            {
                if (property.TryGetValues<IIfcUnit>(persistEntity, out var units))
                {
                    units?.ForEach(u => _referencedUnits.Add(new XbimInstanceHandle(u), persistEntity));
                }
            }
        }
    }

    internal List<IIfcUnit> CleanUpUnusedNamedUnits()
    {
        var keptUnits = new List<IIfcUnit>();
        foreach (var crsUnit in _units)
        {
            var eUnit = (IIfcUnit)crsUnit.GetEntity();
            if (!_referencedUnits.TryGetValue(crsUnit, out var eHostEntity))
            {
                // Unit not referenced anywhere
                Target.Delete(eUnit);
                LogAction(crsUnit, TransformActionResult.Skipped);
            }
            else
            {
                // Otherwise keep unit entity
                keptUnits.Add(eUnit);
            }
        }

        return keptUnits;
    }
}

/// <summary>
/// Map Conversion Transform
/// </summary>
public class MapConversionTransform : ModelTransformTemplate<MapConversionTransformPackage>
{
    public sealed override ILogger? Log { get; protected set; }
    
    public override string Name => "Map Conversion Transform";

    public MapConversionCrsPrefs CrsPrefs { get; init; }
    
    public MapConversionPrefs Prefs { get; init; }
    
    /// <summary>
    /// Map Conversion Transform. Will drop existing IFCMAPCONVERSION and add new IFCMAPCONVERSION by given preferences.
    /// </summary>
    /// <param name="crsPrefs">CRS preferences</param>
    /// <param name="prefs">Preferences</param>
    /// <param name="loggerFactory">The logger factory</param>
    /// <param name="logFilter">The current log filter settings</param>
    public MapConversionTransform(MapConversionCrsPrefs crsPrefs, MapConversionPrefs prefs, 
        ILoggerFactory? loggerFactory, params TransformActionResult[] logFilter) : base(logFilter)
    {
        Log = loggerFactory?.CreateLogger(GetType());
        CrsPrefs = crsPrefs;
        Prefs = prefs;
        // Private
        _loggerFactory = loggerFactory;
    }

    protected override object? PropertyTransform(ExpressMetaProperty property, 
        object hostObject, MapConversionTransformPackage package)
    {
        package.TrackHostForNamedUnit(property, hostObject);
        return base.PropertyTransform(property, hostObject, package);
    }

    protected override TransformResult.Code DoPreprocessTransform(MapConversionTransformPackage package)
    {
        // Check if CRS prefs has been set
        if (null == CrsPrefs)
            throw new ArgumentNullException(nameof(CrsPrefs));
        return base.DoPreprocessTransform(package);
    }

    protected override TransformActionType PassInstance(IPersistEntity instance, MapConversionTransformPackage package)
    {
        // Use package dispatcher
        return package.DispatchObject(instance);
    }

    protected override TransformResult.Code DoPostTransform(MapConversionTransformPackage package)
    {
        var keptUnits = package.CleanUpUnusedNamedUnits();
        if (keptUnits.Any()) 
            Log?.LogWarning("Keeping references named units: {Units}", string.Join(",", keptUnits.Select(u => u.EntityLabel)));

        // Create target projected CRS
        var projectedCRS = CreateProjectedCRS(CrsPrefs, package);

        // Iterate source representation contexts, filter where identifiers and types have a match
        var sourceContexts = package.RepresentationContexts
            .Where(c1 =>
                (Prefs.ContextIdentifers.Length == 0 || Array.Exists(Prefs.ContextIdentifers, c2 => 
                    c1.ContextIdentifier.ToString().ToQualifier().IsEqualTo(c2, StringComparison.InvariantCultureIgnoreCase)))
                && (Prefs.ContextTypes.Length == 0 || Array.Exists(Prefs.ContextTypes, c2 => 
                        c1.ContextType.ToString().ToQualifier().IsEqualTo(c2, StringComparison.InvariantCultureIgnoreCase))));
        
        foreach (var sourceContext in sourceContexts)
        {
            if (package.Map.TryGetValue(new XbimInstanceHandle(sourceContext), out var targetContext))
            {
                var offsetAndHeight = CrsPrefs.OffsetAndHeight;
                var eCrs = (IIfcGeometricRepresentationContext)targetContext.GetEntity();

                // If local offset should be considered, adapt offset and reset local offset
                if (Prefs.UsePlacementOffsetAsTargetRef &&
                    CreateOffsetAndHeigthFromOffset(eCrs, package, out var localOffsetAndHeight))
                {
                    offsetAndHeight = offsetAndHeight.Add(localOffsetAndHeight);
                }

                CreateMapConversion(offsetAndHeight, 
                    CrsPrefs.MapRotation, CrsPrefs.Scale, eCrs,
                    projectedCRS, package);
            }
        }
        
        return base.DoPostTransform(package);
    }

    protected override MapConversionTransformPackage CreateTransformPackage(IModel aSource,
        IModel aTarget,
        CancelableProgressing? progressMonitor)
    {
        return new MapConversionTransformPackage(aSource, aTarget, IfcBuilder.WithModel(aTarget), progressMonitor, LogFilter.ToArray());
    }
    
    #region Private creator methods

    private readonly ILoggerFactory? _loggerFactory;

    // Create an offset and heigth XYZ & set current location to the local origin
    private bool CreateOffsetAndHeigthFromOffset(IIfcGeometricRepresentationContext context, 
        MapConversionTransformPackage package, out XYZ offsetAndHeight)
    {
        if (context.WorldCoordinateSystem is IIfcAxis2Placement3D axis2Placement3D)
        {
            offsetAndHeight = axis2Placement3D.Location.ToXYZ();
            axis2Placement3D.Location.SetXYZ(0, 0, 0);
            Log?.LogDebug("Resetting offset and height to {Location}", axis2Placement3D.Location);
            package.LogAction(new XbimInstanceHandle(context), TransformActionResult.Modified);
            return true;
        }
        offsetAndHeight = XYZ.Zero;
        return false;
    }
    
    // Creates a projected CRS
    private IIfcProjectedCRS CreateProjectedCRS(MapConversionCrsPrefs prefs, MapConversionTransformPackage package)
    {
        var entity = package.Builder.IfcEntityScope.NewOf<IIfcProjectedCRS>();
        entity.Name = prefs.Name;
        entity.Description = prefs.Description;
        entity.GeodeticDatum = prefs.GeodeticDatum;
        entity.MapZone = prefs.MapZone;
        entity.MapProjection = prefs.MapProjection;
        entity.VerticalDatum = prefs.VerticalDatum;
        entity.MapUnit = package.Builder.NewSIUnit(IfcUnitEnum.LENGTHUNIT, IfcSIUnitName.METRE, prefs.MapUnitScale);
        
        Log?.LogDebug("Creating new projected CRS {Entity}", entity.ToString());
        package.LogAction(new XbimInstanceHandle(entity), TransformActionResult.Added);
        package.LogAction(new XbimInstanceHandle(entity.MapUnit), TransformActionResult.Added);

        return entity;
    }

    // Creates a map conversion from given source and target by given offset
    private IIfcMapConversion CreateMapConversion(XYZ offsetAndHeigth, UV axisRotation, Double? scale, 
        IIfcCoordinateReferenceSystemSelect source, IIfcCoordinateReferenceSystem target, MapConversionTransformPackage package)
    {
        var entity = package.Builder.IfcEntityScope.NewOf<IIfcMapConversion>();
        entity.SourceCRS = source;
        entity.TargetCRS = target;
        entity.XAxisAbscissa = axisRotation.U;
        entity.XAxisOrdinate = axisRotation.V;
        entity.Eastings = offsetAndHeigth.X;
        entity.Northings = offsetAndHeigth.Y;
        entity.OrthogonalHeight = offsetAndHeigth.Z;
        entity.Scale = scale;
        
        package.LogAction(new XbimInstanceHandle(entity), TransformActionResult.Added);
        Log?.LogDebug("Creating new map conversion {Entity}", entity.ToString());
        
        return entity;
    }
    
    #endregion
}