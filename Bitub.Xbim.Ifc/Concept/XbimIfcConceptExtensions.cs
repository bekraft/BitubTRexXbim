using System;
using System.Collections.Generic;
using System.Linq;

using Xbim.Common;
using Xbim.Common.Step21;

using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.UtilityResource;


using Google.Protobuf.WellKnownTypes;

using Bitub.Dto;
using Bitub.Dto.Concept;

namespace Bitub.Xbim.Ifc.Concept
{
    public static class XbimIfcConceptExtensions
    {

        /// <summary>
        /// Predefined role qualifier "hasIfcType".
        /// </summary>
        public static Qualifier FeatureHasIfcType 
        { 
            get => "hasIfcType".ToQualifier(); 
        }

        /// <summary>
        /// Predefined feature "name".
        /// </summary>
        public static Qualifier FeatureName
        {
            get => "name".ToQualifier();
        }

        /// <summary>
        /// Predefined feature "globallyUniqueId"
        /// </summary>
        public static Qualifier FeatureGloballyUniqueId
        {
            get => "globallyUniqueId".ToQualifier();
        }

        #region FeatureData conversion

        public static FeatureData ToFeatureData(this IfcGloballyUniqueId guid)
        {
            return new FeatureData { Type = DataType.Guid, Guid = guid.ToGlobalUniqueId() };
        }

        public static FeatureData ToFeatureData(this global::Xbim.Ifc2x3.UtilityResource.IfcGloballyUniqueId guid)
        {
            return new FeatureData { Type = DataType.Guid, Guid = guid.ToGlobalUniqueId() };
        }

        public static FeatureData ToFeatureData(this IIfcValue p, DataOp dataOp = DataOp.Equals)
        {
            return ToFeatureData(p as IExpressValueType, dataOp);
        }

        public static FeatureData ToFeatureData(this IExpressValueType p, DataOp dataOp = DataOp.Equals)
        {
            if (p is IExpressIntegerType ifcInteger)
                return new FeatureData { Type = DataType.Integer, Digit = ifcInteger.Value, Op = dataOp };
            else if (p is IExpressStringType ifcString)
                return new FeatureData { Type = DataType.Label, Value = ifcString.Value ?? "", Op = dataOp };
            else if (p is IExpressRealType ifcReal)
                return new FeatureData { Type = DataType.Decimal, Digit = ifcReal.Value, Op = dataOp };
            else if (p is IExpressNumberType ifcNumber)
                return new FeatureData { Type = DataType.Decimal, Digit = ifcNumber.Value, Op = dataOp };
            else if (p is IExpressLogicalType ifcLogical)
                return new FeatureData { Type = DataType.Logical, Logical = ifcLogical.Value.ToLogical(), Op = dataOp };
            else if (p is IExpressBooleanType ifcBoolean)
                return new FeatureData { Type = DataType.Boolean, Logical = new Logical { Known = ifcBoolean.Value }, Op = dataOp };
            else if (p is global::Xbim.Ifc4.MeasureResource.IfcIdentifier ifcIdentifier4)
                return new FeatureData { Type = DataType.Id, Value = (ifcIdentifier4 as IExpressStringType)?.Value };
            else if (p is global::Xbim.Ifc2x3.MeasureResource.IfcIdentifier ifcIdentifier2x3)
                return new FeatureData { Type = DataType.Id, Value = (ifcIdentifier2x3 as IExpressStringType)?.Value };
            else if (p is global::Xbim.Ifc4.DateTimeResource.IfcDateTime ifcDateTime4)
                return new FeatureData { Type = DataType.Timestamp, TimeStamp = Timestamp.FromDateTime(ifcDateTime4.ToDateTime()) };
            else if (p is global::Xbim.Ifc4.DateTimeResource.IfcTimeStamp ifcTimeStamp4)
                return new FeatureData { Type = DataType.Timestamp, TimeStamp = Timestamp.FromDateTime(ifcTimeStamp4.ToDateTime()) };
            else if (p is global::Xbim.Ifc2x3.MeasureResource.IfcTimeStamp ifcTimeStamp2x3)
                return new FeatureData { Type = DataType.Timestamp, TimeStamp = Timestamp.FromDateTime(global::Xbim.Ifc2x3.MeasureResource.IfcTimeStamp.ToDateTime(ifcTimeStamp2x3)) };
            throw new NotImplementedException($"Missing cast of '{p.GetType()}'");
        }

        /// <summary>
        /// Converts each property into one or more <see cref="FeatureData"/> instances.
        /// </summary>
        /// <param name="p">The IFC property.</param>
        /// <returns>Data concepts</returns>
        public static IEnumerable<FeatureData> ToFeatureData(this IIfcSimpleProperty p)
        {
            if (p is IIfcPropertySingleValue psv)
            {
                yield return psv.NominalValue.ToFeatureData();
            }
            else if (p is IIfcPropertyBoundedValue pbv)
            {
                if (null != pbv.UpperBoundValue)
                    yield return pbv.UpperBoundValue.ToFeatureData(DataOp.LessThanEquals);
                if (null != pbv.LowerBoundValue)
                    yield return pbv.LowerBoundValue.ToFeatureData(DataOp.GreaterThanEquals);
                if (null != pbv.SetPointValue)
                    yield return pbv.SetPointValue.ToFeatureData(DataOp.Equals);
            }
            else if (p is IIfcPropertyEnumeratedValue pev)
            {
                foreach (var dataConcept in pev.EnumerationValues.Select(v => v.ToFeatureData(DataOp.Equals)))
                    yield return dataConcept;
            }
            else if (p is IIfcPropertyListValue plv)
            {
                foreach (var dataConcept in plv.ListValues.Select(v => v.ToFeatureData(DataOp.Equals)))
                    yield return dataConcept;
            }
            else
            {
                throw new NotImplementedException($"Not yet implemented: {p.ExpressType.Name}");
            }
        }

        #endregion

        #region Specific conversions

        public static IEnumerable<Feature> ToIfcTypeFeature(this IIfcObject o)
        {
            yield return new Feature
            {
                Name = FeatureHasIfcType,
                Role = new FeatureRole { Qualifier = o.ToImplementingClassQualifier() }
            };
        }

        public static IEnumerable<Feature> ToIfcObjectNameFeature(this IIfcObject o)
        {
            yield return new Feature { Name = FeatureName, Data = ToFeatureData(o.Name) };
        }

        public static IEnumerable<Feature> ToIfcGuidFeature(this IIfcObject o)
        {
            yield return new Feature { Name = FeatureGloballyUniqueId, Data = ToFeatureData(o.GlobalId) };
        }

        #endregion

        #region FeatureConcept conversion

        /// <summary>
        /// Wraps an IFC generic (root) object into a qualifier based on its name and its GUID.
        /// </summary>
        /// <param name="o">The object</param>
        /// <returns>A qualifier for use as concept reference</returns>
        public static Qualifier ToCanonical(this IIfcRoot o)
        {
            return new string[] { o.Name ?? "Anonymous", o.GlobalId.ToString() }.ToQualifier();
        }

        public static IEnumerable<Feature> ToFeatures<T>(this IIfcObject o, CanonicalFilter filter = null) where T : IIfcSimpleProperty
        {
            // Pass by default, ignore match results
            return o.PropertySets<IIfcPropertySetDefinition>()
                .SelectMany(set => set.ToFeatures<T>())
                .Where(f => filter?.IsPassedBy(f.Name, out _) ?? true);
        }

        public static IEnumerable<Feature> ToFeatures<T>(this IIfcPropertySetDefinition set) where T : IIfcSimpleProperty
        {
            return set.Properties<T>().SelectMany(p => p.ToFeatures(set.Name.ToString().ToQualifier()));
        }

        public static IEnumerable<Feature> ToFeatures(this IIfcSimpleProperty p, Qualifier superCanonical)
        {
            var canonical = superCanonical.Append(p.Name.ToString());
            return p.ToFeatureData().Select(dataConcept => new Feature { Name = canonical, Data = dataConcept });
        }      

        /// <summary>
        /// Converts the whole object into an concept assertion wrapped by an concrete object concept.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static IEnumerable<ELConcept> ToConceptAssertion(this IIfcObject p)
        {
            var productConcept = new ELConcept 
            { 
                Canonical = p.ToCanonical(),                
            };

            productConcept.Feature.Add(p.ToIfcTypeFeature());
            productConcept.Feature.Add(p.ToIfcObjectNameFeature());
            productConcept.Feature.Add(p.ToIfcGuidFeature());
            productConcept.Feature.AddRange(p.PropertySets<IIfcPropertySetDefinition>().SelectMany(pset => pset.ToFeatures<IIfcSimpleProperty>()));

            yield return productConcept;
        }

        #endregion

        #region IFC to concept domain context

        /// <summary>
        /// Will convert a given upper bound generic type of given IFC schema version into an enumerable of EL concepts.
        /// </summary>
        /// <typeparam name="T">The upper bound type.</typeparam>
        /// <param name="schemaVersion">The IFC schema version</param>
        /// <returns>An enumeration of concepts in order of reference</returns>
        public static IEnumerable<ELConcept> ToConcepts<T>(this XbimSchemaVersion schemaVersion) where T : IPersistEntity
        {
            var conceptCache = new Dictionary<Qualifier, ELConcept>();
            foreach (var classifier in schemaVersion.ToImplementingClassifiers<T>())
            {
                for (int k = 0; k < classifier.Path.Count - 1; ++k)
                {
                    ELConcept concept;
                    if (!conceptCache.TryGetValue(classifier.Path[k], out concept))
                    {
                        concept = new ELConcept
                        {
                            Canonical = classifier.Path[k]
                        };          
                        conceptCache.Add(concept.Canonical, concept);

                        if (k > 0)
                            concept.Subsumes.Add(classifier.Path[k - 1]);

                        yield return concept;
                    }
                }
            }
        }

        /// <summary>
        /// Wraps the given IFC schema version into a EL domain description.
        /// </summary>
        /// <param name="schemaVersion">The IFC schema version</param>
        /// <returns>A domain</returns>
        public static ELDomain ToDomain(this XbimSchemaVersion schemaVersion)
        {
            var ifcDomain = new ELDomain
            {
                Canonical = schemaVersion.ToString().ToQualifier()
            };
            return ifcDomain;
        }

        #endregion
    }
}
