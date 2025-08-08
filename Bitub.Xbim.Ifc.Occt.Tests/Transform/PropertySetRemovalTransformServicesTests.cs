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

namespace Bitub.Xbim.Ifc.Tests.Transform;

[TestFixture]
public class PropertySetRemovalTransformServicesTests : TRexWithGeometryServicesTest<PropertySetRemovalTransformServicesTests>
{
    [Test]
    public async Task RemoveByName()
    {
        using (var source = ReadIfcModel("Ifc4-Storey-Walls.ifc"))
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

            CancelableProgressing cp = NewProgressMonitor(true);
            using var result = await request.Run(source, cp);
            
            Assert.That(result.ResultCode, Is.EqualTo(TransformResult.Code.Finished));
            Assert.That(result.Target.Instances
                .OfType<IIfcPropertySet>()
                .Count(s => s.Name == "AllplanAttributes"),  Is.EqualTo(0));

            var propertySets = result.Target.Instances
                .OfType<IIfcPropertySet>()
                .Where(s => s.Name == "AllplanAttributes Copy")
                .ToArray();

            Assert.That(propertySets.Length, Is.EqualTo(4));
            Assert.That(propertySets.All(p => p.Properties<IIfcProperty>().Count() == 3), Is.True);

            var stampAfter = result.Target.ToSchemeValidator();
            Assert.IsTrue(stampAfter.IsCompliantToSchema);

            Assert.That(cp.State.State.HasFlag(ProgressTokenState.IsTerminated), Is.True);
        }
    }

    [Test]
    public async Task KeepAndRemoveByNameBoth()
    {
        using (var source = ReadIfcModel("Ifc4-Storey-Walls.ifc"))
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

            CancelableProgressing cp = NewProgressMonitor(true);
            using var result = await request.Run(source, cp);
            
            Assert.That(result.ResultCode, Is.EqualTo(TransformResult.Code.Finished));
            Assert.That(result.Target.Instances
                .OfType<IIfcPropertySet>()
                .Count(s => s.Name == "AllplanAttributes"),  Is.EqualTo(0));

            var propertySets = result.Target.Instances
                .OfType<IIfcPropertySet>()
                .Where(s => s.Name == "AllplanAttributes Copy")
                .ToArray();

            Assert.That(propertySets.Length, Is.EqualTo(4));
            Assert.That(propertySets.All(p => p.Properties<IIfcProperty>().Count() == 3), Is.True);

            var stampAfter = result.Target.ToSchemeValidator();
            Assert.IsTrue(stampAfter.IsCompliantToSchema);

            Assert.That(cp.State.State.HasFlag(ProgressTokenState.IsTerminated), Is.True);
        }
    }

    [Test]
    public async Task KeepOrRemoveByName()
    {
        using (var source = ReadIfcModel("Ifc4-SampleHouse.ifc"))
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

            CancelableProgressing cp = NewProgressMonitor(true);
            using var result = await request.Run(source, cp);
            
            var propertySets = result.Target.Instances
                .OfType<IIfcPropertySet>()
                .Select(s => s.Name.ToString())
                .Distinct()
                .ToArray();

            Assert.That(result.ResultCode, Is.EqualTo(TransformResult.Code.Finished));
            Assert.That(propertySets.Length, Is.EqualTo(1));
            Assert.That(string.Equals("Pset_SpaceCommon", propertySets[0], StringComparison.OrdinalIgnoreCase), Is.True);

            var stampAfter = result.Target.ToSchemeValidator();

            Assert.That(stampAfter.IsCompliantToSchema, Is.True);
            Assert.That(cp.State.State.HasFlag(ProgressTokenState.IsTerminated), Is.True);
        }
    }
}
