using System;
using System.Collections.Generic;

using Xbim.Common;

using Bitub.Dto;

namespace Bitub.Xbim.Ifc.Transform;

/// <summary>
/// Transformation action
/// </summary>
public enum TransformActionResult
{
    /// <summary>
    /// Marks a copied instance of source.
    /// </summary>
    Copied = 0,
    /// <summary>
    /// Marks a skipped instance of source.
    /// </summary>
    Skipped = 1,
    /// <summary>
    /// Marks a partially copied or/and modified instance of source.
    /// </summary>
    Modified = 2,
    /// <summary>
    /// Marks a new instance of target.
    /// </summary>
    Added = 3
}

/// <summary>
/// Performed transformation by affected instance handle and performed action.
/// </summary>
public sealed class TransformLogEntry
{
    public readonly XbimInstanceHandle Handle;
    public readonly TransformActionResult Performed;

    internal TransformLogEntry(XbimInstanceHandle handle, TransformActionResult result)
    {
        Handle = handle;
        Performed = result;
    }
}

/// <summary>
/// Basic transformation package which bundles source and target model via an instance mapping map.
/// </summary>
public class TransformPackage : IDisposable
{
    #region Private fields
    private readonly List<TransformLogEntry> _logEntry = new ();
    #endregion
    
    public IEnumerable<TransformLogEntry> Log => _logEntry.ToArray();

    public readonly XbimInstanceHandleMap Map;
    
    public ISet<TransformActionResult> LogFilter { get; }

    public IModel Target => Map.ToModel;

    public IModel Source => Map.FromModel;

    public CancelableProgressing? ProgressMonitor { get; private set; }

    public bool IsCanceledOrBroken => (null != ProgressMonitor) && (ProgressMonitor.State.IsCanceled || ProgressMonitor.State.IsBroken);

    public bool LogAction(int sourceEntityLabel, TransformActionResult action)
    {
        return LogAction(new XbimInstanceHandle(Source, sourceEntityLabel), action);
    }

    public bool LogAction(XbimInstanceHandle sourceHandle, TransformActionResult action)
    {
        if (!LogFilter.Contains(action))
            return false;

        _logEntry.Add(new TransformLogEntry(sourceHandle, action));
        return true;
    }

    protected TransformPackage(TransformPackage other, CancelableProgressing? progressMonitor)
    {
        LogFilter = new HashSet<TransformActionResult>(other.LogFilter);
        Map = other.Map;
        ProgressMonitor = progressMonitor;
        
        // Private
        _logEntry = new List<TransformLogEntry>(other._logEntry);
    }
    
    protected TransformPackage(IModel aSource, IModel aTarget, CancelableProgressing? progressMonitor, params TransformActionResult[] logFilter)
    {
        Map = new XbimInstanceHandleMap(aSource, aTarget);
        LogFilter = new HashSet<TransformActionResult>(logFilter);
        ProgressMonitor = progressMonitor;
    }

    public void Dispose()
    {
        Map.Clear();
        ProgressMonitor = null;
        
        // Private
        _logEntry.Clear();
    }
}
