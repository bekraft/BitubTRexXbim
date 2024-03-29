﻿using Bitub.Dto.Scene;
using Bitub.Dto.Spatial;

using Xbim.Common;
using Xbim.Common.Geometry;

namespace Bitub.Xbim.Ifc.Export
{
    public static class ComponentModelExtensions
    {
        #region XYZ context

        /// <summary>
        /// Converts a serialized <see cref="XYZ"/> to an <see cref="XbimVector3D"/> of meter scale.
        /// </summary>
        /// <param name="xyz">The vector of meter scale</param>
        /// <param name="modelFactors">The model conversion factors</param>
        /// <returns></returns>
        public static XbimVector3D ToXbimVector3DMeter(this XYZ xyz, IModelFactors modelFactors)
        {
            return ToXbimVector3D(xyz, modelFactors.OneMeter);
        }

        /// <summary>
        /// Simple converstion from <see cref="XYZ"/> to <see cref="XbimVector3D"/>.
        /// </summary>
        /// <param name="xyz">The vector</param>
        /// <param name="scale">An optional scale (1.0 by default)</param>
        /// <returns></returns>
        public static XbimVector3D ToXbimVector3D(this XYZ xyz, double scale = 1.0)
        {
            return new XbimVector3D(xyz.X * scale, xyz.Y * scale, xyz.Z * scale);
        }

        /// <summary>
        /// Simple converstion from <see cref="XYZ"/> to <see cref="XbimVector3D"/>.
        /// </summary>
        /// <param name="xyz">The vector</param>
        /// <param name="scale">An optional scale per coordinate</param>
        /// <returns></returns>
        public static XbimVector3D ToXbimVector3D(this XYZ xyz, XbimVector3D scale)
        {
            return new XbimVector3D(xyz.X * scale.X, xyz.Y * scale.Y, xyz.Z * scale.Z);
        }

        /// <summary>
        /// Simple converstion from <see cref="XYZ"/> to <see cref="XbimPoint3D"/>.
        /// </summary>
        /// <param name="xyz">The vector</param>
        /// <param name="scale">An optional scale (1.0 by default)</param>
        /// <returns></returns>
        public static XbimPoint3D ToXbimPoint3D(this XYZ xyz, double scale = 1.0)
        {
            return new XbimPoint3D(xyz.X * scale, xyz.Y * scale, xyz.Z * scale);
        }

        /// <summary>
        /// Simple converstion from <see cref="XYZ"/> to <see cref="XbimPoint3D"/>.
        /// </summary>
        /// <param name="xyz">The vector</param>
        /// <param name="scale">An optional scale per coordinate</param>
        /// <returns></returns>
        public static XbimPoint3D ToXbimPoint3D(this XYZ xyz, XbimVector3D scale)
        {
            return new XbimPoint3D(xyz.X * scale.X, xyz.Y * scale.Y, xyz.Z * scale.Z);
        }

        #endregion

        #region XbimMatrix3D context

        public static XbimMatrix3D ToXbimMatrix(this Rotation r)
        {
            return new XbimMatrix3D(
                // Converting to columnwise rotation
                r.Rx.X, r.Ry.X, r.Rz.X, 0,
                r.Rx.Y, r.Ry.Y, r.Rz.Y, 0,
                r.Rx.Z, r.Ry.Z, r.Rz.Z, 0,
                0, 0, 0, 1
            );
        }

        #endregion
    }
}
