using Microsoft.Extensions.Logging;
using Xbim.Common;

using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4x3.ActorResource;
using Xbim.Ifc4x3.Kernel;
using Xbim.Ifc4x3.UtilityResource;
using IfcChangeActionEnum = Xbim.Ifc4.Interfaces.IfcChangeActionEnum;

namespace Bitub.Xbim.Ifc;

public sealed class Ifc4x3Builder : IfcBuilder
{
    internal Ifc4x3Builder(IModel model, ILoggerFactory? loggerFactory = null) 
        : base(model, loggerFactory)
    { }

    protected override IIfcProject InitNewProject(string projectName)
    {
        IfcProject project = Model.Instances.New<IfcProject>();
        project.Name = projectName;
        //ChangeOrNewLengthUnit(IfcSIUnitName.METRE);
        //if (null == project.ModelContext)
        //    project.RepresentationContexts.Add(Model.NewIfc4GeometricContext("Body", "Model"));
        return project;
    }

    protected override IIfcOwnerHistory NewOwnerHistoryEntry(string comment)
    {
        var newVersion = Model.NewIfcOwnerHistoryEntry<IfcOwnerHistory>(comment, 
            OwningUser as IfcPersonAndOrganization, 
            OwningApplication as IfcApplication, 
            IfcChangeActionEnum.ADDED);            
        return newVersion;
    }
}