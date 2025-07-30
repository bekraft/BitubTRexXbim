using System.Linq;
using System.Threading.Tasks;

using Xbim.IO;
using Xbim.Ifc4.Interfaces;

using Bitub.Xbim.Ifc.Transform;
using Bitub.Xbim.Ifc.Validate;

using NUnit.Framework;

namespace Bitub.Xbim.Ifc.Tests.Transform;

[TestFixture]
public class ProductRepresentationRefactorServicesTests : TRexWithGeometryServicesTest<ProductRepresentationRefactorServicesTests>
{
    private TransformActionResult[] DefaultLogResultTypes => new[]
    {
        TransformActionResult.Skipped,
        TransformActionResult.Added
    };

    private static bool IsMultiRepresentation(IIfcProduct product, bool includingMappedItems, params string[] contexts)
    {
        return product.Representation.Representations
            .Where(r => contexts.Contains(r.ContextOfItems.ContextIdentifier.ToString()))
            .Any(r => r.Items.Select(i => CountOfNestedItems(i, includingMappedItems)).Sum() > 1);
    }

    private static int CountOfNestedItems(IIfcRepresentationItem item, bool includingMappedItems)
    {
        if (includingMappedItems && item is IIfcMappedItem mappedItem)
        {
            return mappedItem
                .MappingSource
                .MappedRepresentation
                .Items
                .Select(i => CountOfNestedItems(i, includingMappedItems))
                .Sum();
        }
        else
        {
            return 1;
        }
    }


    [Test]
    public async Task RefactorBody()
    {
        using (var source = ReadIfcModel("Ifc4-Pile-GroupOfBodies.ifc"))
        {
            var stampBefore = source.ToSchemeValidator();
            Assert.IsTrue(stampBefore.IsCompliantToSchema);

            var transform = new ProductRepresentationRefactorTransform(LoggerFactory, DefaultLogResultTypes)
            {
                ContextIdentifiers = new[] { "Body" },
                Strategy = ProductRefactorStrategy.DecomposeMultiItemRepresentations,
                TargetStoreType = XbimStoreType.InMemoryModel,
                EditorCredentials = EditorCredentials
            };

            Assert.AreEqual(1, source.Instances
                .OfType<IIfcBuildingElementProxy>()
                .Count(p => IsMultiRepresentation(p, false,"Body")));
            Assert.AreEqual(4, source.Instances
                .OfType<IIfcBuildingElementProxy>()
                .Count());

            var result = await transform.Run(source, NewProgressMonitor(true));
            
            SaveResultTarget(result);
            
            Assert.AreEqual(0, result.Target.Instances
                .OfType<IIfcBuildingElementProxy>()
                .Count(p => IsMultiRepresentation(p, false, "Body")));
            Assert.AreEqual(17, result.Target.Instances
                .OfType<IIfcBuildingElementProxy>()
                .Count());

            var validator = result.Target.ToSchemeValidator();
            Assert.IsTrue(validator.IsCompliantToSchema);
        }
    }

    [Test]
    public async Task RefactorMappedBodyWithIfcAssembly()
    {
        using (var source = ReadIfcModel("Ifc4-BuildingProxy-WithMappedShapeAndTransformation.ifc"))
        {
            var stampBefore = source.ToSchemeValidator();
            Assert.IsTrue(stampBefore.IsCompliantToSchema);

            var transform = new ProductRepresentationRefactorTransform(LoggerFactory)
            {
                ContextIdentifiers = new[] { "Body" },
                Strategy = ProductRefactorStrategy.DecomposeMultiItemRepresentations 
                    | ProductRefactorStrategy.DecomposeMappedRepresentations 
                    | ProductRefactorStrategy.DecomposeWithEntityElementAssembly,
                TargetStoreType = XbimStoreType.InMemoryModel,
                EditorCredentials = EditorCredentials
            };

            Assert.AreEqual(1, source.Instances.OfType<IIfcBuildingElementProxy>().Count(p => IsMultiRepresentation(p, true, "Body")));
            Assert.AreEqual(1, source.Instances.OfType<IIfcBuildingElementProxy>().Count());

            var result = await transform.Run(source, NewProgressMonitor(true));
            
            SaveResultTarget(result);

            Assert.AreEqual(0, result.Target.Instances.OfType<IIfcBuildingElementProxy>().Count(p => IsMultiRepresentation(p, true, "Body")));
            Assert.AreEqual(2, result.Target.Instances.OfType<IIfcBuildingElementProxy>().Count());

            var validator = result.Target.ToSchemeValidator();
            Assert.IsTrue(validator.IsCompliantToSchema);
        }
    }

    [Test]
    public async Task RefactorKeepMappedBody()
    {
        using (var source = ReadIfcModel("Ifc4-BuildingProxy-WithMappedShapeAndTransformation.ifc"))
        {
            var stampBefore = source.ToSchemeValidator();
            Assert.IsTrue(stampBefore.IsCompliantToSchema);

            var transform = new ProductRepresentationRefactorTransform(LoggerFactory)
            {
                ContextIdentifiers = new[] { "Body" },
                Strategy = ProductRefactorStrategy.DecomposeMultiItemRepresentations,
                TargetStoreType = XbimStoreType.InMemoryModel,
                EditorCredentials = EditorCredentials
            };

            Assert.AreEqual(1, source.Instances.OfType<IIfcBuildingElementProxy>().Count(p => IsMultiRepresentation(p, true, "Body")));
            Assert.AreEqual(1, source.Instances.OfType<IIfcBuildingElementProxy>().Count());

            var result = await transform.Run(source, NewProgressMonitor(true));
            
            SaveResultTarget(result);

            Assert.AreEqual(1, result.Target.Instances.OfType<IIfcBuildingElementProxy>().Count(p => IsMultiRepresentation(p, true, "Body")));
            Assert.AreEqual(1, result.Target.Instances.OfType<IIfcBuildingElementProxy>().Count());

            var validator = result.Target.ToSchemeValidator();
            Assert.IsTrue(validator.IsCompliantToSchema);
        }
    }

    [Test]
    public async Task RefactorBodyWithIfcAssembly()
    {
        using (var source = ReadIfcModel("Ifc4-Pile-GroupOfBodies.ifc"))
        {
            var stampBefore = source.ToSchemeValidator();
            Assert.IsTrue(stampBefore.IsCompliantToSchema);

            var transform = new ProductRepresentationRefactorTransform(LoggerFactory)
            {
                ContextIdentifiers = new[] { "Body" },
                Strategy = ProductRefactorStrategy.DecomposeWithEntityElementAssembly | ProductRefactorStrategy.DecomposeMultiItemRepresentations,
                TargetStoreType = XbimStoreType.InMemoryModel,
                EditorCredentials = EditorCredentials
            };

            Assert.AreEqual(1, source.Instances.OfType<IIfcBuildingElementProxy>().Count(p => IsMultiRepresentation(p, false, "Body")));
            Assert.AreEqual(4, source.Instances.OfType<IIfcBuildingElementProxy>().Count());

            var result = await transform.Run(source, NewProgressMonitor(true));
            
            SaveResultTarget(result);

            Assert.AreEqual(0, result.Target.Instances.OfType<IIfcBuildingElementProxy>().Count(p => IsMultiRepresentation(p, false, "Body")));
            Assert.AreEqual(17, result.Target.Instances.OfType<IIfcBuildingElementProxy>().Count());
            Assert.AreEqual(1, result.Target.Instances.OfType<IIfcElementAssembly>().Count());
            Assert.AreEqual(14, result.Target.Instances.OfType<IIfcElementAssembly>().First().IsDecomposedBy.SelectMany(r => r.RelatedObjects).Count());

            var validator = result.Target.ToSchemeValidator();
            Assert.IsTrue(validator.IsCompliantToSchema);
        }
    }
}