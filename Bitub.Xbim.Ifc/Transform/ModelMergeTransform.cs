using System;

using System.Collections.Generic;

using Xbim.Common;
using Xbim.Common.Geometry;

using Xbim.Ifc4.Interfaces;

using Microsoft.Extensions.Logging;
using Bitub.Dto;
using System.Runtime.InteropServices;

using Xbim.ModelGeometry.Scene;
using Xbim.Geometry.Engine.Interop;
using Xbim.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bitub.Xbim.Ifc.Transform
{
    public sealed class ModelMergeTransformPackage : TransformPackage
    {
        private IDictionary<IModel, XbimPlacementTree> placements = new Dictionary<IModel, XbimPlacementTree>();
        private IDictionary<XbimInstanceHandle, XbimMatrix3D> tInverted = new Dictionary<XbimInstanceHandle, XbimMatrix3D>();
        private XbimGeometryEngine geometryEngine;

        public ModelMergeTransformPackage()
        {
            XbimServices.Current.ConfigureServices(opt => opt.AddXbimToolkit(conf => conf.AddGeometryServices()));
        }

        internal XbimGeometryEngine Engine
        {
            get 
            {      
                if (null == geometryEngine)
                {
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        throw new NotSupportedException($"${nameof(ModelMergeTransformPackage)}) requires WinOS platform.");

                    var geometryServices = XbimServices.Current.ServiceProvider.GetRequiredService<IXbimGeometryServicesFactory>();
                    var loggingFactory = XbimServices.Current.ServiceProvider.GetRequiredService<ILoggerFactory>();
                    geometryEngine = new XbimGeometryEngine(geometryServices, loggingFactory);
                }

                return geometryEngine; 
            }
        }

        internal XbimMatrix3D PlacementOf(IIfcProduct p)
        {
            XbimPlacementTree tree;
            if (!placements.TryGetValue(p.Model, out tree))
            {
                tree = new XbimPlacementTree(p.Model, false);
                placements.Add(p.Model, tree);
            }
            return XbimPlacementTree.GetTransform(p, tree, Engine);
        }

        internal XbimMatrix3D NewPlacementRelative(IIfcProduct container, IIfcProduct newRelativeProduct)
        {
            XbimMatrix3D t;
            var handle = new XbimInstanceHandle(container);
            if (tInverted.TryGetValue(handle, out t))
            {
                // Compute inv of container local placement
                t = PlacementOf(container);
                t.Invert();
                tInverted.Add(handle, t);
            }

            // New placement based on given global transformation
            return t * PlacementOf(newRelativeProduct);            
        }
    }

    public class ModelMergeTransform : ModelTransformTemplate<ModelMergeTransformPackage>
    {
        public override string Name => throw new NotImplementedException();

        public override ILogger Log { get => throw new NotImplementedException(); protected set => throw new NotImplementedException(); }

        protected override ModelMergeTransformPackage CreateTransformPackage(IModel aSource, IModel aTarget, CancelableProgressing progressMonitor)
        {
            throw new NotImplementedException();
        }

        protected override TransformActionType PassInstance(IPersistEntity instance, ModelMergeTransformPackage package)
        {
            throw new NotImplementedException();
        }
    }
}