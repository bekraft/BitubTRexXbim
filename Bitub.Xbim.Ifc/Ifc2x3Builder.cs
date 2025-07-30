using Xbim.Common;

using Xbim.Ifc4.Interfaces;

using Microsoft.Extensions.Logging;

using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.MeasureResource;

using IfcSIUnitName = Xbim.Ifc2x3.MeasureResource.IfcSIUnitName;
using IfcSIPrefix = Xbim.Ifc2x3.MeasureResource.IfcSIPrefix;
using IfcUnitEnum = Xbim.Ifc2x3.MeasureResource.IfcUnitEnum;

using System.Linq;

namespace Bitub.Xbim.Ifc;

/// <summary>
/// IFC 2x3 builder implementation.
/// </summary>
public sealed class Ifc2x3Builder : IfcBuilder
{
    internal Ifc2x3Builder(IModel model, ILoggerFactory? loggerFactory = null)
        : base(model, loggerFactory)
    { }

    protected override IIfcProject InitNewProject(string projectName)
    {
        IfcProject project = Model.Instances.New<IfcProject>();
        project.Name = projectName;
        ChangeOrNewLengthUnit(IfcSIUnitName.METRE);
        if (null == project.ModelContext)
            project.RepresentationContexts.Add(Model.NewIfc2x3GeometricContext("Body", "Model"));
        return project;
    }
    
    private IfcSIUnit ChangeOrNewLengthUnit(IfcSIUnitName name, IfcSIPrefix? prefix = null)
    {
        var project = Model.Instances.OfType<IfcProject>().First();
        var assigment = project.UnitsInContext;
        if (null == assigment)
            assigment = Model.NewIfc2x3UnitAssignment(IfcUnitEnum.LENGTHUNIT, name, prefix);

        // Test for existing
        var unit = assigment.Units.Where(u => (u as IfcSIUnit)?.UnitType == IfcUnitEnum.LENGTHUNIT).FirstOrDefault() as IfcSIUnit;
        if (null == unit)
        {
            unit = Model.Instances.New<IfcSIUnit>(u => { u.UnitType = IfcUnitEnum.LENGTHUNIT; });
            assigment.Units.Add(unit);
        }

        unit.Name = name;
        unit.Prefix = prefix;
        return unit;
    }
}
