﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Bitub.Dto;

using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Ifc;
using Xbim.IO;

using Microsoft.Extensions.Logging;

namespace Bitub.Xbim.Ifc.Transform
{
    /// <summary>
    /// A transform request template provided most of the functionality needed.
    /// <para>A typical transformation request will implemented the branching callback <see cref="PassInstance(IPersistEntity, T)"/>
    /// and if needed when using inverse relationships an override of <see cref="PropertyTransform(ExpressMetaProperty, object, T)"/>. More sophisticated processing
    /// is available by overriding <see cref="DelegateCopy(IPersistEntity, T)"/></para>
    /// </summary>
    /// <typeparam name="T">The package type</typeparam>
    public abstract class ModelTransformTemplate<T> : IModelTransform where T : TransformPackage
    {
        public abstract string Name { get; }

        /// <summary>
        /// The preferred storage type. If not set, source storage type will be used.
        /// </summary>
        public XbimStoreType? TargetStoreType { get; set; } 

        /// <summary>
        /// The editor credentials of change. If null, Xbim will use internal user identification.
        /// </summary>
        public XbimEditorCredentials EditorCredentials { get; set; }

        public abstract ILogger Log { get; protected set; }

        /// <summary>
        /// Actions to be logged by transformation process.
        /// </summary>
        public ISet<TransformActionResult> LogFilter { get; } = new HashSet<TransformActionResult>();

        /// <summary>
        /// Transformation action type.
        /// </summary>
        protected enum TransformActionType
        {
            /// <summary>
            /// Drop the current instance (including relationsships).
            /// </summary>
            Drop, 
            /// <summary>
            /// Copy with direct relations.
            /// </summary>
            Copy, 
            /// <summary>
            /// Copy with inverse relations (including other instances depending on the current)
            /// </summary>
            CopyWithInverse, 
            /// <summary>
            /// Handle the copy process by delegation using <see cref="DelegateCopy(IPersistEntity, T)"/>.
            /// </summary>
            Delegate
        }

        protected ModelTransformTemplate(params TransformActionResult[] logActions)
        {
            LogFilter = new HashSet<TransformActionResult>(logActions);
        }

        /// <summary>
        /// Delegate handling the instance's property transformation. By default all properties (relations and data) are
        /// forwarded as they are into the transformation queue.
        /// </summary>
        /// <param name="property">The meta property descriptor</param>
        /// <param name="hostObject">The hosting object</param>
        /// <param name="package">The task's work package</param>
        /// <returns></returns>
        protected virtual object PropertyTransform(ExpressMetaProperty property, 
            object hostObject, T package)
        {
            if (package.ProgressMonitor?.State.IsAboutCancelling ?? false)
            {
                return null;
            }
            else
            {
                var value = property?.PropertyInfo.GetValue(hostObject);
                if (value is IPersistEntity entity)
                {
                    if (PassInstance(entity, package) == TransformActionType.Drop)
                        return null;
                }
                else if (value is IEnumerable items && property.PropertyInfo.IsLowerConstraintRelationType<IPersistEntity>())
                {
                    var entities = (IEnumerable<IPersistEntity>)items;
                    return EmptyToNull(entities.Where(e => PassInstance(e, package) != TransformActionType.Drop));
                }

                return value;
            }
        }

        /// <summary>
        /// Whether to include an instance or not in transformation queue. Any dropped instance won't be injected into
        /// the new model.
        /// </summary>
        /// <param name="instance">An instance.</param>
        /// <param name="package">The task's work package</param>
        /// <returns>True, if to include into the transformation</returns>
        protected abstract TransformActionType PassInstance(IPersistEntity instance, T package);

        protected abstract T CreateTransformPackage(IModel aSource, IModel aTarget, 
            CancelableProgressing progressMonitor);

        protected virtual TransformResult.Code DoPreprocessTransform(T package)
        {
            return TransformResult.Code.Finished;
        }

        protected IEnumerable<E> EmptyToNull<E>(IEnumerable<E> elements)
        {
            if (elements.Any())
                return elements.ToArray();
            else
                return null;
        }

        protected E Copy<E>(E instance, T package, bool withInverse) where E : IPersistEntity
        {
            package.LogAction(new XbimInstanceHandle(instance), TransformActionResult.Copied);
            try
            {
                return package.Target.InsertCopy(instance, package.Map, (p, o) => PropertyTransform(p, o, package), withInverse, false);
            }
            catch(Exception e)
            {
                Log?.LogError("Exception at #{2}{3}: '{0}' with '{1}'.", e.GetType().Name, e.Message, instance.EntityLabel, instance.ExpressType.Name);
                throw e;
            }
        }

        protected virtual IPersistEntity DelegateCopy(IPersistEntity instance, T package)
        {
            return Copy(instance, package, true);
        }

        protected TransformResult.Code DoTransform(T package)
        {
            foreach (var instance in package.Source.Instances)
            {
                if (package.ProgressMonitor?.State.IsAboutCancelling ?? false)
                    return TransformResult.Code.Canceled;

                switch(PassInstance(instance, package))
                {
                    case TransformActionType.Copy:
                        Copy(instance, package, false);
                        break;
                    case TransformActionType.CopyWithInverse:
                        Copy(instance, package, true);
                        break;
                    case TransformActionType.Delegate:
                        DelegateCopy(instance, package);
                        break;
                    case TransformActionType.Drop:
                        package.LogAction(new XbimInstanceHandle(instance), TransformActionResult.Skipped);
                        break;
                }

                package.ProgressMonitor?.NotifyOnProgressChange(1, Name);
            }
            return TransformResult.Code.Finished;
        }

        protected virtual TransformResult.Code DoPostTransform(T package)
        {
            return TransformResult.Code.Finished;
        }

        private global::Xbim.IO.XbimStoreType DetectStorageType(IModel model)
        {
            if (model is IfcStore s)
                model = s.Model;
            if (model is global::Xbim.IO.Memory.MemoryModel)
                return XbimStoreType.InMemoryModel;
            else if (model is global::Xbim.IO.Esent.EsentModel)
                return XbimStoreType.EsentDatabase;
            else
                throw new NotSupportedException($"Unsupported / unknown type {model.GetType()}");
        }

        protected IfcStore CreateTargetModel(IModel sourcePattern, CancelableProgressing progress)
        {
            var storeType = TargetStoreType ?? DetectStorageType(sourcePattern);

            progress?.NotifyOnProgressChange($"Starting '{Name}' with '{storeType}' model implementation ...");
            IfcStore target;
            if (null == EditorCredentials)
                target = IfcStore.Create(sourcePattern.SchemaVersion, storeType);
            else
                target = IfcStore.Create(EditorCredentials, sourcePattern.SchemaVersion, storeType);

            // Set model factors
            target.ModelFactors.Initialise(
                sourcePattern.ModelFactors.AngleToRadiansConversionFactor,
                sourcePattern.ModelFactors.LengthToMetresConversionFactor,
                sourcePattern.ModelFactors.Precision);

            return target;
        }

        protected Func<TransformResult> FastForward(IModel source, CancelableProgressing progressMonitor)
        {
            if (!progressMonitor?.State.IsAlive ?? false)
                throw new NotSupportedException($"Progress monitor already terminated.");

            return () =>
            {
                progressMonitor?.State.MarkTerminated();
                progressMonitor?.NotifyOnProgressEnd($"Running fast forward '{Name}' model copy ...");
                return new TransformResult(TransformResult.Code.Finished, CreateTransformPackage(source, source, progressMonitor), progressMonitor);
            };
        }

        private TransformResult CreateResultFromCode(TransformResult.Code code, CancelableProgressing cancelableProgressing)
        {
            if (cancelableProgressing?.State.IsAboutCancelling ?? false)
                cancelableProgressing.State.MarkCanceled();
            if (cancelableProgressing?.State.HasErrors ?? false)
                cancelableProgressing.State.MarkBroken();
            return new TransformResult(code);
        }

        protected Func<TransformResult> PrepareInternally(IModel aSource, CancelableProgressing progressMonitor)
        {
            if (!progressMonitor?.State.IsAlive ?? false)
                throw new NotSupportedException($"Progress monitor already terminated.");

            return () =>
            {
                List<TransformLogEntry> logEntries = new List<TransformLogEntry>();
                IfcStore target = CreateTargetModel(aSource, progressMonitor);

                using (ITransaction txStore = target.BeginTransaction(Name))
                {
                    try
                    {
                        T package = CreateTransformPackage(aSource, target, progressMonitor);

                        TransformResult.Code code;
                        progressMonitor?.NotifyOnProgressChange($"Preparing '{Name}' ...");
                        if (TransformResult.Code.Finished != (code = DoPreprocessTransform(package)))
                            return CreateResultFromCode(code, progressMonitor);
                        progressMonitor?.NotifyOnProgressChange(0, $"Preparation done.");

                        progressMonitor?.NotifyOnProgressChange($"Running '{Name}' ...");
                        if (TransformResult.Code.Finished != (code = DoTransform(package)))
                            return CreateResultFromCode(code, progressMonitor);
                        progressMonitor?.NotifyOnProgressChange(0, $"Transformation done.");

                        progressMonitor?.NotifyOnProgressChange($"Post processing '{Name}' ...");
                        if (TransformResult.Code.Finished != (code = DoPostTransform(package)))
                            return CreateResultFromCode(code, progressMonitor);
                        progressMonitor?.NotifyOnProgressChange(0, $"Post-processing done.");

                        txStore.Commit();
                        return new TransformResult(code, package, progressMonitor);
                    }
                    catch (Exception e)
                    {
                        progressMonitor?.State.MarkBroken();
                        txStore.RollBack();
                        return new TransformResult(TransformResult.Code.ExitWithError, e);
                    }
                    finally
                    {
                        progressMonitor?.State.MarkTerminated();
                        progressMonitor?.NotifyOnProgressEnd($"Transform '{Name}' has been finalized.");
                    }
                }
            };
        }

        /// <summary>
        /// Creates a new transformation task but doesn't start it.
        /// </summary>
        /// <param name="aSource">The source model</param>
        /// <param name="progressing">The porgressing token</param>
        /// <returns></returns>
        public Task<TransformResult> Prepare(IModel aSource, out CancelableProgressing progressing)
        {
            progressing = new CancelableProgressing(true);
            progressing.NotifyProgressEstimateChange(aSource.Instances.Count);
            return new Task<TransformResult>(PrepareInternally(aSource, progressing));
        }

        /// <summary>
        /// Creates a new transformation task and starts it.
        /// </summary>
        /// <param name="aSource">The source model</param>
        /// <param name="progressReceiver">The progress receiver</param>
        /// <returns></returns>
        public Task<TransformResult> Run(IModel aSource, CancelableProgressing cancelableProgressing)
        {
            cancelableProgressing?.NotifyProgressEstimateChange(aSource.Instances.Count);
            return Task.Run(PrepareInternally(aSource, cancelableProgressing));
        }
    }
}
