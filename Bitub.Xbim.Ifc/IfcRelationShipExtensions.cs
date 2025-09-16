using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;

namespace Bitub.Xbim.Ifc;

public static class IfcRelationShipExtensions
{
    #region Decomposition & spatial containment

    /// <summary>
    /// Delegates the <i>IsDecomposedBy</i> relationship of a single object.
    /// </summary>
    /// <param name="p">An IfcObjectDefinition</param>
    /// <returns>An enumeration of no, one or more IfcObjectDefinition instances</returns>
    public static IEnumerable<T> SubObjects<T>(this IIfcObjectDefinition p) where T : IIfcObjectDefinition
    {
        return p
            .IsDecomposedBy
            .SelectMany(s => s.RelatedObjects.OfType<T>());
    }

    /// <summary>
    /// Flattens the <i>Decomposes</i> relationship of an object. According to IFC constraints
    /// it should report only a single instance of IfcObjectDefinition.
    /// </summary>
    /// <param name="p">An IfcObjectDefinition</param>
    /// <returns>An enumeration (having a sinlge or no object)</returns>
    public static IEnumerable<T> SuperObject<T>(this IIfcObjectDefinition p) where T : IIfcObjectDefinition
    {
        return p
            .Decomposes
            .Select(s => s.RelatingObject).OfType<T>().Distinct();
    }

    /// <summary>
    /// Returns an enumeration of child objects having either a decomposition relation or spatial containment
    /// relation with the argument object.
    /// </summary>
    /// <typeparam name="T">A preferred type</typeparam>
    /// <param name="o">The parent</param>
    /// <returns>An enumeration of objects of given type</returns>
    public static IEnumerable<T> Children<T>(this IIfcObjectDefinition o) where T : IIfcObjectDefinition
    {
        var productSubs = o.SubObjects<T>();
        if (o is IIfcSpatialElement s)
            return Enumerable.Concat(productSubs, s.ContainsElements.SelectMany(r => r.RelatedElements.OfType<T>()));
        else
            return productSubs;
    }

    /// <summary>
    /// Returns all products of a spatial element.
    /// </summary>
    /// <typeparam name="T">The product type</typeparam>
    /// <param name="o">The parent object</param>
    /// <returns></returns>
    public static IEnumerable<T> ChildProducts<T>(this IIfcSpatialElement o) where T : IIfcProduct
    {
        return o.ContainsElements.SelectMany(r => r.RelatedElements.OfType<T>());
    }

    /// <summary>
    /// Returns an enumeration of parent objects having either a decomposition relation or spatial containment
    /// relation with the argument object.
    /// </summary>
    /// <typeparam name="T">A preferred type</typeparam>
    /// <param name="o">The child</param>
    /// <returns>An enumeration of parent objects</returns>
    public static IEnumerable<T> Parent<T>(this IIfcObjectDefinition o) where T : IIfcObjectDefinition
    {
        var productSupers = o.SuperObject<T>();
        if (o is IIfcProduct p)
            return Enumerable.Concat(productSupers, new IIfcProduct[] { p.IsContainedIn }.OfType<T>()).Distinct();
        else
            return productSupers;
    }

    #endregion

    #region Property relations

    public static IEnumerable<T> PropertiesAll<T>(this IIfcObject p) where T : IIfcProperty
    {
        return p.IsDefinedBy.SelectMany(r =>
        {
            if (r.RelatingPropertyDefinition is IIfcPropertySetDefinition set)
                return set.Properties<T>();
            else if (r.RelatingPropertyDefinition is IfcPropertySetDefinitionSet setOfSet)
                return setOfSet.PropertySetDefinitions.SelectMany(s => s.Properties<T>());
            else
                return Enumerable.Empty<T>();
        });
    }

    public static IEnumerable<T> PropertySet<T>(this IIfcRelDefinesByProperties r) where T : IIfcPropertySetDefinition
    {
        if (r.RelatingPropertyDefinition is IIfcPropertySetDefinition set)
            return Enumerable.Repeat(set, 1).OfType<T>();
        else if (r.RelatingPropertyDefinition is IfcPropertySetDefinitionSet setOfSet)
            return setOfSet.PropertySetDefinitions.OfType<T>();
        else
            return Enumerable.Empty<T>();
    }

    public static IEnumerable<T> PropertySets<T>(this IIfcObject o) where T : IIfcPropertySetDefinition
    {
        return o.IsDefinedBy.SelectMany(r => r.PropertySet<T>());
    }

    /// <summary>
    /// All property sets of object. Returns a tuple of set name vs. array of properties.
    /// </summary>
    /// <typeparam name="T">Property type scope</typeparam>
    /// <param name="p">Object in context</param>
    /// <returns>Sequence of pset names vs. array of hosted properties.</returns>
    public static IEnumerable<Tuple<string, T[]>> PropertiesSets<T>(this IIfcObject p) where T : IIfcProperty
    {
        return p.IsDefinedBy.SelectMany(r =>
        {
            if (r.RelatingPropertyDefinition is IIfcPropertySetDefinition set)
                return Enumerable.Repeat(new Tuple<string, T[]>(set.Name, set.Properties<T>().ToArray()), 1);
            else if (r.RelatingPropertyDefinition is IfcPropertySetDefinitionSet setOfSet)
                return setOfSet.PropertySetDefinitions.Select(s => new Tuple<string,T[]>(s.Name, s.Properties<T>().ToArray()));
            else
                return Enumerable.Empty<Tuple<string, T[]>>();
        });
    }

    /// <summary>
    /// All properties of type T within property set.
    /// </summary>
    /// <typeparam name="T">Property type scope</typeparam>
    /// <param name="set"></param>
    /// <returns>Sequence of properties</returns>
    public static IEnumerable<T> Properties<T>(this IIfcPropertySetDefinition set) where T : IIfcProperty
    {
        if (set is IIfcPropertySet pSet)
            return pSet.HasProperties.OfType<T>();
        else
            return Enumerable.Empty<T>();
    }

    #endregion

    #region General relation handling

    /// <summary>
    /// Will transfer all existing relations of a (more abstract) template to a (more specific) target instance.
    /// </summary>
    /// <typeparam name="T1">Template type</typeparam>
    /// <typeparam name="T2">Target type as specialisation of T1</typeparam>
    /// <param name="target">The target instance to attach relations to</param>
    /// <param name="template">The template instance providing relations (only existing by instance)</param>
    /// <returns>The modified target instance</returns>
    public static T2 CreateSameRelationshipsLike<T1, T2>(this T2 target, T1 template) where T1 : IPersistEntity where T2 : T1
    {
        var templateType = template.GetType();
        var targetType = target.GetType();

        // Scan through hosted indirect relations of template type T
        foreach (var relationProperty in templateType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(
                property => typeof(IEnumerable).IsAssignableFrom(property.GetMethod?.ReturnType) && property.GetMethod.ReturnType.IsGenericType)
            .Where(
                property => typeof(IIfcRelationship).IsAssignableFrom(property.GetMethod?.ReturnType.GenericTypeArguments[0])))
        {   
            // Scan through relation objects of type IEnumerable<? extends IIfcRelationship>
            var seq = relationProperty.GetValue(template) as IEnumerable;
            if (null != seq)
            {
                foreach (var relation in seq)
                {
                    var t1 = relation.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Select(p => p.PropertyType).ToArray();
                    foreach (var invRelationProperty in relation.GetType()
                                 .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(property =>
                                     typeof(IItemSet).IsAssignableFrom(property.PropertyType) &&
                                     property.PropertyType.IsGenericType)
                                 .Where(property =>
                                     property.PropertyType.GetGenericArguments()[0].IsAssignableFrom(targetType)))
                    {
                        var itemSet = invRelationProperty.GetValue(relation);
                        itemSet?.GetType().GetMethod("Add")?.Invoke(itemSet, new object[] { target });
                    }
                }
            }
        }
        return target;
    }

    /// <summary>
    /// Finds a relation typed of container <c>TContainer</c> with lower generic type param constraint <c>TParam</c>.
    /// </summary>
    /// <typeparam name="TContainer">The type of lower base property type.</typeparam>
    /// <typeparam name="TParam">The relation type</typeparam>
    /// <param name="type">The host type</param>
    /// <param name="propertyName">The relation name</param>
    /// <returns>Reflected property info.</returns>
    public static IEnumerable<PropertyInfo> GetLowerConstraintGenericProperty<TContainer,TParam>(this Type type,
        string? propertyName = null)
    {
        return GetLowerConstraintGenericProperty(type, typeof(TContainer), typeof(TParam), propertyName);
    }
    
    /// <summary>
    /// Finds a relation typed of container <c>containerType</c> with lower generic type param constraint <c>basePropertyType</c>.
    /// </summary>
    /// <param name="containerType">The type of lower base property type.</param>
    /// <param name="basePropertyType">The relation type</param>
    /// <param name="type">The host type</param>
    /// <param name="propertyName">The relation name</param>
    /// <returns>Reflected property info.</returns>
    public static IEnumerable<PropertyInfo> GetLowerConstraintGenericProperty(this Type type, 
        Type containerType, Type? basePropertyType, string? propertyName = null)
    {
        return type.GetInterfaces()
            .SelectMany(t => t.GetProperties())
            .Where(p => (null == propertyName || p.Name == propertyName) && containerType.IsAssignableFrom(p.PropertyType))
            .Where(p => (null == basePropertyType || p.PropertyType.GetGenericArguments().All(basePropertyType.IsAssignableFrom)));
    }

    /// <summary>
    /// Finds a relation typed by lower constraint <c>TParam</c> which implements <see cref="IItemSet"/>.
    /// </summary>
    /// <typeparam name="TParam">The relation type</typeparam>
    /// <param name="type">The host type</param>
    /// <param name="relationName">The relation name</param>
    /// <returns>Reflected property info.</returns>
    public static PropertyInfo? GetLowerConstraintNaryRelationType<TParam>(this Type type, 
        string? relationName = null)
    {
        return type.GetLowerConstraintGenericProperty<IItemSet, TParam>(relationName).FirstOrDefault();
    }

    /// <summary>
    /// Determines whether the given property is a sub type of given <c>TParam</c>.
    /// </summary>
    /// <typeparam name="TParam">The generic argument of relation or property</typeparam>
    /// <param name="propertyInfo">The property</param>
    /// <returns>True, if relation is a super generic type of given type</returns>
    public static bool IsLowerConstraintPropertyType<TParam>(this PropertyInfo propertyInfo)
    {
        return IsLowerConstraintPropertyType(propertyInfo, typeof(TParam));
    }
    
    /// <summary>
    /// Determines whether the given property is a sub type of given <c>TParam</c>.
    /// </summary>
    /// <param name="propertyInfo">The property</param>
    /// <param name="tParam">The param type</param>
    /// <returns>True, if relation is a super generic type of given type</returns>
    public static bool IsLowerConstraintPropertyType(this PropertyInfo propertyInfo, Type tParam)
    {
        return tParam.IsAssignableFrom(propertyInfo.PropertyType);
    }

    /// <summary>
    /// Determines whether there is an equivalently named property as a super generic type of given <c>TParam</c>.
    /// Additionally, to <see cref="IsLowerConstraintPropertyType{TParam}"/>:
    /// If the property redirects to a simple relation <c>TParam</c> denotes the expected base type.
    /// If the property redirects to a n-ary relation <c>TParam</c> denotes the expected generic parameter base type.
    /// </summary>
    /// <typeparam name="TParam">The generic argument of relation</typeparam>
    /// <param name="propertyInfo">The property</param>
    /// <returns>True, if there's a relation as a super generic type of given type</returns>
    public static bool HasLowerConstraintRelationType<TParam>(this PropertyInfo propertyInfo)
    {
        return HasLowerConstraintRelationType(propertyInfo, typeof(TParam));
    }

    /// <summary>
    /// Determines whether there is an equivalently named property as a super generic type of given <c>TParam</c>.
    /// Additionally, to <see cref="IsLowerConstraintPropertyType{TParam}"/>:
    /// If the property redirects to a simple relation <c>tParam</c> denotes the expected base type.
    /// If the property redirects to a n-ary relation <c>tParam</c> denotes the expected generic parameter base type.
    /// </summary>
    /// <param name="propertyInfo">The property</param>
    /// <param name="tParam">The param type</param>
    /// <returns>True, if there's a relation as a super generic type of given type</returns>
    public static bool HasLowerConstraintRelationType(this PropertyInfo propertyInfo, Type tParam)
    {
        return IsLowerConstraintPropertyType(propertyInfo, tParam)
               || ((typeof(IItemSet).IsAssignableFrom(propertyInfo.PropertyType) 
                    || typeof(IOptionalItemSet).IsAssignableFrom(propertyInfo.PropertyType)) 
                   && propertyInfo.PropertyType.GetGenericArguments().All(tParam.IsAssignableFrom));
    }

    /// <summary>
    /// Will add related instances to host instance using given <c>relationName</c> and lower constraint type <c>TParam</c>.
    /// </summary>
    /// <typeparam name="TParam">The relation type</typeparam>
    /// <typeparam name="T">The host type (implicit)</typeparam>
    /// <param name="hostInstance">The host instance</param>
    /// <param name="relationName">The relation name</param>
    /// <param name="instances">The related instances</param>
    public static bool AddRelationsByLowerConstraint<TParam>(this IPersistEntity hostInstance, string relationName, IEnumerable<TParam> instances)
    {
        var propertyInfo = hostInstance.GetType().GetLowerConstraintNaryRelationType<TParam>(relationName);

        var items = propertyInfo?.GetValue(hostInstance);
        var addRange = items?.GetType().GetMethod("AddRange");
        if (null == items || null == addRange)
            return false;

        addRange.Invoke(items, new object[] { instances.Cast<TParam>().ToList() });
        return true;
    }
    
    /// <summary>
    /// Get property value from relationship.
    /// </summary>
    /// <param name="metaProperty">The EXPRESS property</param>
    /// <param name="instance">The object instance</param>
    /// <param name="value">The out value if found</param>
    /// <typeparam name="TParam">Value type</typeparam>
    /// <returns>An object of value type</returns>
    /// <exception cref="NotSupportedException"></exception>
    public static bool TryGetSingleValue<TParam>(this ExpressMetaProperty metaProperty, object instance, out TParam? value)
    {
        value = default;
        if (IsLowerConstraintPropertyType<TParam>(metaProperty.PropertyInfo))
        {
            if (metaProperty.PropertyInfo.GetValue(instance) is TParam param)
            {
                value = param;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Get property value as an enumerable from relationship.
    /// </summary>
    /// <param name="metaProperty">The EXPRESS property</param>
    /// <param name="instance">The object instance</param>
    /// <param name="values">The out values argument, EMPTY if not found</param>
    /// <typeparam name="TParam">The generic inner type of enumerable</typeparam>
    /// <returns></returns>
    public static bool TryGetMultiValue<TParam>(this ExpressMetaProperty metaProperty, object instance, out IEnumerable<TParam>? values)
    {
        values = Array.AsReadOnly<TParam>([]);
        if (HasLowerConstraintRelationType<TParam>(metaProperty.PropertyInfo))
        {
            values = metaProperty.PropertyInfo.GetValue(instance) as IEnumerable<TParam>;
            return null != values;
        }
        return false;
    }
    
    /// <summary>
    /// Get the values from given property, insensible to single or multi valued relations.
    /// </summary>
    /// <param name="metaProperty">The EXPRESS property</param>
    /// <param name="instance">The object instance</param>
    /// <param name="values">The out values argument, EMPTY if not found</param>
    /// <typeparam name="TParam"></typeparam>
    /// <returns></returns>
    public static bool TryGetValues<TParam>(this ExpressMetaProperty metaProperty, object instance, out IEnumerable<TParam>? values)
    {
        values = Array.AsReadOnly<TParam>([]);
        if (IsLowerConstraintPropertyType<TParam>(metaProperty.PropertyInfo))
        {
            if (metaProperty.PropertyInfo.GetValue(instance) is TParam param)
            {
                values = Array.AsReadOnly([ param ]);
                return true;
            }
        } 
        else if (HasLowerConstraintRelationType<TParam>(metaProperty.PropertyInfo))
        {
            if (metaProperty.PropertyInfo.GetValue(instance) is IList itemSet)
            {
                // Wrap item set into list
                var list = new List<TParam>();
                foreach (var item in itemSet)
                {
                    list.Add((TParam)item);
                }
                values = list.AsReadOnly();
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Set the property value to relationship.
    /// </summary>
    /// <param name="metaProperty">The EXPRESS property</param>
    /// <param name="instance">The object instance</param>
    /// <param name="value">The value to be set</param>
    /// <typeparam name="TParam">Value type</typeparam>
    /// <exception cref="NotSupportedException"></exception>
    public static void SetSingleValue<TParam>(this ExpressMetaProperty metaProperty, object instance, TParam? value)
    {
        if (HasLowerConstraintRelationType<TParam>(metaProperty.PropertyInfo))
            metaProperty.PropertyInfo.SetValue(instance, value);
        else
            throw new NotSupportedException($"Property MUST support {typeof(TParam)}");
    }

    #endregion
}