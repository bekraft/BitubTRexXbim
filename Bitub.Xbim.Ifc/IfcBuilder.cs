using System;
using System.Collections.Generic;
using System.Linq;

using Xbim.IO;

using Xbim.Ifc4.Interfaces;
using Xbim.Ifc;

using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Common.Geometry;

using Microsoft.Extensions.Logging;

using Bitub.Dto;

namespace Bitub.Xbim.Ifc;

/// <summary>
/// Generic Ifc builder bound to an IFC schema version and assembly.
/// </summary>
public abstract class IfcBuilder
{
    public readonly IModel Model;
    public readonly IfcAssemblyScope IfcAssembly;

    /// <summary>
    /// Default application.
    /// </summary>
    public static readonly ApplicationData DefaultApplication = new ApplicationData 
    {
        ApplicationID = "trex",
        ApplicationName = "BitubTRex"
    };

    /// <summary>
    /// Default author.
    /// </summary>
    public static readonly AuthorData DefaultAuthorData = new AuthorData
    {
        Name = "(anonymous)",
        GivenName = ""
    };

    /// <summary>
    /// The main IFC entity factory scoped to <see cref="IPersistEntity"/> as base implementation of any Xbim IFC persistent object.
    /// </summary>
    public readonly IfcEntityScope<IPersistEntity> IfcEntityScope;

    #region Internals        
    protected Stack<IIfcObjectDefinition> InstanceScopeStack { get; private set; } = new ();

    private readonly ILogger? _log;
    private readonly Qualifier _schema;

    /// <summary>
    /// New builder attached to IFC entity assembly given as scope.
    /// </summary>
    /// <param name="model">A store.</param>
    /// <param name="loggerFactory">The logger factory</param>
    protected IfcBuilder(IModel model, ILoggerFactory? loggerFactory = null)
    {
        // Principle properties
        _log = loggerFactory?.CreateLogger(GetType());
        _schema = model.SchemaVersion.ToString().ToQualifier();
        
        IfcAssembly = IfcAssemblyScope.SchemaAssemblyScope[model.SchemaVersion];
        Model = model;

        // Type scopes
        IfcEntityScope = new IfcEntityScope<IPersistEntity>(this);

        OwningUser = Model.Instances.OfType<IIfcPersonAndOrganization>().FirstOrDefault();
        OwningApplication = Model.Instances.OfType<IIfcApplication>().FirstOrDefault();
        
        Transactive(m1 =>
        {
            if (m1 is not IfcStore s) return;
            OwningUser ??= s.DefaultOwningUser;
            OwningApplication ??= s.DefaultOwningApplication;
        });            
    }

    private void NewContainer(IIfcObjectDefinition container)
    {
        var scope = CurrentScope;
        InstanceScopeStack.Push(container);
        Model.NewDecomposes(scope).RelatedObjects.Add(container);
    }

    /// <summary>
    /// Inits a new project specific to implementing IFC version.
    /// </summary>
    /// <param name="projectName">The project name</param>
    /// <returns>A valid template project</returns>
    protected abstract IIfcProject InitNewProject(string projectName);

    #endregion

    /// <summary>
    /// Returns the current schema version of IFC builder instance.
    /// </summary>
    public Qualifier Schema => new Qualifier(_schema);

    /// <summary>
    /// Current owner history entry.
    /// </summary>
    public IIfcOwnerHistory? OwnerHistoryTag { get; set; }

    /// <summary>
    /// New builder wrapping a new in-memory IFC model.
    /// </summary>
    /// <param name="c">The editor's credentials</param>
    /// <param name="version">The schema version</param>
    /// <param name="loggerFactory">A logger factory</param>
    /// <returns>A builder instance</returns>
    public static IfcBuilder WithCredentials(XbimEditorCredentials c, 
        XbimSchemaVersion version = XbimSchemaVersion.Ifc4, ILoggerFactory? loggerFactory = null)
    {
        var newStore = IfcStore.Create(version, XbimStoreType.InMemoryModel);
        return WithModel(newStore, loggerFactory);
    }    

    /// <summary>
    /// Wraps an existing store.
    /// </summary>
    /// <param name="model">The model</param>
    /// <param name="loggerFactory">The logger factory</param>
    /// <returns>A new builder instance</returns>
    public static IfcBuilder WithModel(IModel model, ILoggerFactory? loggerFactory = null)
    {
        switch (model.SchemaVersion)
        {
            case XbimSchemaVersion.Ifc2X3:
                return new Ifc2x3Builder(model, loggerFactory);
            case XbimSchemaVersion.Ifc4:
                return new Ifc4Builder(model, loggerFactory);
            case XbimSchemaVersion.Ifc4x3:
                return new Ifc4x3Builder(model, loggerFactory);
        }
        throw new NotImplementedException($"Missing implementation for ${model.SchemaVersion}");
    }

    /// <summary>
    /// Will start a new <see cref="IIfcProject"/> from scratch.
    /// </summary>
    /// <param name="projectName">The name of the project.</param>
    /// <param name="c">Editor's name and identification data</param>
    /// <param name="version">The schema version</param>
    /// <param name="loggerFactory">Optional logger factory</param>
    /// <returns>A builder wrapping a pre-filled model.</returns>
    public static IfcBuilder WithNewProject(string projectName, XbimEditorCredentials c,
        XbimSchemaVersion version = XbimSchemaVersion.Ifc4, ILoggerFactory? loggerFactory = null)
    {
        var builder = WithCredentials(c, version, loggerFactory);
        // Initialization
        builder.Transactive(s =>
        {
            var project = builder.InitNewProject(projectName);
            builder.OwnerHistoryTag = builder.NewOwnerHistoryEntry(IfcChangeActionEnum.ADDED);
            project.OwnerHistory = builder.OwnerHistoryTag;
            builder.InstanceScopeStack.Push(project);
        });
        return builder;
    }

    /// <summary>
    /// Current known user.
    /// </summary>
    public IIfcPersonAndOrganization? OwningUser { get; set; }

    /// <summary>
    /// Current known application.
    /// </summary>
    public IIfcApplication? OwningApplication { get; set; }
    
    /// <summary>
    /// New adding owner history (versioning) entry. 
    /// </summary>
    /// <returns>A new owner history entry</returns>
    public IIfcOwnerHistory NewOwnerHistoryEntry(IfcChangeActionEnum changeAction)
    {
        var ownerHistory = IfcEntityScope.NewOf<IIfcOwnerHistory>();
        ownerHistory.OwningUser = OwningUser;
        ownerHistory.OwningApplication = OwningApplication;
        ownerHistory.ChangeAction = changeAction;
        ownerHistory.CreationDate = DateTime.Now;
        return ownerHistory;
    }

    public IIfcApplication NewApplicationData(ApplicationData application)
    {
        IIfcApplication? app = null;
        Transactive(m =>
        {
            app = IfcEntityScope.NewOf<IIfcApplication>(e =>
            {
                e.ApplicationIdentifier = application.ApplicationID;
                e.ApplicationFullName = application.ApplicationName;
                e.Version = application.Version;
            });
        });
        return app!;
    }

    /// <summary>
    /// Current top scope of model hierarchy.
    /// </summary>
    public IIfcObjectDefinition CurrentScope => InstanceScopeStack.Peek();

    /// <summary>
    /// Current top placement in model hierarchy.
    /// </summary>
    public IIfcObjectPlacement? CurrentPlacement => InstanceScopeStack
        .OfType<IIfcProduct>()
        .FirstOrDefault(p => p.ObjectPlacement != null)?
        .ObjectPlacement;

    /// <summary>
    /// Returns a collection of concrete product types (which might be an IfcElement)
    /// </summary>
    public IEnumerable<Type> InstantiableProducts => IfcEntityScope.Implementing<IIfcProduct>();

    /// <summary>
    /// Returns a subset of IfcProduct which is conforming to IfcElement
    /// </summary>
    public IEnumerable<Type> InstantiableElements => IfcEntityScope.Implementing<IIfcElement>();

    public string TransactionContext => $"Modification ${DateTime.Now}";

    /// <summary>
    /// Wraps an IfcStore modification into a transaction context.
    /// </summary>
    /// <param name="action">The modification return true, if transaction shall be applied</param>
    public void Wrap(Func<IModel,bool> action)
    {
        using (var txn = Model.BeginTransaction(TransactionContext))
        {
            try
            {
                if (action(Model))
                {
                    txn.Commit();
                }
                else
                {
                    txn.RollBack();
                    _log?.LogWarning($"Detected cancellation of commit '{txn.Name}'");
                }
            }
            catch(Exception e)
            {
                txn.RollBack();
                _log?.LogError(e, "Exception caught. Rollback done.");
            }
        }
    }

    /// <summary>
    /// Wraps an IfcStore modification into a transaction context.
    /// </summary>
    /// <param name="action">The modification</param>
    public void Transactive(Action<IModel> action)
    {
        if (null != Model.CurrentTransaction)
        {
            action?.Invoke(Model);
        }
        else
        {
            using (var txn = Model.BeginTransaction(TransactionContext))
            {
                try
                {
                    action?.Invoke(Model);
                    txn.Commit();
                }
                catch (Exception e)
                {
                    txn.RollBack();
                    _log?.LogError(e, "Exception caught. Rollback done.");
                }
            }
        }
    }

    /// <summary>
    /// Adds a placement to current top scope.
    /// </summary>
    /// <returns>A local placement reference</returns>
    public IIfcLocalPlacement NewLocalPlacement(XbimVector3D refPosition, bool scaleUp = false)
    {
        IIfcLocalPlacement? placement = null;
        Transactive(s =>
        {
            var product = CurrentScope as IIfcProduct;
            var relPlacement = CurrentPlacement;
            if (null != product)
            {
                if (null == product.ObjectPlacement)
                {
                    placement = s.NewLocalPlacement(refPosition, scaleUp);
                    if (relPlacement != product.ObjectPlacement)
                        // Don't reference former placement while replacing own placement
                        placement.PlacementRelTo = relPlacement;

                    product.ObjectPlacement = placement;
                }
                else
                {
                    _log?.LogWarning("Entity {Label} has already a placement by label {placement}.", 
                        product.EntityLabel, product.ObjectPlacement.EntityLabel);
                }
            }
            else
            {
                throw new OperationCanceledException("No IfcProduct as head of current hierarchy");
            }
        });
        return placement!;
    }

    public E New<E>(Action<E>? modifier = null) where E : IPersistEntity
    {
        return New<E>(typeof(E), modifier);
    }

    public E New<E>(Type t, Action<E>? modifier = null) where E : IPersistEntity
    {
        E? entity = default(E);
        Transactive(s =>
        {
            entity = (E)IfcEntityScope.New<E>(t);
            modifier?.Invoke(entity);
        });
        return entity!;
    }

    public IIfcSite NewSite(string? siteName = null)
    {
        IIfcSite? site = null;
        Transactive(s =>
        {
            site = IfcEntityScope.NewOf<IIfcSite>();
            site.OwnerHistory = OwnerHistoryTag;
            site.Name = siteName;

            NewContainer(site);
        });

        return site!;
    }

    public IIfcBuilding NewBuilding(string? buildingName = null)
    {
        IIfcBuilding? building = null;
        Transactive(s =>
        {
            building = IfcEntityScope.NewOf<IIfcBuilding>();
            building.OwnerHistory = OwnerHistoryTag;
            building.Name = buildingName;

            NewContainer(building);
        });

        return building!;
    }

    public IIfcBuildingStorey NewStorey(string? name = null, double elevation = 0)
    {
        IIfcBuildingStorey? storey = null;
        Transactive(s =>
        {
            storey = IfcEntityScope.NewOf<IIfcBuildingStorey>();
            storey.Name = name;
            storey.OwnerHistory = OwnerHistoryTag;
            storey.Elevation = elevation;

            NewContainer(storey);
        });

        return storey!;
    }

    private void InitProduct(IIfcProduct product)
    {
        var cScope = CurrentScope;
        if (cScope is IIfcSpatialStructureElement e)
        {
            // If spatial container at head, create containment
            Model.NewContains(e).RelatedElements.Add(product);
        }
        else
        {
            // Otherwise create an aggregation relation
            Model.NewDecomposes(cScope).RelatedObjects.Add(product);
        }
    }

    /// <summary>
    /// New product instance given by type parameter.
    /// </summary>
    /// <typeparam name="P">The product type</typeparam>
    /// <param name="placement">A placement</param>
    /// <param name="name">An optional name</param>
    /// <returns>New stored product</returns>
    public P NewProduct<P>(IIfcLocalPlacement? placement = null, string? name = null) where P : IIfcProduct
    {
        P? product = default(P);
        Transactive(s =>
        {
            product = IfcEntityScope.NewOf<P>();
            product.Name = name;
            product.ObjectPlacement = placement;
            InitProduct(product);
        });
        return product!;
    }

    /// <summary>
    /// New product instance given by type parameter.
    /// </summary>
    /// <param name="productName">A type label of the product instance</param>
    /// <param name="placement">A placement</param>
    /// <param name="name">An optional name</param>
    /// <returns>New stored product</returns>
    public IIfcProduct NewProduct(Qualifier productName, IIfcLocalPlacement? placement = null, string? name = null)
    {
        IIfcProduct? product = null;
        if (!_schema.IsSuperQualifierOf(productName))
            throw new ArgumentException($"Wrong schema version of pName. Store is a {Model.SchemaVersion}");

        Transactive(s =>
        {
            product = IfcEntityScope.New(productName) as IIfcProduct;
            if (null == product)
                throw new ArgumentException($"Wrong product name: {productName.ToLabel()}");
            
            product.Name = name;
            product.ObjectPlacement = placement;
            InitProduct(product);
        });
        
        return product!;
    }

    /// <summary>
    /// Wrap subsequent product creation by given product into a assembly group.
    /// </summary>
    /// <param name="p">The new group product</param>
    public void NewScope(IIfcProduct p)
    {
        if (InstanceScopeStack.Any(e => e == p))
            throw new ArgumentException($"#{p.EntityLabel} already scoped.");

        NewContainer(p);
    }

    /// <summary>
    /// Drop current product scope.
    /// </summary>
    /// <returns>Dropped container or null, if there's no.</returns>
    public IIfcObjectDefinition? DropCurrentScope()
    {
        if (InstanceScopeStack.Count > 1)
            return InstanceScopeStack.Pop();
        else
            return null;
    }

    public P NewProperty<P>(string propertyName, string? description = null) where P : IIfcProperty
    {
        P? property = default(P);
        Transactive(s =>
        {
            property = IfcEntityScope.NewOf<P>();
            property.Name = propertyName;
            property.Description = description;
        });
        return property!;
    }

    public T NewValueType<T>(object value) where T : IIfcValue
    {
        return IfcEntityScope.NewOf<T>(value);
    }

    public IIfcRelDefinesByProperties NewPropertySet(string propertySetName, 
        string? description = null, IIfcProduct? initialProduct = null)
    {
        IIfcRelDefinesByProperties? pSetRel = null;
        Transactive(s =>
        {
            var set = s.NewIfcPropertySet(propertySetName, description);
            pSetRel = s.NewIfcRelDefinesByProperties(set);
            if(null != initialProduct)
                pSetRel.RelatedObjects.Add(initialProduct);
        });
        return pSetRel!;
    }

    public IIfcNamedUnit NewSIUnit(IIfcSIUnit unit)
    {
        return NewSIUnit(unit.UnitType, unit.Name, unit.Prefix);
    }
    
    public IIfcNamedUnit NewSIUnit(IfcUnitEnum unitType, IfcSIUnitName name, 
        IfcSIPrefix? prefix = null)
    {
        var ifcSiUnitType = IfcEntityScope.Implementing<IIfcSIUnit>().First();
        var ifcSiUnit = New<IIfcSIUnit>(ifcSiUnitType);

        ifcSiUnit.UnitType = unitType;
        ifcSiUnit.Name = name;
        ifcSiUnit.Prefix = prefix;
        return ifcSiUnit;
    }
}
