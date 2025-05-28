using System;
using System.Linq;
using System.Threading.Tasks;

using Xbim.Ifc4.Interfaces;

using Bitub.Dto;

using Bitub.Xbim.Ifc.Transform;
using Bitub.Xbim.Ifc.Validate;

using Xbim.IO;

using NUnit.Framework;

using Microsoft.Extensions.Logging;

namespace Bitub.Xbim.Ifc.Occt.Tests.Transform
{
    [TestFixture]
    public class PropertySetRemovalTransformTests : GeometryTestBase<PropertySetRemovalTransformTests>
    {
        [Test]
        public async Task RemoveByName()
        {
            using (var source = ReadIfc4Model("Ifc4-Storey-Walls.ifc"))
            {
                var stampBefore = source.ToSchemeValidator();
                Assert.IsTrue(stampBefore.IsCompliantToSchema);

                Assert.AreEqual(4, source.Instances
                    .OfType<IIfcPropertySet>()
                    .Count(s => s.Name == "AllplanAttributes"));

                var request = new PropertySetRemovalTransform(LoggerFactory)
                {
                    ExludePropertySetByName = new string[] { "AllplanAttributes" },
                    IsNameMatchingCaseSensitive = false,
                    // Common config
                    TargetStoreType = XbimStoreType.InMemoryModel,
                    EditorCredentials = EditorCredentials
                };

                CancelableProgressing cp;
                using (var result = await request.Run(source, cp = NewProgressMonitor()))
                {
                    if (null != result.Cause)
                        Logger?.LogError("Exception: {0}, {1}, {2}", result.Cause, result.Cause.Message, result.Cause.StackTrace);

                    Assert.AreEqual(TransformResult.Code.Finished, result.ResultCode);
                    Assert.AreEqual(0, result.Target.Instances
                        .OfType<IIfcPropertySet>()
                        .Count(s => s.Name == "AllplanAttributes"));

                    var pset = result.Target.Instances
                        .OfType<IIfcPropertySet>()
                        .Where(s => s.Name == "AllplanAttributes Copy")
                        .ToArray();

                    Assert.AreEqual(4, pset.Length);
                    Assert.IsTrue(pset.All(p => p.Properties<IIfcProperty>().Count() == 3));

                    var stampAfter = result.Target.ToSchemeValidator();
                    Assert.IsTrue(stampAfter.IsCompliantToSchema);

                    Assert.IsTrue(cp.State.State.HasFlag(ProgressTokenState.IsTerminated));
                }
            }
        }

        [Test]
        public async Task KeepAndRemoveByNameBoth()
        {
            using (var source = ReadIfc4Model("Ifc4-Storey-Walls.ifc"))
            {
                var stampBefore = source.ToSchemeValidator();
                Assert.IsTrue(stampBefore.IsCompliantToSchema);

                var request = new PropertySetRemovalTransform(LoggerFactory)
                {
                    ExludePropertySetByName = new string[] { "AllplanAttributes" },
                    IncludePropertySetByName = new string[] { "AllplanAttributes", "AllplanAttributes Copy" },
                    IsNameMatchingCaseSensitive = false,
                    FilterRuleStrategy = FilterRuleStrategyType.ExcludeBeforeInclude,
                    // Common config
                    TargetStoreType = XbimStoreType.InMemoryModel,
                    EditorCredentials = EditorCredentials
                };

                CancelableProgressing cp;
                using (var result = await request.Run(source, cp = NewProgressMonitor()))
                {
                    if (null != result.Cause)
                        Logger?.LogError("Exception: {0}, {1}, {2}", result.Cause, result.Cause.Message, result.Cause.StackTrace);

                    Assert.AreEqual(TransformResult.Code.Finished, result.ResultCode);

                    Assert.AreEqual(0, result.Target.Instances
                        .OfType<IIfcPropertySet>()
                        .Count(s => s.Name == "AllplanAttributes"));

                    var pset = result.Target.Instances
                        .OfType<IIfcPropertySet>()
                        .Where(s => s.Name == "AllplanAttributes Copy")
                        .ToArray();

                    Assert.AreEqual(4, pset.Length);
                    Assert.IsTrue(pset.All(p => p.Properties<IIfcProperty>().Count() == 3));                    

                    var stampAfter = result.Target.ToSchemeValidator();

                    Assert.IsTrue(stampAfter.IsCompliantToSchema);
                    Assert.IsTrue(cp.State.State.HasFlag(ProgressTokenState.IsTerminated));
                }
            }
        }

        [Test]
        public async Task KeepOrRemoveByName()
        {
            using (var source = ReadIfc4Model("Ifc4-SampleHouse.ifc"))
            {
                var stampBefore = source.ToSchemeValidator();
                Assert.IsTrue(stampBefore.IsCompliantToSchema);

                var request = new PropertySetRemovalTransform(LoggerFactory)
                {
                    ExludePropertySetByName = new string[] { "Other" },
                    IncludePropertySetByName = new string[] { "Pset_SpaceCommon", "Other" },
                    IsNameMatchingCaseSensitive = false,
                    FilterRuleStrategy = FilterRuleStrategyType.ExcludeBeforeInclude,
                    // Common config
                    TargetStoreType = XbimStoreType.InMemoryModel,
                    EditorCredentials = EditorCredentials
                };

                CancelableProgressing cp;
                using (var result = await request.Run(source, cp = NewProgressMonitor()))
                {
                    if (null != result.Cause)
                        Logger?.LogError("Exception: {0}, {1}, {2}", result.Cause, result.Cause.Message, result.Cause.StackTrace);

                    var psetsRemaining = result.Target.Instances
                        .OfType<IIfcPropertySet>()
                        .Select(s => s.Name.ToString())
                        .Distinct()
                        .ToArray();

                    Assert.AreEqual(TransformResult.Code.Finished, result.ResultCode);
                    Assert.AreEqual(1, psetsRemaining.Length);
                    Assert.IsTrue(string.Equals("Pset_SpaceCommon", psetsRemaining[0], StringComparison.OrdinalIgnoreCase));

                    var stampAfter = result.Target.ToSchemeValidator();

                    Assert.IsTrue(stampAfter.IsCompliantToSchema);
                    Assert.IsTrue(cp.State.State.HasFlag(ProgressTokenState.IsTerminated));
                }
            }
        }
    }
}
