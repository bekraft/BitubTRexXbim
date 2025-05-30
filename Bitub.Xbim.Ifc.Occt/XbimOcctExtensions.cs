using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Xbim.Common.Configuration;
using Xbim.Geometry.Engine.Interop;

using Xbim.Ifc4.Interfaces;

namespace Bitub.Xbim.Ifc;

public static class XbimOcctExtensions
{
    public static void EnsureGeometryServiceConfigured()
    {
        if (!XbimServices.Current.IsConfigured)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new NotSupportedException($"Requires WinOS platform.");
            
            XbimServices.Current.ConfigureServices(opt => opt.AddXbimToolkit(conf =>
            {
                conf.AddGeometryServices().AddHeuristicModel();
            }));
        }
    }

    public static IXbimManagedGeometryEngine GetGeometryManagedEngine(this XbimServices services)
    {
        EnsureGeometryServiceConfigured();
        var geometryServices = XbimServices.Current.ServiceProvider.GetRequiredService<IXbimGeometryServicesFactory>();
        var loggingFactory = XbimServices.Current.ServiceProvider.GetRequiredService<ILoggerFactory>();
        return new XbimGeometryEngine(geometryServices, loggingFactory);
    }
}