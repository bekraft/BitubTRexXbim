using System;
using Xbim.Ifc4.Interfaces;

namespace Bitub.Xbim.Ifc.Map;

/// <summary>
/// Scaled SI unit record.
/// </summary>
/// <param name="UnitName">The unit name as IFC standard.</param>
/// <param name="Prefix">The prefix (default null)</param>
/// <param name="Label">The label</param>
public record IfcScaleUnit(
    IfcSIUnitName UnitName,
    string Label,
    IfcSIPrefix? Prefix)
{
    /// <summary>
    /// Scale of this unit identifier to the base unit identifier.
    /// </summary>
    public double ScaleToUnit
    {
        get
        {
            switch (Prefix)
            {
                case null: return 1.0;
                case IfcSIPrefix.EXA: return 1e18;
                case IfcSIPrefix.PETA: return 1e15;
                case IfcSIPrefix.TERA: return 1e12;
                case IfcSIPrefix.GIGA: return 1e9;
                case IfcSIPrefix.MEGA: return 1e6;
                case IfcSIPrefix.KILO: return 1e3;
                case IfcSIPrefix.HECTO: return 1e2;
                case IfcSIPrefix.DECA: return 10;
                case IfcSIPrefix.DECI: return 1e-1;
                case IfcSIPrefix.CENTI: return 1e-2;
                case IfcSIPrefix.MILLI: return 1e-3;
                case IfcSIPrefix.MICRO: return 1e-6;
                case IfcSIPrefix.NANO: return 1e-9;
                case IfcSIPrefix.PICO: return 1e-12;
                case IfcSIPrefix.FEMTO: return 1e-15;
                case IfcSIPrefix.ATTO: return 1e-18;
                default:
                    throw new NotImplementedException($"Unknown prefix {Prefix}");
            }
        }
    }
    
    /// <summary>
    /// Returns the scale of this unit referring to the given unit such that a value of this unit times the scale will yield
    /// an equivalent value on behalf of the given unit. 
    /// </summary>
    /// <param name="otherUnit">The target unit to scale to</param>
    /// <returns>A scale of this unit</returns>
    /// <exception cref="ArgumentException">If the unit names differ.</exception>
    public double ScaleTo(IfcScaleUnit otherUnit)
    {
        if (otherUnit.UnitName != UnitName)
            throw new ArgumentException($"Unit name mismatch. Must be '{UnitName}'.");
        return ScaleToUnit / otherUnit.ScaleToUnit;
    }
}