using System.Collections.Generic;
using Xbim.Common.ExpressValidation;

namespace Bitub.Xbim.Ifc.Validation
{
    public class ValidationResultEqualityComparer : IEqualityComparer<ValidationResult>
    {
        public bool Equals(ValidationResult x, ValidationResult y)
        {
            return x.IsSameResult(y);
        }

        public int GetHashCode(ValidationResult obj)
        {
            return obj.GetHashCode();
        }
    }
}
