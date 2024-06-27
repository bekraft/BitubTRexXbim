using System.Threading.Tasks;

using Xbim.Common;

using Bitub.Dto;

namespace Bitub.Xbim.Ifc.Import
{
    public interface IModelImporter<TSource>
    {
        Task<IModel> RunImport(TSource someSource, CancelableProgressing monitor);
    }
}
