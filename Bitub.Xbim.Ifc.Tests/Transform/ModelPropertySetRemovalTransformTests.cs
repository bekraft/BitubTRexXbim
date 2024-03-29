﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

using Bitub.Dto;
using Bitub.Dto.Spatial;

using Bitub.Xbim.Ifc;
using Bitub.Xbim.Ifc.Transform;
using Bitub.Xbim.Ifc.Validation;

using Xbim.IO;

namespace Bitub.Xbim.Ifc.Tests.Transform
{
    [TestClass]
    public class ModelPropertySetRemovalTransformTests : TestBase<ModelPropertySetRemovalTransformTests>
    {
        [TestMethod]
        [DeploymentItem(@"Resources\Ifc4-Storey-With-4Walls.ifc")]
        public async Task RemoveByName()
        {
            IfcStore.ModelProviderFactory.UseMemoryModelProvider();
            using (var source = IfcStore.Open(@"Resources\Ifc4-Storey-With-4Walls.ifc"))
            {
                var stampBefore = source.ToSchemeValidator();
                Assert.IsTrue(stampBefore.IsCompliantToSchema);

                Assert.AreEqual(4, source.Instances
                    .OfType<IIfcPropertySet>()
                    .Where(s => s.Name == "AllplanAttributes")
                    .Count());

                var request = new ModelPropertySetRemovalTransform(LoggerFactory)
                {
                    ExludePropertySetByName = new string[] { "AllplanAttributes" },
                    IsNameMatchingCaseSensitive = false,
                    // Common config
                    TargetStoreType = XbimStoreType.InMemoryModel,
                    EditorCredentials = EditorCredentials
                };

                var cp = new CancelableProgressing(true);
                cp.OnProgressChange += (sender, e) => logger.LogDebug($"State {e.State}: Percentage = {e.Percentage}; State object = {e.StateObject}");

                using (var result = await request.Run(source, cp))
                {
                    if (null != result.Cause)
                        logger?.LogError("Exception: {0}, {1}, {2}", result.Cause, result.Cause.Message, result.Cause.StackTrace);

                    Assert.AreEqual(TransformResult.Code.Finished, result.ResultCode);
                    Assert.AreEqual(0, result.Target.Instances
                        .OfType<IIfcPropertySet>()
                        .Where(s => s.Name == "AllplanAttributes")
                        .Count());

                    var pset = result.Target.Instances
                        .OfType<IIfcPropertySet>()
                        .Where(s => s.Name == "AllplanAttributes Copy")
                        .ToArray();

                    Assert.AreEqual(4, pset.Length);
                    Assert.IsTrue(pset.All(p => p.Properties<IIfcProperty>().Count() == 3));

                    var stampAfter = result.Target.ToSchemeValidator();
                    Assert.IsTrue(stampAfter.IsCompliantToSchema);

                    Assert.IsTrue(cp.State.State.HasFlag(ProgressTokenState.IsTerminated));

                    result.Target.SaveAsIfc(new FileStream("Ifc4-Storey-With-4Walls-AllplanAttributes-Copy-1.ifc", FileMode.Create));
                }
            }
        }

        [TestMethod]
        [DeploymentItem(@"Resources\Ifc4-Storey-With-4Walls.ifc")]
        public async Task KeepAndRemoveByNameBoth()
        {
            IfcStore.ModelProviderFactory.UseMemoryModelProvider();
            using (var source = IfcStore.Open(@"Resources\Ifc4-Storey-With-4Walls.ifc"))
            {
                var stampBefore = source.ToSchemeValidator();
                Assert.IsTrue(stampBefore.IsCompliantToSchema);

                var request = new ModelPropertySetRemovalTransform(LoggerFactory)
                {
                    ExludePropertySetByName = new string[] { "AllplanAttributes" },
                    IncludePropertySetByName = new string[] { "AllplanAttributes", "AllplanAttributes Copy" },
                    IsNameMatchingCaseSensitive = false,
                    FilterRuleStrategy = FilterRuleStrategyType.ExcludeBeforeInclude,
                    // Common config
                    TargetStoreType = XbimStoreType.InMemoryModel,
                    EditorCredentials = EditorCredentials
                };

                var cp = new CancelableProgressing(true);
                cp.OnProgressChange += (sender, e) => logger.LogDebug($"State {e.State}: Percentage = {e.Percentage}; State object = {e.StateObject}");

                using (var result = await request.Run(source, cp))
                {
                    if (null != result.Cause)
                        logger?.LogError("Exception: {0}, {1}, {2}", result.Cause, result.Cause.Message, result.Cause.StackTrace);

                    Assert.AreEqual(TransformResult.Code.Finished, result.ResultCode);

                    Assert.AreEqual(0, result.Target.Instances
                        .OfType<IIfcPropertySet>()
                        .Where(s => s.Name == "AllplanAttributes")
                        .Count());

                    var pset = result.Target.Instances
                        .OfType<IIfcPropertySet>()
                        .Where(s => s.Name == "AllplanAttributes Copy")
                        .ToArray();

                    Assert.AreEqual(4, pset.Length);
                    Assert.IsTrue(pset.All(p => p.Properties<IIfcProperty>().Count() == 3));                    

                    var stampAfter = result.Target.ToSchemeValidator();

                    Assert.IsTrue(stampAfter.IsCompliantToSchema);
                    Assert.IsTrue(cp.State.State.HasFlag(ProgressTokenState.IsTerminated));

                    result.Target.SaveAsIfc(new FileStream("Ifc4-Storey-With-4Walls-AllplanAttributes-Copy-2.ifc", FileMode.Create));
                }
            }
        }

        [TestMethod]
        [DeploymentItem(@"Resources\Ifc4-SampleHouse.ifc")]
        public async Task KeepOrRemoveByName()
        {
            IfcStore.ModelProviderFactory.UseMemoryModelProvider();
            using (var source = IfcStore.Open(@"Resources\Ifc4-SampleHouse.ifc"))
            {
                var stampBefore = source.ToSchemeValidator();
                Assert.IsTrue(stampBefore.IsCompliantToSchema);

                var request = new ModelPropertySetRemovalTransform(LoggerFactory)
                {
                    ExludePropertySetByName = new string[] { "Other" },
                    IncludePropertySetByName = new string[] { "Pset_SpaceCommon", "Other" },
                    IsNameMatchingCaseSensitive = false,
                    FilterRuleStrategy = FilterRuleStrategyType.ExcludeBeforeInclude,
                    // Common config
                    TargetStoreType = XbimStoreType.InMemoryModel,
                    EditorCredentials = EditorCredentials
                };

                var cp = new CancelableProgressing(true);
                cp.OnProgressChange += (sender, e) => logger.LogDebug($"State {e.State}: Percentage = {e.Percentage}; State object = {e.StateObject}");

                using (var result = await request.Run(source, cp))
                {
                    if (null != result.Cause)
                        logger?.LogError("Exception: {0}, {1}, {2}", result.Cause, result.Cause.Message, result.Cause.StackTrace);

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

                    result.Target.SaveAsIfc(new FileStream("Ifc4-SampleHouse-Pset_SpaceCommon-Other.ifc", FileMode.Create));
                }
            }
        }
    }
}
