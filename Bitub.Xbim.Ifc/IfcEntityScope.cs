using System;
using System.Linq;

using Xbim.Common;

using Bitub.Dto;

namespace Bitub.Xbim.Ifc;

/// <summary>
/// Generic Ifc entity creator scope bound to a builder.
/// </summary>
/// <typeparam name="T">The type of entity</typeparam>
public class IfcEntityScope<T> : TypeScope where T : IPersist
{
    #region Internals

    private readonly IfcBuilder Builder;
    
    #endregion


    public IfcEntityScope(IfcBuilder builder) 
        : base(typeof(T), builder.IfcAssembly, new [] { builder.IfcAssembly.Factory.GetType().Module })
    {
        this.Builder = builder;
    }
    
    public IfcEntityScope<E> GetEntityScopeOf<E>() where E : T
    {
        return new IfcEntityScope<E>(Builder);
    }

    public E New<E>(Type t, Action<E>? mod = null) where E : IPersistEntity
    {
        if (!typeof(E).IsAssignableFrom(t))
            throw new ArgumentException($"Type '{t.Name}' has to be equal or more specific as '{typeof(E).Name}'");

        var result = (E)Builder.Model.Instances.New(this[GetScopedQualifier(t)]);
        mod?.Invoke(result);
        return result;
    }

    public E New<E>(Action<E>? mod = null) where E : T
    {
        E result = (E)Builder.Model.Instances.New(this[GetScopedQualifier(typeof(E))]);
        mod?.Invoke(result);
        return result;
    }

    public T New(Qualifier qualifiedType)
    {
        return (T)Builder.Model.Instances.New(this[qualifiedType]);
    }

    public E NewOf<E>(object value) where E : IExpressValueType
    {
        var typeList = Implementing<E>().ToList();
        var valueType = typeList.First();
        var ctor = valueType.GetConstructor(new Type[] { value.GetType() });
        if (null == ctor)
            throw new NotSupportedException($"Type '{valueType.Name}' does not have a public parameterless constructor");
        return (E)ctor.Invoke(new object[] { value });
    }

    public E NewOf<E>(Action<E>? mod = null) where E : T, IPersistEntity
    {
        var typeList = Implementing<E>().ToList();
        E result = (E)Builder.Model.Instances.New(typeList.First());
        mod?.Invoke(result);            
        return result;
    }
}