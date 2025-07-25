using System;
using System.Runtime.InteropServices;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Xbim.Common;
using Xbim.Common.Configuration;
using Xbim.Geometry.Abstractions;
using Xbim.Geometry.Engine.Interop;
using Xbim.Geometry.Engine.Interop.Configuration;

using Xbim.Ifc4.Interfaces;
using Xbim.ModelGeometry.Scene;

namespace Bitub.Xbim.Ifc;

public static class XbimOcctExtensions
{
    /// <summary>
    /// Geometry engine option used to create new instances.
    /// </summary>
    public static GeometryEngineOptions EngineOptions { get; set; } = new GeometryEngineOptions
        { GeometryEngineVersion = XGeometryEngineVersion.V6 };
    
    /// <summary>
    /// Whether to use heuristic store.
    /// </summary>
    public static bool UseHeuristicStoreType { get; set; } = true;
    
    /// <summary>
    /// A logger factory to use whenever Xbim services are initialized.
    /// </summary>
    public static ILoggerFactory LoggerFactory { get; set; }

    /// <summary>
    /// Sets the maximum thread count for geometry creation.
    /// </summary>
    public static uint MaxThreadsOnGeometryCreation { get; set; } = 1;
    
    /// <summary>
    /// Configure Xbim services.
    /// </summary>
    /// <exception cref="NotSupportedException">If a non-win OS is detected.</exception>
    public static void ConfigureGeometryServiceWinOs()
    {
        if (!XbimServices.Current.IsBuilt)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new NotSupportedException($"Requires WinOS platform.");
            
            XbimServices.Current.ConfigureServices(opt => opt.AddXbimToolkit(conf =>
            {
                conf.AddGeometryServices();
                if (UseHeuristicStoreType)
                    conf.AddHeuristicModel();
                else
                    conf.AddMemoryModel();
               
                if (LoggerFactory != null)
                    conf.AddLoggerFactory(LoggerFactory);
            }));
        }
    }

    /// <summary>
    /// Creates a new instance of managed geometry engine according to <see cref="EngineOptions"/>.
    /// </summary>
    /// <param name="services">The Xbim services instance</param>
    /// <returns>A new geometry engine</returns>
    public static IXbimManagedGeometryEngine CreateGeometryManagedEngine(this XbimServices services)
    {
        ConfigureGeometryServiceWinOs();
        var geometryServices = XbimServices.Current.ServiceProvider.GetRequiredService<IXbimGeometryServicesFactory>();
        var loggingFactory = XbimServices.Current.ServiceProvider.GetRequiredService<ILoggerFactory>();
        return new XbimGeometryEngine(geometryServices, loggingFactory, EngineOptions);
    }

    /// <summary>
    /// Wraps a new model context onto given model.
    /// </summary>
    /// <param name="services">The Xbim services instance</param>
    /// <param name="model">The IFC model</param>
    /// <param name="logger">The logger</param>
    /// <param name="maxThreadsToUse">Max thread to be used, default is to use static global <see cref="MaxThreadsOnGeometryCreation"/> parameter.</param>
    /// <returns>A newly created model context</returns>
    public static Xbim3DModelContext CreateGeometryModelContext(this XbimServices services,
        IModel model,
        ILogger logger,
        uint maxThreadsToUse = 0)
    {
        ConfigureGeometryServiceWinOs();
        return new Xbim3DModelContext(model, "model", null, logger, EngineOptions.GeometryEngineVersion)
        {
            MaxThreads = maxThreadsToUse > 0 ? (int)maxThreadsToUse : (int)MaxThreadsOnGeometryCreation,
        };
    }
}