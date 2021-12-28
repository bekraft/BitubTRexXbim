using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Collections.Generic;

using Xbim.Common;
using Xbim.Common.Metadata;

using Bitub.Dto;
using System.Reflection;

namespace Bitub.Xbim.Ifc.Transform
{
    /// <summary>
    /// Model filter strategy used by <see cref="ModelFilterTransform"/>.
    /// </summary>
    [Flags]
    public enum ModelFilterStrategy
    {
        /// <summary>
        /// Pure copy without any relations. If not specified with relations, all matching elements will be aggregated into project scope.
        /// </summary>
        PureCopy = 0x00,
        
        /// <summary>
        /// Transfer IFC decomposition relations with direct or indirect scope to matching elements.
        /// </summary>
        WithIfcRelDecomposes = 0x01,

        /// <summary>
        /// Transfer IFC spatial relations with direct or indirect scope to matching elements.
        /// </summary>
        WithIfcRelContainedInSpatialStructure = 0x02,

        /// <summary>
        /// Transfer IFC property relations with direct or indirect scope to matching elements.
        /// </summary>
        WithIfcRelDefinesByProperties = 0x04,

        /// <summary>
        /// Transfer IFC type relations with direct or indirect scope to matching elements.
        /// </summary>
        WithIfcRelDefinesByType = 0x08,

        /// <summary>
        /// Transfer IFC product representation relations with direct or indirect scope to matching elements.
        /// </summary>
        WithIfcRepresentation = 0x10,

        /// <summary>
        /// Combined flag of all relationship flags.
        /// </summary>
        WithAllIfcRelations = 0x0f,
    }

    public class ModelFilterTransformPackage : TransformPackage
    {
        public int[] ExclusiveEntityLabels { get; private set; } = new int[] { };
        public int[] InclusiveEntityLabels { get; private set; } = new int[] { };

        public short[] ExclusiveTypeIds { get; private set; } = new short[] { };
        public short[] InclusiveTypeIds { get; private set; } = new short[] { };

        public ModelFilterStrategy RelationalStrategy { get; private set; }


        public ModelFilterTransformPackage(IModel source, IModel target, CancelableProgressing progressMonitor,
            ModelFilterStrategy rules = 0) : base(source, target, progressMonitor)
        {
            Array.Sort(ExclusiveEntityLabels);
            RelationalStrategy = rules;
        }

        internal ModelFilterTransformPackage WithInclusiveEntities(IEnumerable<int> entityLabels)
        {
            InclusiveEntityLabels = entityLabels.ToArray();
            Array.Sort(InclusiveEntityLabels);
            return this;
        }

        internal ModelFilterTransformPackage WithExclusiveEntities(IEnumerable<int> entityLabels)
        {
            ExclusiveEntityLabels = entityLabels.ToArray();
            Array.Sort(ExclusiveEntityLabels);
            return this;
        }

        internal ModelFilterTransformPackage WithInclusiveExpressTypes(IEnumerable<ExpressType> expressTypes)
        {
            InclusiveTypeIds = expressTypes.Select(t => t.TypeId).ToArray();
            Array.Sort(InclusiveTypeIds);
            return this;
        }

        internal ModelFilterTransformPackage WithExclusiveExpressTypes(IEnumerable<ExpressType> expressTypes)
        {
            ExclusiveTypeIds = expressTypes.Select(t => t.TypeId).ToArray();
            Array.Sort(ExclusiveTypeIds);
            return this;
        }

        internal bool IsAccepted(IPersistEntity entity)
        {
            return IsAcceptedByLabel(entity) && IsAcceptedByTypeId(entity);
        }

        internal bool IsAcceptedByLabel(IPersistEntity entity)
        {
            var byLabelExcluded = 0 <= Array.BinarySearch(ExclusiveEntityLabels, entity.EntityLabel);
            var byLabelIncluded = (InclusiveEntityLabels.Length == 0) || (0 <= Array.BinarySearch(InclusiveEntityLabels, entity.EntityLabel));

            return !byLabelExcluded && byLabelIncluded;
        }

        internal bool IsAcceptedByTypeId(IPersistEntity entity)
        {
            var byTypeIdExcluded = 0 <= Array.BinarySearch(ExclusiveTypeIds, entity.ExpressType.TypeId);
            var byTypeIdIncluded = (InclusiveTypeIds.Length == 0) || (0 <= Array.BinarySearch(InclusiveTypeIds, entity.ExpressType.TypeId));

            return !byTypeIdExcluded && byTypeIdIncluded;
        }

        internal bool IsFollowReleation(PropertyInfo propertyInfo)
        {
            return false;
        }
    }

    /// <summary>
    /// Filtering request which will restrict the model output to the given explicitely and exclusively given entity labels and/or express types.
    /// Additionally, the relational strategy will embed decomposition, spatial and semantical references.
    /// </summary>
    public class ModelFilterTransform : ModelTransformTemplate<ModelFilterTransformPackage>
    {
        public override string Name => "Model filtering";

        public override ILogger Log { get; protected set; }

        public int[] ExclusiveEntityLabels { get; set; } = new int[] { };
        public int[] InclusiveEntityLabels { get; set; } = new int[] { };

        public ExpressType[] ExclusiveExpressTypes { get; set; } = new ExpressType[] { };
        public ExpressType[] InclusiveExpressTypes { get; set; } = new ExpressType[] { };

        public ModelFilterStrategy RelationalStrategy { get; set; } = ModelFilterStrategy.PureCopy;

        protected override ModelFilterTransformPackage CreateTransformPackage(IModel aSource, IModel aTarget, 
            CancelableProgressing progressMonitor)
        {
            return new ModelFilterTransformPackage(aSource, aTarget, progressMonitor, RelationalStrategy)
                .WithExclusiveEntities(ExclusiveEntityLabels)
                .WithInclusiveEntities(InclusiveEntityLabels)
                .WithExclusiveExpressTypes(ExclusiveExpressTypes)
                .WithInclusiveExpressTypes(InclusiveExpressTypes);
        }

        protected override object PropertyTransform(ExpressMetaProperty property, 
            object hostObject, ModelFilterTransformPackage package)
        {
            return base.PropertyTransform(property, hostObject, package);
        }

        protected override TransformActionType PassInstance(IPersistEntity instance, 
            ModelFilterTransformPackage package)
        {
            if (!package.IsAccepted(instance))
                return TransformActionType.Drop;
            else
                return TransformActionType.Copy;
        }
    }
}
