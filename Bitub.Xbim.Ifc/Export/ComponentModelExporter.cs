using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.Extensions.Logging;

using Xbim.Common;

using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

using Bitub.Dto;
using Bitub.Dto.Spatial;
using Bitub.Dto.Scene;

using Component = Bitub.Dto.Scene.Component;
using System.Threading.Tasks;

using Bitub.Xbim.Ifc.Tesselate;
using Bitub.Xbim.Ifc.Concept;

namespace Bitub.Xbim.Ifc.Export
{
    /// <summary>
    /// Transfer scene model data exporter. Internally uses an abstract tesselation provider. In case of Xbim tesselation use
    /// <code>
    /// var exporter = new IfcSceneExporter(new XbimTesselationContext(loggerFactory), loggerFactory);
    /// var result = await exporter.Run(myModel);
    /// </code>
    /// </summary>
    public class ComponentModelExporter : IExporter<ComponentScene>
    {
        #region Internals
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly ITesselationContext<ScenePreferences> _tesselatorInstance;
        #endregion

        /// <summary>
        /// Creates a new instance of a scene exporter.
        /// </summary>
        /// <param name="loggerFactory"></param>
        public ComponentModelExporter(ITesselationContext<ScenePreferences> tesselatorInstance, ILoggerFactory loggerFactory = null)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory?.CreateLogger<ComponentModelExporter>();
            _tesselatorInstance = tesselatorInstance;
        }

        /// <summary>
        /// Initial experter settings.
        /// </summary>
        public ScenePreferences Preferences { get; set; } = new ScenePreferences();

        /// <summary>
        /// Default color settings.
        /// </summary>
        public XbimColourMap DefaultProductColorMap { get; set; } = new XbimColourMap(StandardColourMaps.IfcProductTypeMap);

        
        /// <summary>
        /// Runs the model transformation.
        /// </summary>
        /// <param name="model">The IFC model</param>
        /// <returns>A scene</returns>
        public Task<ComponentScene> RunExport(IModel model, CancelableProgressing monitor)
        {
            return Task.Run(() =>
            {
                try
                {
                    var applied = new ScenePreferences(Preferences);
                    if (Preferences.BodyExportType == 0)
                        applied.BodyExportType = SceneBodyExportType.MeshBody;

                    return BuildScene(model, applied, monitor);
                }
                catch (Exception e)
                {
                    monitor?.State.MarkBroken();
                    _logger.LogError("{0}: {1} [{2}]", e.GetType().Name, e.Message, e.StackTrace);
                    throw new ThreadInterruptedException($"Export broke due to {e.Message}", e);
                }
            });
        }

        // Runs the scene model export
        private ComponentScene BuildScene(IModel model, ScenePreferences exportSettings, CancelableProgressing progressing)
        {
            var exportContext = new SceneContext<ScenePreferences>(_loggerFactory);
            exportContext.InitContextsAndScaleFromModel(model, exportSettings);

            // Transfer materials
            var componentScene = exportContext.CreateEmptySceneModelFromProject(model.Instances.OfType<IIfcProject>().First());
            var materials = model.ToMaterialBySurfaceStyles().ToDictionary(m => m.Id.Nid);
            componentScene.Materials.AddRange(materials.Values);
            
            _logger?.LogInformation("Starting model tesselation of {0}", model.Header.Name);
            // Retrieve enumeration of components having a geomety within given contexts            
            var messages = _tesselatorInstance.Tesselate(model, exportContext, progressing);
            var ifcClassifierMap = model.SchemaVersion.ToImplementingClassification<IIfcProduct>();

            _logger?.LogInformation("Starting model export of {0}", model.Header.Name);

            // Run transfer and log parents
            var parents = new HashSet<int>();
            var componentCache = new Dictionary<int, Component>();
            foreach (var msg in messages)
            {
                if (progressing?.State.IsAboutCancelling ?? false)
                {
                    _logger?.LogInformation("Canceled model export of '{0}'", model.Header.FileName);
                    progressing.State.MarkCanceled();
                    break;
                }

                switch (msg.messageType)
                {
                    case TesselationMessageType.Context:
                        componentScene.Contexts.Add(msg.SceneContext.sceneContext);
                        break;
                    case TesselationMessageType.Representation:
                        componentScene.ShapeBodies.Add(msg.ShapeRepresentation.shapeBody);
                        break;
                    case TesselationMessageType.Shape:
                        var product = model.Instances[msg.ProductShape.productLabel] as IIfcProduct;
                        
                        if (!componentCache.TryGetValue(product.EntityLabel, out Component c))
                        {
                            c = product.ToComponent(out int? optParent, ifcClassifierMap, exportSettings.ComponentIdentificationStrategy);

                            componentCache.Add(product.EntityLabel, c);
                            componentScene.Components.Add(c);

                            if (optParent.HasValue)
                                parents.Add(optParent.Value);
                        }

                        c.Shapes.AddRange(msg.ProductShape.shapes);
                        c.BoundingBox = msg.ProductShape.shapes
                            .Select(shape => shape.BoundingBox)
                            .Aggregate(new BoundingBox { ABox = ABox.Empty }, (a, b) => a.UnionWith(b));
                        break;
                }
            }

            // Check for remaining components (i.e. missing parents without geometry)
            parents.RemoveWhere(id => componentCache.ContainsKey(id));
            var missingInstance = new Queue<int>(parents);
            while (missingInstance.Count > 0)
            {
                if (progressing?.State.IsAboutCancelling ?? false)
                {
                    if (!progressing.State.IsCanceled)
                    {
                        _logger?.LogInformation("Canceled model export of '{0}'", model.Header.FileName);
                        progressing.State.MarkCanceled();
                    }
                    break;
                }

                if (model.Instances[missingInstance.Dequeue()] is IIfcProduct product)
                {
                    if (!componentCache.TryGetValue(product.EntityLabel, out Component c))
                    {
                        c = product.ToComponent(out int? optParent, ifcClassifierMap, exportSettings.ComponentIdentificationStrategy);

                        componentCache.Add(product.EntityLabel, c);

                        if (optParent.HasValue && !componentCache.ContainsKey(optParent.Value))
                            // Enqueue missing parents
                            missingInstance.Enqueue(optParent.Value);

                        componentScene.Components.Add(c);
                    }
                }
            }

            // Add default materials where required (Nid < 0)
            componentScene.Materials.AddRange(
                DefaultProductColorMap.ToMaterialByIfcTypeIDs( 
                    model,
                    componentScene.Components
                        .SelectMany(c => c.Shapes)
                        .Select(s => s.Material)
                        .Where(m => 0 > m.Nid) // Only negative (not allowed in IFC label specification, Xbim internal meaning 
                        .Select(m => - m.Nid) // Make positive
                        .Distinct(),
                    (nid) => new RefId { Nid = -nid } // Create negative NIDs again
                )
            );

            return componentScene;
        }        
    }
}
