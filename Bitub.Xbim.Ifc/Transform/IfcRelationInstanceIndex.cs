using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common;

namespace Bitub.Xbim.Ifc.Transform;

/// <summary>
/// An Ifc relation visitor used by walking hierarchies
/// </summary>
public interface IIfcRelationVisitor
{
    /// <summary>
    /// Visit an entity at given depth
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="level"></param>
    /// <param name="relationInfo"></param>
    /// <returns></returns>
    bool Visit(IPersistEntity entity, int level, IIfcRelationInfo relationInfo);
}

/// <summary>
/// An Ifc Relation hierarchical index.
/// </summary>
public class IfcRelationInstanceIndex
{
    /// <summary>
    /// Parent - Instance Level record
    /// </summary>
    /// <param name="Parent">A parent</param>
    /// <param name="Level">A level (starting with 0)</param>
    struct NodeAtLevel(XbimInstanceHandle Parent, XbimInstanceHandle[] Children, int Level = 0)
    {
        public XbimInstanceHandle Parent { get; } =  Parent;
        public int Level { get; set; } = Level;
        public XbimInstanceHandle[] Children { get; set; } = Children;
    }

    #region Private Members

    // Relation Info - Instance Handle - ParentInstanceAtLevel
    private readonly Dictionary<IIfcRelationInfo, Dictionary<XbimInstanceHandle, NodeAtLevel>> _relationCache;
    
    #endregion

    /// <summary>
    /// New relation instance cache, register the given relations for tracking.
    /// </summary>
    /// <param name="ifcRelationInfo">The relation info to track</param>
    public IfcRelationInstanceIndex(params IIfcRelationInfo[] ifcRelationInfo)
    {
        _relationCache = new ();
        // Prepare internal cache
        foreach (var relationInfo in ifcRelationInfo)
        {
            _relationCache[relationInfo] = new Dictionary<XbimInstanceHandle, NodeAtLevel>();
        }
    }

    // Propagate level change 
    private void PropagateLevelChange(Dictionary<XbimInstanceHandle, NodeAtLevel> cache, 
        XbimInstanceHandle instance, int level)
    {
        var queue = new Queue<Tuple<XbimInstanceHandle, int>>();
        queue.Enqueue(new Tuple<XbimInstanceHandle, int>(instance, level));
        
        while (queue.Count > 0)
        {
            var t = queue.Dequeue();
            if (cache.TryGetValue(t.Item1, out NodeAtLevel nodeAtLevel))
            {
                nodeAtLevel.Level = t.Item2;
                cache[t.Item1] = nodeAtLevel;    
            }
            
            foreach(var childHandle in nodeAtLevel.Children)
                queue.Enqueue(new Tuple<XbimInstanceHandle, int>(childHandle, t.Item2 + 1));
        }
    }
    
    /// <summary>
    /// Puts an entity to hierarchy cache, if any registered relation matches.
    /// </summary>
    /// <param name="entity">The entity host</param>
    /// <returns>True, if there's a match</returns>
    /// <exception cref="NotSupportedException">Thrown, if there are more than 1 parents held by relation</exception>
    public bool PutCache(IPersistEntity entity)
    {
        bool hasRelationType = false;
        var instanceHandle = new XbimInstanceHandle(entity);
        
        foreach (var relationInfo in _relationCache.Keys)
        {
            var cache = _relationCache[relationInfo];
            var parents = relationInfo
                .GetParentOf(entity)
                .ToArray();
            
            if (parents.Length > 1)
            {
                throw new NotSupportedException($"Only single rooted relations are allowed. Got: {parents.Length}");
            }
            else if (parents.Length == 1)
            {
                var parentHandle = new XbimInstanceHandle(parents.First());
                var childHandles = relationInfo
                    .GetTargetOf(entity)
                    .Select(e => new XbimInstanceHandle(e))
                    .ToArray();

                if (cache.TryGetValue(parentHandle, out NodeAtLevel parentAtLevel))
                {
                    // Parent already exist
                    var level = parentAtLevel.Level + 1;
                    cache[instanceHandle] = new NodeAtLevel(parentHandle, childHandles, level);
                    PropagateLevelChange(cache, instanceHandle, level);
                }
                else
                {
                    // Parent unknown, start at default level 0
                    cache[instanceHandle] = new NodeAtLevel(parentHandle, childHandles);
                    PropagateLevelChange(cache, instanceHandle, 0);
                }
                hasRelationType = true;
            }
        }
        return hasRelationType;
    }
}