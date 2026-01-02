using Bitub.Dto.Spatial;
using Xbim.Ifc4.Interfaces;

namespace Bitub.Xbim.Ifc.TRex;

public static class TRexExtensions
{
    public static XYZ ToXYZ(this IIfcCartesianPoint point)
    {
        return new XYZ(point.X, point.Y, point.Z);
    }
}