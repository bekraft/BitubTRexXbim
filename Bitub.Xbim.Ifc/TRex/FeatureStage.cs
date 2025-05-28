using System;
using System.Collections.Generic;
using Bitub.Dto;
using Bitub.Dto.Concept;

namespace Bitub.Xbim.Ifc.TRex
{
    /// <summary>
    /// A feature concept bound to a stage level in model hierarchy.
    /// </summary>
    public class FeatureStage : IEquatable<FeatureStage>, IComparable<FeatureStage>
    {
        public readonly Feature _feature;
        public readonly int _stage;

        public FeatureStage(int s, Feature f)
        {
            _feature = f;
            _stage = s;
        }

        public Qualifier FeatureName => _feature.Name;

        public int CompareTo(FeatureStage other)
        {
            return Math.Sign(_stage - other._stage);
        }

        public override bool Equals(object obj)
        {
            if (obj is FeatureStage fs)
                return Equals(fs);
            else
                return false;
        }

        public bool Equals(FeatureStage other)
        {
            return _feature.Name.Equals(other?._feature?.Name);
        }

        public override int GetHashCode()
        {
            int hashCode = -1497673742;
            hashCode = hashCode * -1521134295 + EqualityComparer<Feature>.Default.GetHashCode(_feature);
            hashCode = hashCode * -1521134295 + _stage.GetHashCode();
            return hashCode;
        }
    }

}
