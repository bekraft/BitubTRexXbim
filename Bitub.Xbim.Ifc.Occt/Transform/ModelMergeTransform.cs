using System;
using System.Collections.Generic;

using Xbim.Common;
using Xbim.Common.Geometry;

using Xbim.Ifc4.Interfaces;

using Bitub.Dto;

using Xbim.ModelGeometry.Scene;
using Xbim.Common.Configuration;

using Microsoft.Extensions.Logging;

namespace Bitub.Xbim.Ifc.Transform
{
    public sealed class ModelMergeTransformPackage : TransformPackage
    {
        #region Privates members

        private readonly IDictionary<IModel, XbimPlacementTree> _placements = new Dictionary<IModel, XbimPlacementTree>();
        private readonly IDictionary<XbimInstanceHandle, XbimMatrix3D> _tInverted = new Dictionary<XbimInstanceHandle, XbimMatrix3D>();
        private IXbimManagedGeometryEngine _geometryEngine;

        #endregion

        internal ModelMergeTransformPackage(IModel source, IModel target, CancelableProgressing progressMonitor) 
            : base (source, target, progressMonitor)
        {
        }
        
        private IXbimManagedGeometryEngine Engine
        {
            get 
            {      
                if (null == _geometryEngine)
                {
                    _geometryEngine = XbimServices.Current.CreateGeometryManagedEngine();
                }

                return _geometryEngine; 
            }
        }

        private XbimMatrix3D PlacementOf(IIfcProduct p)
        {
            XbimPlacementTree tree;
            if (!_placements.TryGetValue(p.Model, out tree))
            {
                tree = new XbimPlacementTree(p.Model, _geometryEngine, false);
                _placements.Add(p.Model, tree);
            }
            return XbimPlacementTree.GetTransform(p, tree, Engine);
        }

        internal XbimMatrix3D NewPlacementRelative(IIfcProduct container, IIfcProduct newRelativeProduct)
        {
            XbimMatrix3D t;
            var handle = new XbimInstanceHandle(container);
            if (_tInverted.TryGetValue(handle, out t))
            {
                // Compute inv of container local placement
                t = PlacementOf(container);
                t.Invert();
                _tInverted.Add(handle, t);
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