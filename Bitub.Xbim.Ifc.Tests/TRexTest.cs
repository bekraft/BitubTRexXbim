using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Bitub.Xbim.Ifc.Transform;

using Bitub.Dto;

using Xbim.IO;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Common;
using Xbim.Common.Geometry;

using NUnit.Framework;

using Microsoft.Extensions.Logging;
using Xbim.Common.Configuration;

namespace Bitub.Xbim.Ifc.Tests;

public abstract class TRexTest<T>
{
    protected double Precision { get; } = 1e-5;

    protected ILoggerFactory LoggerFactory { get; } = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddSimpleConsole(options =>
    {
        options.IncludeScopes = true;
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    }));

    protected ILogger Logger { get; private set; }

    protected TRexTest()
    {
        Logger = LoggerFactory.CreateLogger<T>();

        if (!XbimServices.Current.IsBuilt)
        {
           BuildServiceProvider();
        }
    }

    protected virtual void BuildServiceProvider()
    {
        XbimServices.Current.ConfigureServices(opt =>
            opt.AddXbimToolkit(conf =>
                conf.AddMemoryModel().AddLoggerFactory(LoggerFactory)
            ));
    }

    protected XbimEditorCredentials EditorCredentials { get; } = new XbimEditorCredentials
    {
        ApplicationDevelopersName = "Bitub",
        ApplicationFullName = "Testing Bitub.Ifc",
        ApplicationIdentifier = "Bitub.Ifc",
        ApplicationVersion = "1.0",
        EditorsFamilyName = "One",
        EditorsGivenName = "Some",
        EditorsOrganisationName = "Self Employed"
    };

    protected CancelableProgressing NewProgressMonitor(bool cancelable = false)
    {
        var monitor = new CancelableProgressing(cancelable);
        monitor.OnProgressChange += (sender, e) => Logger.LogDebug($"State {e.State}: Percentage = {e.Percentage}; State object = {e.StateObject}");
        monitor.OnProgressEnd += (sender, e) => Logger.LogInformation($"Progress has ended: {e.State}");
        return monitor;
    }

    protected void IsSameArrayElements(object[] asserted, object[] actual)
    {
        foreach(var x in actual)
        {
            if (!asserted.Any(a => a.Equals(x)))
                Assert.Fail($"{x} hasn't been found in ({string.Join(",", asserted)})");
        }
    }

    protected void AssertIdentityPlacement(IIfcLocalPlacement localPlacement)
    {
        if (localPlacement.RelativePlacement is IIfcAxis2Placement3D a)
        {
            if (null != a.Axis)
                Assert.IsTrue(a.Axis.ToXbimVector3D().IsEqual(new XbimVector3D(0, 0, 1), Precision), "Axis fails" );
            if (null != a.RefDirection)
                Assert.IsTrue(a.RefDirection.ToXbimVector3D().IsEqual(new XbimVector3D(1, 0, 0), Precision), "RefDirection fails");
            if (a.Location is { } p)
            {
                Assert.That(p.Coordinates.Count, Is.EqualTo(3), "No 3d");
                Assert.That(p.ToXbimVector3D().IsEqual(XbimVector3D.Zero, Precision), Is.True, "Location fails");
            }
            else
            {
                Assert.Fail($"Wrong type Location type '{a.Location?.ExpressType.ExpressName}'");
            }
        }
        else
        {
            Assert.Fail($"Wrong type RelativePlacement type '{localPlacement.RelativePlacement?.ExpressType.ExpressName}'");
        }
    }
    
    protected IModel ReadIfcModel(string resourceName)
    {
        return IfcStore.Open(
            ReadEmbeddedFileStream(resourceName), StorageType.Ifc, XbimModelType.MemoryModel);
    }

    protected void SaveResultTarget(TransformResult result)
    {
        var store = result.Source as IfcStore;
        if (null != store && !result.IsCanceledOrBroken)
        {
            var fileNameWithoutExtension = store.FileName.Substring(0, store.FileName.LastIndexOf('.'));
            result.Target.SaveAsIfc(new FileStream($"{fileNameWithoutExtension}_Result.ifc", FileMode.Create));
        }
    }

    protected Stream ReadEmbeddedFileStream(string resourceName)
    {
        var assembly = Assembly.GetAssembly(GetType());
        var resourceNames = assembly?.GetManifestResourceNames();
        if (!resourceNames?.Contains(resourceName) ?? false)
            throw new ArgumentException($"Resource '{resourceName}' not found in assembly '{assembly.GetName().Name}'");

        return assembly?.GetManifestResourceStream(resourceName) ?? Stream.Null;
    }

    protected string ReadUtf8TextFrom(string resourceName)
    {
        using var fs = ReadEmbeddedFileStream(resourceName);
        return ReadUtf8TextFrom(fs);
    }

    protected string ReadUtf8TextFrom(Stream binStream)
    {
        using var sr = new StreamReader(binStream, System.Text.Encoding.UTF8);
        return sr.ReadToEnd();
    }

    protected string ResolveFilename(string localPath)
    {
        return Path.Combine(ExecutingFullpath, localPath);
    }

    protected string ExecutingFullpath
    {
        get {
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(assemblyLocation);
        }
    }
}
