﻿using System.Threading.Tasks;

using Bitub.Dto;

using Xbim.Common;

namespace Bitub.Xbim.Ifc.Export
{
    public interface IExporter<TResult>
    {
        Task<TResult> RunExport(IModel ifcModel, CancelableProgressing monitor);
    }
}
