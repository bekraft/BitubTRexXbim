using System;
using System.Collections.Generic;

using Bitub.Dto.Scene;
using Bitub.Dto.Spatial;

using Xbim.Common;
using Xbim.Common.Geometry;

namespace Bitub.Xbim.Ifc.Tesselate
{
    public static class SceneXbimExtensions
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

        /// <summary>
        /// Returns all three direction vectors, x, y and z respectively.
        /// </summary>
        /// <param name="m">The matrix 3D.</param>
        /// <returns>Array as enumerable.</returns>
        public static IEnumerable<XbimVector3D> ToDirections(this XbimMatrix3D m)
        {
            return new XbimVector3D[]
            {
                new XbimVector3D(m.M11, m.M21, m.M31),
                new XbimVector3D(m.M12, m.M22, m.M32),
                new XbimVector3D(m.M13, m.M23, m.M33)
            };
        }

        public static XbimMatrix3D ToNormalizedDirections(this XbimMatrix3D m)
        {
            var dirs = (XbimVector3D[])m.ToDirections();
            return ToRotationMatrix(dirs[0].Normalized(), dirs[1].Normalized(), dirs[2].Normalized());
        }

        public static XbimMatrix3D ToRotationMatrix(XbimVector3D rx, XbimVector3D ry, XbimVector3D rz)
        {
            return new XbimMatrix3D(
                rx.X, ry.X, rz.X, 0,
                rx.Y, ry.Y, rz.Y, 0,
                rx.Z, ry.Z, rz.Z, 0,
                0, 0, 0, 1
            );
        }

        /// <summary>
        /// Conversion from <see cref="Rotation"/> to <see cref="XbimMatrix3D"/>.
        /// </summary>
        /// <param name="r">The rotation matrix</param>
        /// <param name="translation">The translation vector</param>
        /// <returns>Xbim Matrix3D</returns>
        public static XbimMatrix3D ToXbimMatrix(this M33 r, XYZ translation)
        {
            return new XbimMatrix3D(
                // Converting to columnwise rotation
                r.Rx.X, r.Ry.X, r.Rz.X, 0,
                r.Rx.Y, r.Ry.Y, r.Rz.Y, 0,
                r.Rx.Z, r.Ry.Z, r.Rz.Z, 0,
                translation.X, translation.Y, translation.Z, 1
            );
        }

        /// <summary>
        /// Conversion from <see cref="Rotation"/> to <see cref="XbimMatrix3D"/>.
        /// </summary>
        /// <param name="r">The rotation matrix</param>
        /// <returns>Xbim Matrix3D with zero offset.</returns>
        public static XbimMatrix3D ToXbimMatrix(this M33 r) => ToXbimMatrix(r, XYZ.Zero);

        /// <summary>
        /// Conversion from <see cref="Dto.Scene.Transform"/> to <see cref="XbimMatrix3D" />. 
        /// In case of an unqualified transform (type None), Identity will be returned.
        /// </summary>
        /// <param name="transform">The transform</param>
        /// <returns>Xbim Matrix3D</returns>
        public static XbimMatrix3D ToXbimMatrix(this Dto.Scene.Transform transform)
        {
            switch (transform.RotationOrQuaternionCase)
            {
                case Dto.Scene.Transform.RotationOrQuaternionOneofCase.R:
                    return transform.R.ToXbimMatrix(transform.T);
                case Dto.Scene.Transform.RotationOrQuaternionOneofCase.Q:
                    var m33 = transform.Q.ToM33();
                    return m33.ToXbimMatrix(transform.T);                    
                case Dto.Scene.Transform.RotationOrQuaternionOneofCase.None:
                    return XbimMatrix3D.Identity;
                default:
                    throw new ArgumentException($"Type {transform.RotationOrQuaternionCase} not available");
            }
        }
        #endregion
    }
}
