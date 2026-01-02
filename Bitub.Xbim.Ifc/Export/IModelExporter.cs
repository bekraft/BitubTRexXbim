using System.Threading.Tasks;

using Bitub.Dto;

using Xbim.Common;

namespace Bitub.Xbim.Ifc.Export
{
    /// <summary>
    /// A generic exporter interface using an Xbim model and progress monitor.
    /// </summary>
    public interface IModelExporter<TResult>
    {
        Task<TResult> RunExport(IModel ifcModel, CancelableProgressing monitor);
    }
}
