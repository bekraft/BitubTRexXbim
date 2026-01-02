using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Bitub.Dto;

namespace Bitub.Xbim.Ifc;

/// <summary>
/// An assembly type classification helper.
/// </summary>
public class AssemblyScope
{
    public readonly Assembly[] AssemblySpaces;
    public readonly StringComparison ComparisonType;

    public AssemblyScope(params Assembly[] assemblies) : this(StringComparison.Ordinal, assemblies)
    {
    }

    public AssemblyScope(StringComparison stringComparisonType, params Assembly[] assemblies)
    {
        AssemblySpaces = assemblies;
        ComparisonType = stringComparisonType;
    }

    public IEnumerable<Type> Implementing(Type baseType)
    {
        return AssemblySpaces.SelectMany(a => a.ExportedTypes.Where(t => t.IsSubclassOf(baseType) || t.GetInterfaces().Any(i => i == baseType)));
    }

    public IEnumerable<Type> GetLocalType(Qualifier name)
    {
        return AssemblySpaces.SelectMany(a => a.ExportedTypes.Where(t => 0 == string.Compare(t.Name, name.GetLastFragment(), ComparisonType)));
    }

    public IEnumerable<Qualifier> TypeNames => AssemblySpaces.SelectMany(a => a.ExportedTypes.Select(t => t.ToQualifier()));

    public IEnumerable<AssemblyName> SpaceNames => AssemblySpaces.Select(a => a.GetName());

    public IEnumerable<Module> Modules => AssemblySpaces.SelectMany(a => a.Modules);

    public virtual Qualifier GetModuleQualifer(Module module)
    {
        return module.Name.ToQualifier();
    }

    public TypeScope GetScopeOf<TBase>()
    {
        return GetScopeOf<TBase>(Modules.ToArray());
    }

    public TypeScope GetScopeOf<TBase>(Module module)
    {
        return new TypeScope(typeof(TBase), this, new Module[] { module });
    }

    public TypeScope GetScopeOf<TBase>(IEnumerable<Module> modules)
    {
        return new TypeScope(typeof(TBase), this, modules.ToArray());
    }
}
