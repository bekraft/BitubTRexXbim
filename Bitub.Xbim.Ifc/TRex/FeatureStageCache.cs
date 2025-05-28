using System;
using System.Collections.Generic;
using System.Linq;

using Bitub.Dto;
using Bitub.Dto.Concept;

namespace Bitub.Xbim.Ifc.TRex
{
    public class FeatureStageCache
    {
        #region Internals
        private Dictionary<Qualifier, SortedList<int, Feature>> _cache;
        private int _stage;
        #endregion

        public FeatureStageCache(QualifierCaseEqualityComparer comparer) 
        {
            _cache = new Dictionary<Qualifier, SortedList<int, Feature>>(comparer);
        }

        public FeatureStageCache() : this(new QualifierCaseEqualityComparer(StringComparison.OrdinalIgnoreCase))
        { }

        public int Stage 
        {
            get => _stage;
            set => _stage = value;
        }

        public IEnumerable<Feature> GetAllByDepth(Qualifier qualifier, FeatureStageStrategy strategy, FeatureStageRange stageRange)
        {
            var features = _cache[qualifier]?.Where(f => f.Key <= _stage);
            if (null == features)
                yield break;

            switch (strategy)
            {
                case FeatureStageStrategy.FirstOf:
                    var first = features.FirstOrDefault(f => f.Key >= stageRange.lower);
                    if (null != first.Value)
                        yield return first.Value;
                    break;
                case FeatureStageStrategy.LastOf:
                    var last = features.LastOrDefault(f => f.Key <= stageRange.upper);
                    if (null != last.Value)
                        yield return last.Value;
                    break;
                case FeatureStageStrategy.AllOf:
                    foreach (var f in features.Where(f => stageRange.IsInRange(f.Key)))
                        yield return f.Value;
                    break;
                default:
                    throw new NotImplementedException($"Missing '{strategy}'");
            }
        }

        public FeatureStage AddFeatureStage(FeatureStage featureStage)
        {
            return AddFeatureStage(featureStage._stage, featureStage._feature);
        }

        public FeatureStage AddFeatureStage(int stage, Feature feature)
        {
            SortedList<int, Feature> features;
            if (!_cache.TryGetValue(feature.Name, out features))
                _cache.Add(feature.Name, features = new SortedList<int, Feature>());

            var formerFeature = features[stage];
            features[stage] = feature;
            return null != formerFeature ? new FeatureStage(stage, formerFeature) : null;
        }

        public void DropAllAboveStage(int stage)
        {
            foreach (var fKeyValue in _cache)
            {
                foreach (var stageKey in fKeyValue.Value.Keys.Where(k => k > stage).ToArray())
                    fKeyValue.Value.Remove(stageKey);
            }    
        }
    }
}
