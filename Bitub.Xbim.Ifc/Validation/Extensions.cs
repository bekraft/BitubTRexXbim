using System;
using System.Collections.Generic;
using System.Linq;

using Bitub.Dto;

using Xbim.Common;
using Xbim.Common.Enumerations;
using Xbim.Common.ExpressValidation;

namespace Bitub.Xbim.Ifc.Validation
{
    public static class Extensions
    {
        #region Ifc GUID validation

        // See https://technical.buildingsmart.org/resources/ifcimplementationguidance/ifc-guid/
        public const string IfcGuidAlphabet =
           //          1         2         3         4         5         6   
           //0123456789012345678901234567890123456789012345678901234567890123
           "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_$";


        public static bool IsValidIfcGuid(this string ifcGuid)
        {
            if (ifcGuid.Length != 22)
                return false;
            if (ifcGuid.Any(c => !IfcGuidAlphabet.Contains(c)))
                return false;

            return true;
        }

        public static Qualifier ToIfcGuidQualifier(this string ifcGuid)
        {
            if (IsValidIfcGuid(ifcGuid))
                return new Qualifier { Anonymous = new GlobalUniqueId { Base64 = ifcGuid } };
            else
                throw new ArgumentException("Invalid ifcGuid");
        }

        #endregion

        #region Scheme validation

        public static SchemaValidator ToSchemeValidator(this IModel model, ValidationFlags validationFlags = ValidationFlags.All)
        {
            return SchemaValidator.OfInstances(model.Instances, validationFlags);
        }

        /// <summary>
        /// Compares any two results.
        /// </summary>
        /// <param name="a">Validation result</param>
        /// <param name="b">Compared validation result</param>
        /// <returns>True, if both are equal by content</returns>
        public static bool IsSameResult(this ValidationResult a, ValidationResult b)
        {
            return (a.Item == b.Item)
                && (a.IssueType == b.IssueType)
                && String.Equals(a.IssueSource, b.IssueSource, StringComparison.Ordinal)
                && String.Equals(a.Message, b.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Compares any two results within their context (parent containment).
        /// </summary>
        /// <param name="a">Validation result</param>
        /// <param name="b">Compared validation result</param>
        /// <returns>True, if both are equal by content and context</returns>
        public static bool IsSameResultInContext(this ValidationResult a, ValidationResult b)
        {
            ValidationResult r1 = a;
            ValidationResult r2 = b;
            bool isSameInContext;
            do
            {
                isSameInContext = IsSameResult(r1, r2);
                r1 = r1.Context;
                r2 = r2.Context;
            } while (isSameInContext && (null != r1) && (null != r2));

            return isSameInContext && (null == r1) && (null == r2);
        }

        /// <summary>
        /// Does a set difference operation by comparing results A and B. 
        /// </summary>
        /// <param name="rLeft">Validation results left hand</param>
        /// <param name="rRight">Validation results right hand</param>
        /// <returns>Returns left without right</returns>
        public static IEnumerable<ValidationResult> Diff(this IEnumerable<ValidationResult> rLeft, IEnumerable<ValidationResult> rRight)
        {
            var left = new HashSet<ValidationResult>(rLeft, new ValidationResultEqualityComparer());
            foreach (var bResult in rRight)
                left.Remove(bResult);

            return left.ToArray();
        }

        /// <summary>
        /// Whether both results are equivalent. The compare is not sensitive to the order of results since
        /// it matches per item. The time stamp isn't considered.
        /// </summary>
        /// <param name="rLeft">Validation results left hand</param>
        /// <param name="rRight">Validation results right hand</param>
        /// <returns>True, if both have the same issues, same issue types and messages</returns>
        public static bool IsSameByResults(this IEnumerable<ValidationResult> rLeft, IEnumerable<ValidationResult> rRight)
        {
            return !Diff(rLeft, rRight).Any() && !Diff(rRight, rLeft).Any();
        }

        #endregion
    }
}
