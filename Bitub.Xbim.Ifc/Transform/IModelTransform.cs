using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Xbim.IO;
using Xbim.Ifc;

using Xbim.Common;

using Bitub.Dto;

using Microsoft.Extensions.Logging;

namespace Bitub.Xbim.Ifc.Transform;

public sealed class TransformResult : TransformPackage
{
    public enum Code
    {
        Finished, 
        Canceled, 
        ExitWithError,
        NotSupported
    }

    public Code ResultCode
    {
        get;
        internal set;
    }

    public string? ResultMessage 
    { 
        get; 
        internal set; 
    }

    public Exception? Cause 
    { 
        get; 
        internal set;         
    }

    internal TransformResult(Code r, TransformPackage package, CancelableProgressing? progressMonitor) 
        : base(package, progressMonitor)
    {
        ResultCode = r;
    }

    internal TransformResult(Code r, TransformPackage package, Exception? exception = null) 
        : base(package, null)
    {
        ResultCode = r;
        Cause = exception;
    }
}

/// <summary>
/// Fundamental transformation request.
/// </summary>
public interface IModelTransform
{
    /// <summary>
    /// The associated logger instance.
    /// </summary>
    ILogger? Log { get; }

    /// <summary>
    /// A (unique) name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The preferred storage type. If not set, source storage type will be used.
    /// </summary>
    public XbimStoreType? TargetStoreType { get; set; }

    /// <summary>
    /// The editor credentials of change. If null, Xbim will use internal user identification.
    /// </summary>
    public XbimEditorCredentials? EditorCredentials { get; set; }

    /// <summary>
    /// Actions to be logged by transformation process.
    /// </summary>
    public ISet<TransformActionResult> LogFilter { get; }

    /// <summary>
    /// Runs the transformation request.
    /// </summary>
    /// <param name="aSource">The model</param>
    /// <param name="cancelableProgressing">An optional progress emitter</param>
    /// <returns></returns>
    Task<TransformResult> Run(IModel aSource, CancelableProgressing cancelableProgressing);
}
