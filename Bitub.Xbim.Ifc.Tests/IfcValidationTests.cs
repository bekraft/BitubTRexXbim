using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Linq;

using Xbim.Ifc;
using Xbim.Common;
using Xbim.Common.Enumerations;

using Bitub.Xbim.Ifc.Validation;

namespace Bitub.Xbim.Ifc.Tests
{
    [TestClass]
    public class IfcValidationTests : TestBase<IfcValidationTests>
    {
        [DeploymentItem(@"Resources\Ifc2x3-Slab-BooleanResult.ifc")]
        [TestMethod]
        public void IsSchemataCompliant()
        {
            IfcStore.ModelProviderFactory.UseMemoryModelProvider();
            using (var source = IfcStore.Open(@"Resources\Ifc2x3-Slab-BooleanResult.ifc"))
            {
                var validationStamp = source.ToSchemeValidator(
                    ValidationFlags.Properties | ValidationFlags.Inverses);

                var lookUp = validationStamp.InstanceResults;
                
                Assert.AreEqual(1, lookUp.Count);

                Assert.IsTrue(validationStamp.IsConstraintToSchema);
                Assert.IsFalse(validationStamp.IsCompliantToSchema);

                var results = lookUp[new XbimInstanceHandle(source.Instances[176464])];
                Assert.AreEqual(1, results.Count());

                Assert.IsFalse(Extensions.Diff(validationStamp.Results, validationStamp.Results).Any());
                Assert.IsTrue(validationStamp.Results.IsSameByResults(validationStamp.Results));
            }
        }

        [DeploymentItem(@"Resources\Ifc2x3-Slab-BooleanResult.ifc")]
        [TestMethod]
        public void IsSchemetaConstraintCompliant()
        {
            IfcStore.ModelProviderFactory.UseMemoryModelProvider();
            using (var source = IfcStore.Open(@"Resources\Ifc2x3-Slab-BooleanResult.ifc"))
            {
                var validationStamp = source.ToSchemeValidator(
                    ValidationFlags.TypeWhereClauses | ValidationFlags.EntityWhereClauses);

                var lookUp = validationStamp.InstanceResults;

                Assert.AreEqual(1, lookUp.Count);

                Assert.IsFalse(validationStamp.IsConstraintToSchema);
                Assert.IsTrue(validationStamp.IsCompliantToSchema);

                var results = lookUp[new XbimInstanceHandle(source.Model.Instances[25])];
                Assert.AreEqual(1, results.Count());

                Assert.IsFalse(validationStamp.Results.Diff(validationStamp.Results).Any());
                Assert.IsTrue(validationStamp.Results.IsSameByResults(validationStamp.Results));
            }
        }
    }
}
