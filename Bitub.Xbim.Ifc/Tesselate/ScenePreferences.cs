using System;
using System.IO;
using System.Xml.Serialization;

using Bitub.Dto;
using Bitub.Dto.Scene;
using Bitub.Dto.Spatial;

namespace Bitub.Xbim.Ifc.Tesselate
{
    /// <summary>
    /// Identification strategy.
    /// </summary>
    public enum SceneComponentIdentificationStrategy
    {
        UseGloballyUniqueID,
        UseIfcInstanceLabel
    }

    /// <summary>
    /// Transformation labeling strategy. Either provide as quaternion or matrix4x4 representation.
    /// </summary>
    public enum SceneTransformationStrategy
    {
        Quaternion,
        Matrix
    }

    /// <summary>
    /// Scene positioning strategy.
    /// </summary>
    public enum ScenePositioningStrategy
    {
        NoCorrection,
        UserCorrection,
        MeanTranslationCorrection,
        MostPopulatedRegionCorrection,
        SignificantPopulationCorrection,
        MostExtendedRegionCorrection
    }

    /// <summary>
    /// Scene export body type. If <c>Auto</c>, exporting context will decide best match to provided data.
    /// </summary>
    [Flags]
    public enum SceneBodyExportType
    {
        /// <summary>
        /// Lets the exporter decide.
        /// </summary>
        Auto = 0,
        /// <summary>
        /// Transmit only wires.
        /// </summary>
        WireBody = 0x01,
        /// <summary>
        /// Transmit full meshes only
        /// </summary>
        MeshBody = 0x02,
        /// <summary>
        /// Transmit mesh faces having boundary loops.
        /// </summary>
        FaceBody = 0x04        
    }

    [XmlRoot("ScenePreferences", Namespace = "https://github.com/bekraft/BitubTRexXbim/Bitub.Xbim.Ifc.Tesselate")]
    public class ScenePreferences
    {
        /// <summary>
        /// Translation correction strategy to be applied while transferring data.
        /// </summary>
        public ScenePositioningStrategy Positioning { get; set; } = default(ScenePositioningStrategy);

        /// <summary>
        /// Transformation strategy (default using global coordinates).
        /// </summary>
        public SceneTransformationStrategy Transforming { get; set; } = default;

        /// <summary>
        /// Given user translation (in units) a priori. Only applies of positioning is set to UserCorrection.
        /// </summary>
        public XYZ UserModelCenter { get; set; } = new XYZ();

        /// <summary>
        /// Maximum threshold of centre bias of local coordinates relative to local origin.
        /// Percentage measure of greater axis extent of bounding box. By default disabled (positive infinity). If set
        /// to value less than infinity, the local origin will be translated to the center of the bounding box. Local transforms will
        /// be adjusted to reflect the change. Sometimes offsets are directly embedded into vertex coordinates instead of transform. This option be
        /// sensitive to these biased exports.
        /// </summary>
        // TODO 
        //public float MaxBodyOriginCentreBias { get; set; } = float.PositiveInfinity;

        /// <summary>
        /// If <see cref="SceneComponentIdentificationStrategy.UseGloballyUniqueID"/>, IFC's entity labels will be used as ID. Otherwise object's globally unique IDs are preferred. Enable this option, if given GUIDs are not
        /// unique (should, but not guranteed by vendor software). When using entity labels, component IDs won't be unique across multiple scenes.
        /// </summary>
        public SceneComponentIdentificationStrategy ComponentIdentificationStrategy { get; set; } = SceneComponentIdentificationStrategy.UseGloballyUniqueID;

        /// <summary>
        /// Custom target CRS including additional offset in target system.
        /// </summary>
        public Dto.Scene.Transform CRS { get; set; } = Dto.Scene.Transform.Identity;

        /// <summary>
        /// Scale vector per dimension x,y and z denoting the count of units per meter.
        /// </summary>
        public XYZ Scale { get; set; } = XYZ.Ones;

        /// <summary>
        /// The representation contexts to transfer.
        /// </summary>
        public SceneContext[] SelectedContext { get; set; } = new SceneContext[] { new SceneContext { Name = "Body".ToQualifier() } };

        /// <summary>
        /// The body shape export strategy
        /// </summary>
        public SceneBodyExportType BodyExportType { get; set; } = SceneBodyExportType.Auto;

        public ScenePreferences()
        {
        }

        public ScenePreferences(ScenePreferences settings)
        {
            foreach (var prop in GetType().GetProperties())
            {
                prop.SetValue(this, prop.GetValue(settings));
            }
        }

        public static ScenePreferences ReadFrom(string fileName)
        {
            var serializer = new XmlSerializer(typeof(ScenePreferences));
            return serializer.Deserialize(File.OpenText(fileName)) as ScenePreferences;
        }

        public void SaveTo(string fileName)
        {
            var serializer = new XmlSerializer(typeof(ScenePreferences));
            serializer.Serialize(File.CreateText(fileName), this);
        }
    }
}
