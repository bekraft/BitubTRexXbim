using System.IO;
using System.Linq;
using System.Reflection;

using Bitub.Xbim.Ifc.Transform;

using Bitub.Dto;

using Xbim.IO;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Common.Geometry;

using NUnit.Framework;

using Microsoft.Extensions.Logging;
using Xbim.Common.Configuration;

namespace Bitub.Xbim.Ifc.Tests
{
    public abstract class TestBase<T>
    {
        protected double Precision { get; } = 1e-5;

        protected ILoggerFactory LoggerFactory { get; } = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        }));

        protected ILogger Logger { get; private set; }

        protected TestBase()
        {
            Logger = LoggerFactory.CreateLogger<T>();
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
                    Assert.IsTrue(a.Axis.ToXbimVector3D().IsEqual(new XbimVector3D(1, 0, 0), Precision), "RefDirection fails");
                if (a.Location is IIfcCartesianPoint p)
                {
                    Assert.IsTrue(p?.ToXbimVector3D().IsEqual(XbimVector3D.Zero, Precision), "Location fails");
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

        protected IModel ReadIfc2x3Model(string resourceName)
        {
            return IfcStore.Open(
                ReadEmbeddedFileStream(resourceName), StorageType.Ifc, XbimSchemaVersion.Ifc2X3, XbimModelType.MemoryModel);
        }

        protected IModel ReadIfc4Model(string resourceName)
        {
            return IfcStore.Open(
                ReadEmbeddedFileStream(resourceName), StorageType.Ifc, XbimSchemaVersion.Ifc4, XbimModelType.MemoryModel);
        }

        protected Stream ReadEmbeddedFileStream(string resourceName)
        {
            return Assembly.GetAssembly(typeof(T))?.GetManifestResourceStream(resourceName);
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
}
