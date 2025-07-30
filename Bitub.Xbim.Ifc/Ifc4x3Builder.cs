using Microsoft.Extensions.Logging;
using Xbim.Common;

using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4x3.Kernel;

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
        return project;
    }
}