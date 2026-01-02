using System.Collections.Generic;
using System.Linq;
using Bitub.Dto.Scene;
using Xbim.Common.Geometry;

namespace Bitub.Xbim.Ifc.Export
{
    /// <summary>
    /// Aggregates component and remaining shape labels.
    /// </summary>
    public sealed class XbimCompositeShape
    {
        #region Private members
        private List<int> _instanceLabels = new List<int>();
        private List<Shape> _shapeList = new List<Shape>();
        #endregion

        public XbimCompositeShape(IEnumerable<XbimShapeInstance> productShapeInstances)
        {
            _instanceLabels = productShapeInstances.Select(i => i.InstanceLabel).OrderBy(i => i).ToList();
        }

        public bool MarkDone(XbimShapeInstance productShapeInstance)
        {
            var idx = _instanceLabels.BinarySearch(productShapeInstance.InstanceLabel);
            if (0 > idx)
                return false;
            else
                _instanceLabels.RemoveAt(idx);

            return true;
        }

        public bool Add(XbimShapeInstance productShapeInstance, Shape productShape)
        {
            var isHeldAndDone = MarkDone(productShapeInstance);
            _shapeList.Add(productShape);
            return isHeldAndDone;
        }

        public IEnumerable<Shape> Shapes
        {
            get => _shapeList.ToArray();
        }

        public bool IsComplete
        {
            get => _instanceLabels.Count == 0;
        }
    }
}
