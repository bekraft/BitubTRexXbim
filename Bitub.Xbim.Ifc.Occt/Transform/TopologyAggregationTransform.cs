using System;
using Bitub.Dto;
using Microsoft.Extensions.Logging;
using Xbim.Common;

namespace Bitub.Xbim.Ifc.Transform;

/// <summary>
/// Topology aggregation preferences.
/// </summary>
public record TopologyAggregationPrefs(
    Type RelationType,
    Type ObjectType,
    int MaxDepthInRow,
    string AggregationNamePattern);

public class TopologyAggregationTransform : ModelTransformTemplate<TopologyAggregationTransformPackage>
{
    public sealed override ILogger Log { get; protected set; }
    
    public override string Name => "Topology Aggregation Transform";
    
    public TopologyAggregationPrefs TopologyAggregationPrefs { get; set; }

    public TopologyAggregationTransform(ILoggerFactory loggerFactory, params TransformActionResult[] filter) : base(filter)
    {
        Log = loggerFactory.CreateLogger<TopologyAggregationTransform>();
    }
    
    protected override TransformActionType PassInstance(IPersistEntity instance, TopologyAggregationTransformPackage package)
    {
        throw new System.NotImplementedException();
    }

    protected override TopologyAggregationTransformPackage CreateTransformPackage(IModel aSource, IModel aTarget,
        CancelableProgressing progressMonitor)
    {
        return new TopologyAggregationTransformPackage(aSource, aTarget, progressMonitor);
    }
    
}