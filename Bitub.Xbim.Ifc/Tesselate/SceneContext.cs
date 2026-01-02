using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Ifc4.Interfaces;

using Bitub.Dto;
using Bitub.Dto.Scene;
using Bitub.Xbim.Ifc.Transform;

using Google.Protobuf.WellKnownTypes;

using Microsoft.Extensions.Logging;

namespace Bitub.Xbim.Ifc.Tesselate
{
    /// <summary>
    /// Builds up an export context based on settings and model configuration.
    /// </summary>
    /// <typeparam name="TSettings">The settings type</typeparam>
    public class SceneContext<TSettings> where TSettings: ScenePreferences
    {
        #region Private members
        private readonly ILogger _logger;
        #endregion

        public SceneContext(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger<SceneContext<TSettings>>();
        }

        public SceneContext(TSettings settings, XbimVector3D scale, XbimMatrix3D crs) 
        {
            this.Settings = settings;
            this.Scale = scale;
            this.CRS = crs;
        }

        /// <summary>
        /// Settings.
        /// </summary>
        public TSettings Settings { get; private set; }

        /// <summary>
        /// Scene scale in each direction.
        /// </summary>
        public XbimVector3D Scale { get; private set; } = new XbimVector3D(1, 1, 1);

        /// <summary>
        /// Coordinate Reference System (global transformation).
        /// </summary>
        public XbimMatrix3D CRS { get; private set; } = XbimMatrix3D.Identity;

        /// <summary>
        /// IFC context label versus Scene Context Transform.
        /// </summary>
        public ConcurrentDictionary<int, SceneContextTransform> ContextCache { get; } = new ConcurrentDictionary<int, SceneContextTransform>();
        /// <summary>
        /// IFC shape label versus Representation Qualifier.
        /// </summary>
        public ConcurrentDictionary<int, Qualifier> RepresentationQualifierCache { get; } = new ConcurrentDictionary<int, Qualifier>();

        /// <summary>
        /// Initialize context and scale from given Xbim IFC model using the given settings.
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="settings">The settings</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void InitContextsAndScaleFromModel(IModel model, TSettings settings)
        {
            if (null == settings)
                throw new ArgumentNullException(nameof(settings));

            Settings = settings;
            Scale = (Settings.Scale * (1.0 / model.ModelFactors.OneMeter)).ToXbimVector3D();
            CRS = Settings.CRS.ToXbimMatrix();

            Settings.SelectedContext = Settings.SelectedContext.Select(c => new SceneContext
            {
                Name = c.Name,
                // Given in DEG => use as it is
                FDeflection = model.ModelFactors.DeflectionAngle,
                // Given internally in model units => convert to meter
                FTolerance = model.ModelFactors.LengthToMetresConversionFactor * model.ModelFactors.DeflectionTolerance,
            }).ToArray();
        }

        /// <summary>
        /// Create a <see cref="SceneContext"/> from IFC representation context.
        /// </summary>
        /// <param name="representationContext">The representation context.</param>
        /// <returns>A scene context</returns>
        public SceneContext CreateContextByIfcRepresentationContext(IIfcRepresentationContext representationContext)
        {
            return new SceneContext
            {
                Name = representationContext.ContextIdentifier.ToString().ToQualifier(),
                // Given in DEG => use as it is
                FDeflection = representationContext.Model.ModelFactors.DeflectionAngle,
                // Given internally in model units => convert to meter
                FTolerance = representationContext.Model.ModelFactors.LengthToMetresConversionFactor * representationContext.Model.ModelFactors.DeflectionTolerance,
            };
        }

        /// <summary>
        /// Active scene contexts.
        /// </summary>
        public SceneContext[] ActiveContexts 
        { 
            get => ContextCache.Values.Select(v => v.sceneContext).ToArray(); 
        }

        /// <summary>
        /// Context labels (as refered inside the IFC file).
        /// </summary>
        public int[] ActiveContextLabels 
        { 
            get => ContextCache.Keys.ToArray(); 
        }

        /// <summary>
        /// If true, the context is held by the cache.
        /// </summary>
        /// <param name="contextLabel">The context label (reference inside IFC)</param>
        /// <returns></returns>
        public bool IsInContext(int contextLabel) => ContextCache.ContainsKey(contextLabel);

        /// <summary>
        /// Creates an empty new <see cref="ComponentScene"/> from IFC project reference.
        /// </summary>
        /// <param name="p">The project</param>
        /// <returns>An empty initialized scene</returns>
        public ComponentScene CreateEmptySceneModelFromProject(IIfcProject p)
        {
            return new ComponentScene()
            {
                Metadata = new MetaData
                {
                    Name = p?.Name,
                    Stamp = Timestamp.FromDateTime(DateTime.Now.ToUniversalTime())
                },                
                Id = p?.GlobalId.ToGlobalUniqueId().ToQualifier()
            };
        }

        public IDictionary<int, SceneContext> ContextsCreateFromModel(IModel model, IGeometryStoreReader gReader)
        {
            return gReader.ContextIds
                   .Select(label => model.Instances[label])
                   .OfType<IIfcRepresentationContext>()
                   .ToDictionary(c => c.EntityLabel, c => new SceneContext { Name = c.ContextIdentifier.ToString().ToQualifier() });
        }

        public IDictionary<int, SceneContext> ContextsCreateFrom(IModel model, IGeometryStoreReader gReader, string[] contextIdentifiers)
        {
            return gReader.ContextIds
                   .Select(label => model.Instances[label])
                   .OfType<IIfcRepresentationContext>()
                   .Select(c => (c.EntityLabel, contextIdentifiers.FirstOrDefault(id => string.Equals(id, c.ContextIdentifier, StringComparison.OrdinalIgnoreCase))))
                   .Where(t => t.Item2 != null)
                   .ToDictionary(t => t.EntityLabel, t => new SceneContext { Name = t.Item2.ToQualifier() });
        }

        public IEnumerable<(int, SceneContext)> FilterActiveUserSceneContexts(IModel model, int[] contextLabels)
        {
            // Retrieve all context with geometry and match those to pregiven in settings
            return contextLabels
                   .Select(label => model.Instances[label])
                   .OfType<IIfcRepresentationContext>()
                   .Select(c => (c.EntityLabel, Settings.SelectedContext.FirstOrDefault(sc => sc.Name.IsSuperQualifierOf(c.ContextIdentifier.ToString().ToQualifier(), StringComparison.OrdinalIgnoreCase))))
                   .Where(t => t.Item2 != null);
        }

        // Compute contexts and related transformation
        public IEnumerable<SceneContextTransform> CreateSceneContextTransforms(IModelFactors factors, XbimContextRegionCollection contextRegions, IDictionary<int, SceneContext> contextTable)
        {
            foreach (var cr in contextRegions)
            {
                if (contextTable.TryGetValue(cr.ContextLabel, out SceneContext sc))
                {
                    XbimVector3D offset = XbimVector3D.Zero;
                    XbimVector3D mean = XbimVector3D.Zero;
                    foreach (var r in cr)
                    {
                        mean += r.Centre.ToXbimVector3D();
                        sc.Regions.Add(r.ToRegion(Scale));
                    }
                    mean *= 1.0 / cr.Count;

                    switch (Settings.Positioning)
                    {
                        case ScenePositioningStrategy.UserCorrection:
                            // Center at user's center
                            offset = Settings.UserModelCenter.ToXbimVector3DMeter(factors);
                            break;
                        case ScenePositioningStrategy.MostPopulatedRegionCorrection:
                            // Center at most populated
                            offset = cr.MostPopulated().Centre.ToXbimVector3D();
                            break;
                        case ScenePositioningStrategy.MostExtendedRegionCorrection:
                            // Center at largest
                            offset = cr.Largest().Centre.ToXbimVector3D();
                            break;
                        case ScenePositioningStrategy.MeanTranslationCorrection:
                            // Use mean correction
                            offset = mean;
                            break;
                        case ScenePositioningStrategy.SignificantPopulationCorrection:
                            var population = cr.Sum(r => r.Population);
                            XbimRegion rs = null;
                            double max = double.NegativeInfinity;
                            foreach (var r in cr)
                            {
                                // Compute weighted extent by relative population
                                double factor = r.Size.Length * r.Population / population;
                                if (max < factor)
                                {
                                    rs = r;
                                    max = factor;
                                }
                            }
                            offset = rs.Centre.ToXbimVector3D();
                            break;
                        case ScenePositioningStrategy.NoCorrection:
                            // No correction
                            _logger?.LogInformation($"No translation correction applied by settings to context '{cr.ContextLabel}'");
                            break;
                        default:
                            throw new NotImplementedException($"Missing implementation for '{Settings.Positioning}'");
                    }

                    switch (Settings.Transforming)
                    {
                        case SceneTransformationStrategy.Matrix:
                            // If Matrix or Global use rotation matrix representation
                            sc.Wcs = new XbimMatrix3D(offset).ToTransformM(Scale);
                            break;
                        case SceneTransformationStrategy.Quaternion:
                            // Otherwise use Quaternion representation
                            sc.Wcs = new XbimMatrix3D(offset).ToTransformQ(Scale);
                            break;
                        default:
                            throw new NotImplementedException($"{Settings.Transforming}");
                    }

                    // Set correction to negative offset shift (without scale since in model space units)
                    yield return new SceneContextTransform(cr.ContextLabel, sc, new XbimMatrix3D(offset * -1));
                }
                else
                {
                    _logger?.LogWarning("Excluding context label '{0}'. Not mentioned by settings.", cr.ContextLabel);
                }
            }
        }

    }
}
