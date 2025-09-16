using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace Bitub.Xbim.Ifc.Transform;

/// <summary>
/// Generic Ifc Relation Info interface which aggregates type of relation, target type and handling on host instances.
/// </summary>
public interface IIfcRelationInfo
{
    Type IfcRelationType { get; }
    Type ParentType { get; }
    Type TargetType { get; }
    string Name { get; }
    string HostPropertyName { get; }
    string TargetPropertyName { get; }
    IEnumerable<PropertyInfo> GetRelationInfoOf(IPersistEntity entity);
    IEnumerable<IIfcRelationship> GetRelationOf(IPersistEntity entity);
    IEnumerable<IPersistEntity> GetTargetOf(IPersistEntity entity);
    IEnumerable<IPersistEntity> GetParentOf(IPersistEntity entity);
}

/// <summary>
/// Concrete Ifc Relation Info implementation
/// </summary>
/// <param name="Name">The semantic name of relation info</param>
/// <param name="HostPropertyName">The host's property name holding the reference to relation</param>
/// <param name="TargetPropertyName">The relations target property name pointing towards the intended direction</param>
/// <typeparam name="TRelation">The relation type</typeparam>
/// <typeparam name="PValue">The parent value type</typeparam>
/// <typeparam name="TValue">The target value type</typeparam>
public record IfcRelationInfo<TRelation, PValue, TValue>(
    string Name, 
    string HostPropertyName,
    string ParentPropertyName,
    string TargetPropertyName) : IIfcRelationInfo where TRelation : IIfcRelationship 
{
    public Type IfcRelationType { get; } = typeof(TRelation);
    public Type ParentType { get; } = typeof(PValue);
    public Type TargetType { get; } = typeof(TValue);

    /// <summary>
    /// Returns the relation property info instances of given host entity.
    /// </summary>
    /// <param name="entity">The host entity</param>
    /// <returns>An enumerable of relation property infos</returns>
    public IEnumerable<PropertyInfo> GetRelationInfoOf(IPersistEntity entity)
    {
        return Enumerable.Concat(
            // 1-ary relations
            entity.GetType()
                .GetLowerConstraintGenericProperty(IfcRelationType, null, HostPropertyName),
            // n-ary relations
            entity.GetType()
                .GetLowerConstraintGenericProperty(typeof(IItemSet), IfcRelationType, HostPropertyName));
    }

    /// <summary>
    /// Returns the relation objects
    /// </summary>
    /// <param name="entity">The host entity</param>
    /// <returns>An enumerable of TRelation</returns>
    public IEnumerable<TRelation> GetRelationOf(IPersistEntity entity)
    {
        return GetRelationInfoOf(entity)
            .Select(i => i.GetValue(entity))
            .Where(e => e != null)
            .Cast<TRelation>();
    }
    
    /// <summary>
    /// Returns the target values of relation.
    /// </summary>
    /// <param name="entity">The host entity</param>
    /// <returns>An enumerable of TValue</returns>
    /// <exception cref="NotImplementedException">If property does not exist</exception>
    public IEnumerable<TValue> GetTargetOf(IPersistEntity entity)
    {
        var targetPropertyInfo = IfcRelationType.GetProperty(TargetPropertyName);
        if (null == targetPropertyInfo)
            throw new NotImplementedException($"Target property {TargetPropertyName} does not exist on type {IfcRelationType.FullName}.");
        
        return GetRelationOf(entity)
            .Select(r => targetPropertyInfo.GetValue(r))
            .Where(e => e != null)
            .Cast<TValue>();
    }

    /// <summary>
    /// Returns the parent values of this relation.
    /// </summary>
    /// <param name="entity">The host entity</param>
    /// <returns>An enumerable of PValue</returns>
    /// <exception cref="NotImplementedException">If the parent property does not exist</exception>
    public IEnumerable<PValue> GetParentOf(IPersistEntity entity)
    {
        var parentPropertyInfo = IfcRelationType.GetProperty(ParentPropertyName);
        if (null == parentPropertyInfo)
            throw new NotImplementedException($"Parent property {ParentPropertyName} does not exist on type {IfcRelationType.FullName}.");
        
        return GetRelationOf(entity)
            .Select(r => parentPropertyInfo.GetValue(r))
            .Where(e => e != null)
            .Cast<PValue>();
    }

    #region IIfcRelationDef Members
    
    IEnumerable<IIfcRelationship> IIfcRelationInfo.GetRelationOf(IPersistEntity entity)
    {
        return GetRelationOf(entity).Cast<IIfcRelationship>();
    }

    IEnumerable<IPersistEntity> IIfcRelationInfo.GetTargetOf(IPersistEntity entity)
    {
        return GetTargetOf(entity).Cast<IPersistEntity>();
    }

    IEnumerable<IPersistEntity> IIfcRelationInfo.GetParentOf(IPersistEntity entity)
    {
        return GetParentOf(entity).Cast<IPersistEntity>();
    }
    
    #endregion
}