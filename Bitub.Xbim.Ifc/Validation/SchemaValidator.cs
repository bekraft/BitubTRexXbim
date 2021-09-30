﻿using System;
using System.Collections.Generic;
using System.Linq;

using Xbim.Common;
using Xbim.Common.Enumerations;
using Xbim.Common.ExpressValidation;
using Xbim.Common.Step21;

namespace Bitub.Xbim.Ifc.Validation
{
    /// <summary>
    /// IFC model validation "stamp".
    /// </summary>
    public class SchemaValidator
    {
        /// <summary>
        /// Create a new scheme validator of given sequence of instances.
        /// </summary>
        /// <param name="instances"></param>
        /// <param name="validationFlags"></param>
        /// <returns>A new scheme validator</returns>
        public static SchemaValidator OfInstances(IEnumerable<IPersistEntity> instances, ValidationFlags validationFlags = ValidationFlags.All)
        {
            var validator = new Validator()
            {
                CreateEntityHierarchy = true,
                ValidateLevel = validationFlags
            };

            return new SchemaValidator
            {
                SchemaResultLookup = instances
                    .SelectMany(instance => validator.Validate(instance).Select(result => (instance.Model.SchemaVersion, result)))
                    .ToLookup(g => g.SchemaVersion, g => g.result)
            };
        }

        /// <summary>
        /// According schema.
        /// </summary>
        public IEnumerable<XbimSchemaVersion> SchemaVersion { get => SchemaResultLookup.Select(g => g.Key); }

        /// <summary>
        /// Validation results by schemata.
        /// </summary>
        public ILookup<XbimSchemaVersion, ValidationResult> SchemaResultLookup { get; private set; }

        /// <summary>
        /// Current results of this stamp.
        /// </summary>
        public IEnumerable<ValidationResult> Results { get => SchemaResultLookup.SelectMany(g => g);  }

        /// <summary>
        /// Results per persitent entity.
        /// </summary>
        public ILookup<XbimInstanceHandle, ValidationResult> InstanceResults 
        { 
            get => Results.Where(r => r.Item is IPersistEntity).ToLookup(r => new XbimInstanceHandle(r.Item as IPersistEntity)); 
        }

        // A schema mandatory proposition failure
        private bool IsComplianceFailure(ValidationResult r)
        {
            return ValidationFlags.None != (r.IssueType & (ValidationFlags.Properties | ValidationFlags.Inverses));
        }

        // A WHERE clause failure
        private bool IsConstraintFailure(ValidationResult r)
        {
            return ValidationFlags.None != (r.IssueType & (ValidationFlags.EntityWhereClauses | ValidationFlags.TypeWhereClauses));
        }

        /// <summary>
        /// Whether the results indicate no conflicts with constraint rules (WHERE clauses) of the referenced schema.
        /// </summary>
        public bool IsConstraintToSchema { get => !Unfold().Any(IsConstraintFailure); }

        /// <summary>
        /// Whether the results indicate no schema conflict in missing references and properties.
        /// </summary>
        public bool IsCompliantToSchema { get => !Unfold().Any(IsComplianceFailure); }

        /// <summary>
        /// Flattens all validations results.
        /// </summary>
        /// <returns>An unfold flat hierarchy of results in topological order (children fellow parents)</returns>
        public IEnumerable<ValidationResult> Unfold(ValidationFlags filter = ValidationFlags.All)
        {
            var stack = new Stack<ValidationResult>(Results ?? Enumerable.Empty<ValidationResult>());
            while (stack.Count > 0)
            {
                var result = stack.Pop();
                foreach (var child in result.Details.Where(d => filter.HasFlag(d.IssueType)))
                    stack.Push(child);

                yield return result;
            }
        }

        /// <summary>
        /// Returns all compliance failures by schema version.
        /// </summary>
        public ILookup<XbimSchemaVersion, ValidationResult> SchemaComplianceFailures
        {
            get => SchemaResultLookup
                .SelectMany(g => g.Where(IsComplianceFailure).Select(result => (g.Key, result)))
                .ToLookup(g => g.Key, g => g.result);
        }

        /// <summary>
        /// Returns all constraint failures by schema version.
        /// </summary>
        public ILookup<XbimSchemaVersion, ValidationResult> SchemaConstraintFailures
        {
            get => SchemaResultLookup
                .SelectMany(g => g.Where(IsConstraintFailure).Select(result => (g.Key, result)))
                .ToLookup(g => g.Key, g => g.result);
        }
    }
}
