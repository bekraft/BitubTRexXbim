using System;
using System.Collections.Generic;
using Bitub.Dto;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace Bitub.Xbim.Ifc.Transform;
    
/// <summary>
/// Relational reducer token.
/// </summary>
sealed class IfcTopologyPatchToken
{
    /// <summary>
    /// The current object as owner of an indexed relation type held by spec.
    /// </summary>
    internal IIfcObject End { get; set; } 
    /// <summary>
    /// The current aggregator.
    /// </summary>
    internal IIfcObject Start { get; set; }
    /// <summary>
    /// The current transitive depth measured from <see cref="Start"/> towards <see cref="End"/>.
    /// </summary>
    internal int DepthInRow { get; set; }
}

/// <summary>
/// Topology aggregation transform package.
/// </summary>
public sealed class TopologyAggregationTransformPackage : TransformPackage
{
    private Dictionary<TopologyAggregationPrefs, List<IfcTopologyPatchToken>> TokenState { get; }
    private Dictionary<IIfcObject, List<IfcTopologyPatchToken>> PatchEnds { get; }
    

    internal TopologyAggregationTransformPackage(IModel source, IModel target,
        CancelableProgressing cancelableProgressing)
        : base(source, target, cancelableProgressing)
    {
        TokenState = new Dictionary<TopologyAggregationPrefs, List<IfcTopologyPatchToken>>();
    }


    public bool IsWatched(IIfcObject o, IIfcRelationship r)
    {
        throw new NotImplementedException();
    }

    public void SetEntityTypeWithDepth<T>(T entityType, int depthLimit) where T : IIfcSpatialStructureElement
    {
        throw new NotImplementedException();
    }

    
}