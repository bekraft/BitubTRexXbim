﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

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
                var validationStamp = SchemaValidator.OfModel(source,
                    ValidationFlags.Properties | ValidationFlags.Inverses);

                var lookUp = validationStamp.InstanceResults;
                
                Assert.AreEqual(1, lookUp.Count);

                Assert.IsTrue(validationStamp.IsConstraintToSchema);
                Assert.IsFalse(validationStamp.IsCompliantToSchema);

                var results = lookUp[new XbimInstanceHandle(source.Instances[176464])];
                Assert.AreEqual(1, results.Count());

                Assert.IsFalse(SchemaValidator.Diff(validationStamp.Results, validationStamp.Results).Any());
                Assert.IsTrue(validationStamp.Equals(validationStamp));
            }
        }

        [DeploymentItem(@"Resources\Ifc2x3-Slab-BooleanResult.ifc")]
        [TestMethod]
        public void IsSchemetaConstraintCompliant()
        {
            IfcStore.ModelProviderFactory.UseMemoryModelProvider();
            using (var source = IfcStore.Open(@"Resources\Ifc2x3-Slab-BooleanResult.ifc"))
            {
                var validationStamp = SchemaValidator.OfModel(source,
                    ValidationFlags.TypeWhereClauses | ValidationFlags.EntityWhereClauses);

                var lookUp = validationStamp.InstanceResults;

                Assert.AreEqual(1, lookUp.Count);

                Assert.IsFalse(validationStamp.IsConstraintToSchema);
                Assert.IsTrue(validationStamp.IsCompliantToSchema);

                var results = lookUp[new XbimInstanceHandle(source.Model.Instances[25])];
                Assert.AreEqual(1, results.Count());

                Assert.IsFalse(SchemaValidator.Diff(validationStamp.Results, validationStamp.Results).Any());
                Assert.IsTrue(validationStamp.Equals(validationStamp));
            }
        }

        /*
        //[DeploymentItem(@"Resources\Ifc4-Rotated-1st-floor.ifc")]
        [TestMethod]
        public void GeometryConstraintTest()
        {
            IfcStore.ModelProviderFactory.UseMemoryModelProvider();
            using (var source = IfcStore.Open(@"P:\2020\2020-0157\Projekt\06\00_IFC_Datadrop\03_Extern\ESB_3_002-(HH-LSBG).ifc"))
            {
                using (var writer = new StreamWriter(File.OpenWrite(@"P:\2020\2020-0157\Projekt\06\00_IFC_Datadrop\03_Extern\ESB_3_002-(HH-LSBG).md")))
                {
                    double oneMeter = source.ModelFactors.LengthToMetresConversionFactor;
                    double oneMeter3 = oneMeter * oneMeter * oneMeter;

                    foreach (var issue in new GeometryValidator(LoggerFactory).GetIssuesFromModel(source).Where(i => i.Any(info => info.HasGeometricalIssues)))
                    {
                        logger.LogInformation($"Found issues for #{issue.Key.EntityLabel} '{issue.Key.Name ?? "Unkown"}' GUID '{issue.Key.GlobalId}'");
                        writer.WriteLine($"# #{issue.Key.EntityLabel} ({issue.Key.ExpressType.Name}) '{issue.Key.Name ?? "Unkown"}'");
                        writer.WriteLine();

                        writer.WriteLine("| # | IFC Typ | Geometrietyp | Issue Typ | Bounding box | Volumen | Geschlossenes Volumen |");
                        writer.WriteLine("|:-:|:-:|:-:|:-:|-:|-:|-:|");
                        issue.Where(info => info.HasGeometricalIssues).OrderByDescending(info => info.BoundingBox.Volume()).ForEach(info =>
                        {
                            writer.WriteLine($"| {info.InstanceHandle.EntityLabel} | {info.InstanceHandle.EntityExpressType.Name} | {info.ObjectType.Name} " + 
                                $"| {info.IssueFlag} | {info.BoundingBox.Volume() * oneMeter3} m3 | {info.Volume * oneMeter3 ?? 0} m3 | {info.EnclosedVolume * oneMeter3} m3 |");
                        });
                        writer.WriteLine();
                        writer.Flush();
                    }
                }
            }
        }
        */
    }
}
