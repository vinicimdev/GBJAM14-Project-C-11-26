using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Spline
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ElectricPoleChainCableGenerator : MonoBehaviour
    {
        public enum DiscoveryMode
        {
            DirectChildren,
            ExplicitList
        }

        public enum QualityMode
        {
            Mobile,
            Balanced,
            HighQuality,
            Cinematic
        }

        [Header("Pole Discovery")]
        [SerializeField] private DiscoveryMode discoveryMode = DiscoveryMode.DirectChildren;
        [SerializeField] private ElectricPoleCableHub[] explicitPoles;
        [SerializeField] private bool includeInactivePoles;

        [Header("Cable Generation")]
        [SerializeField] private bool autoRebuildInEditMode = true;
        [SerializeField] private bool rebuildAtRuntimeStart = true;
        [SerializeField] private bool removeOldGeneratedCables = true;
        [SerializeField] private string generatedCablePrefix = "AutoCable";
        [SerializeField, HideInInspector] private string lastGeneratedCablePrefix = "AutoCable";
        [SerializeField] private bool generateBetweenConsecutivePoles = true;
        [SerializeField] private bool closeLoop;
        [SerializeField] private QualityMode qualityMode = QualityMode.Balanced;

        [Header("Auto Refresh")]
        [SerializeField] private bool autoRebuildWhenChildrenChange = true;
        [SerializeField] [Min(0.05f)] private float editorRebuildDelay = 0.2f;

        [Header("Cable Defaults")]
        [SerializeField] private int segments = 24;
        [SerializeField] private float sag = 0.45f;
        [SerializeField] private bool autoSagFromDistance = true;
        [SerializeField] private float sagPerMeter = 0.08f;
        [SerializeField] private float maxAutoSag = 2.5f;
        [SerializeField] private float cableWidth = 0.045f;
        [SerializeField] private int radialSegments = 8;
        [SerializeField] private Material cableMaterial;
        [SerializeField] private bool replaceUnsupportedMaterial = true;
        [SerializeField] private bool createPipelineCompatibleMaterialWhenEmpty = true;
        [SerializeField] private Color cableColor = new Color(0.005f, 0.005f, 0.004f, 1f);
        [SerializeField] [Range(0f, 1f)] private float cableSmoothness = 0.42f;

        [Header("Wind")]
        [SerializeField] private bool windSway = true;
        [SerializeField] private float windAmplitude = 0.04f;
        [SerializeField] private float windSpeed = 3.32f;
        [SerializeField] private float windSpatialFrequency = 0.14f;
        [SerializeField] private Vector3 windDirection = new Vector3(1f, 0f, 0.25f);
        [SerializeField] [Range(0f, 1f)] private float endpointWindLock = 1f;
        [SerializeField] private bool addPerCableWindVariation = true;

        [Header("Generated Cable LOD")]
        [SerializeField] private bool useDistanceLod = true;
        [SerializeField] private Camera lodCamera;
        [SerializeField] [Min(0f)] private float nearDistance = 35f;
        [SerializeField] [Min(0f)] private float midDistance = 80f;
        [SerializeField] [Min(0f)] private float farDistance = 140f;
        [SerializeField] [Min(2)] private int nearSegments = 24;
        [SerializeField] [Min(2)] private int midSegments = 14;
        [SerializeField] [Min(2)] private int farSegments = 8;
        [SerializeField] [Min(3)] private int nearRadialSegments = 8;
        [SerializeField] [Min(3)] private int midRadialSegments = 6;
        [SerializeField] [Min(3)] private int farRadialSegments = 4;
        [SerializeField] private bool disableBeyondFarDistance;

        [Header("Generated Cable Bake")]
        [SerializeField] private bool bakedKeepsWind = true;

        private readonly List<ElectricPoleCableHub> discoveredPoles = new List<ElectricPoleCableHub>();
        private int lastPoleSignature;
        private float nextEditorRebuildTime;
        private bool rebuildQueued;
        private bool isRebuilding;
        private Material generatedPipelineMaterial;

        private readonly struct CableEndpointKey : System.IEquatable<CableEndpointKey>
        {
            private readonly int firstId;
            private readonly int secondId;
            private readonly int pairIndex;
            private readonly int socketIndex;

            public CableEndpointKey(Transform first, Transform second, int pairIndex, int socketIndex)
            {
                int firstInstanceId = first != null ? first.GetInstanceID() : 0;
                int secondInstanceId = second != null ? second.GetInstanceID() : 0;
                firstId = Mathf.Min(firstInstanceId, secondInstanceId);
                secondId = Mathf.Max(firstInstanceId, secondInstanceId);
                this.pairIndex = pairIndex;
                this.socketIndex = socketIndex;
            }

            public bool Equals(CableEndpointKey other)
            {
                return firstId == other.firstId && secondId == other.secondId &&
                       pairIndex == other.pairIndex && socketIndex == other.socketIndex;
            }

            public override bool Equals(object obj)
            {
                return obj is CableEndpointKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = (firstId * 397) ^ secondId;
                    hash = (hash * 397) ^ pairIndex;
                    return (hash * 397) ^ socketIndex;
                }
            }
        }

        private void OnEnable()
        {
            EnsureCableMaterial();
            CaptureCurrentSignature();
            QueueRebuild();
        }

        private void OnDisable()
        {
            DestroyGeneratedMaterial();
        }

        private void Start()
        {
            EnsureCableMaterial();

            if (Application.isPlaying && rebuildAtRuntimeStart)
            {
                RebuildCables();
            }
        }

        private void Update()
        {
            if (Application.isPlaying || !autoRebuildInEditMode || !autoRebuildWhenChildrenChange || isRebuilding)
            {
                return;
            }

            if (HasPoleChainChanged())
            {
                QueueRebuild();
            }

            if (rebuildQueued && Time.realtimeSinceStartup >= nextEditorRebuildTime)
            {
                rebuildQueued = false;
                RebuildCables();
            }
        }

        private void OnTransformChildrenChanged()
        {
            if (!autoRebuildWhenChildrenChange || isRebuilding)
            {
                return;
            }

            QueueRebuild();
        }

        private void OnValidate()
        {
            ClampValues();
            UpdateGeneratedMaterialProperties();

            if (!Application.isPlaying && autoRebuildInEditMode && !isRebuilding)
            {
                QueueRebuild();
            }
        }

        [ContextMenu("Validate Setup")]
        public ElectricCableSetupReport ValidateSetup()
        {
            ElectricCableSetupReport report = new ElectricCableSetupReport();
            RefreshPoleList(discoveredPoles);

            if (discoveredPoles.Count == 0)
            {
                report.Error("No poles found. Put pole instances as direct children of this PoleChain, or use Explicit List mode.");
                return report;
            }

            if (discoveredPoles.Count == 1)
            {
                report.Warning("Only one pole found. At least two poles are required to generate cables between poles.");
            }
            else
            {
                report.Success(discoveredPoles.Count + " poles detected.");
            }

            int validPoleCount = 0;
            int minimumSocketCount = int.MaxValue;
            for (int i = 0; i < discoveredPoles.Count; i++)
            {
                ElectricPoleCableHub hub = discoveredPoles[i];
                if (hub == null)
                {
                    report.Error("Pole " + (i + 1) + " has no ElectricPoleCableHub.");
                    continue;
                }

                validPoleCount++;
                if (hub.SocketCount <= 0)
                {
                    report.Error(hub.name + " has no sockets assigned.");
                    continue;
                }

                minimumSocketCount = Mathf.Min(minimumSocketCount, hub.SocketCount);
                for (int socketIndex = 0; socketIndex < hub.SocketCount; socketIndex++)
                {
                    if (hub.GetSocket(socketIndex) == null)
                    {
                        report.Error(hub.name + " has a null socket at index " + socketIndex + ".");
                    }
                }
            }

            if (validPoleCount > 0 && minimumSocketCount != int.MaxValue)
            {
                report.Success("Minimum sockets per pole: " + minimumSocketCount + ".");
            }

            if (string.IsNullOrWhiteSpace(generatedCablePrefix))
            {
                report.Warning("Generated Cable Prefix is empty. Old generated cables cannot be identified safely.");
            }

            Material material = EnsureCableMaterial();
            if (material == null)
            {
                report.Warning("No cable material assigned and no compatible fallback material could be created.");
            }
            else if (!IsMaterialSupported(material))
            {
                report.Warning("Cable material shader is unsupported in the current render pipeline.");
            }
            else
            {
                report.Success("Cable material is valid for the current render pipeline.");
            }

            int existingCableCount = CountGeneratedCables();
            if (existingCableCount > 0)
            {
                report.Success(existingCableCount + " generated cables found.");
            }

            return report;
        }

        [ContextMenu("Fix Common Issues")]
        public void FixCommonIssues()
        {
            generatedCablePrefix = string.IsNullOrWhiteSpace(generatedCablePrefix) ? "AutoCable" : generatedCablePrefix;
            removeOldGeneratedCables = true;
            autoRebuildInEditMode = true;
            autoRebuildWhenChildrenChange = true;
            createPipelineCompatibleMaterialWhenEmpty = true;
            replaceUnsupportedMaterial = true;
            EnsureCableMaterial();
            ClampValues();
            ApplySettingsToExistingGeneratedCables();
            RebuildCables();
        }

        [ContextMenu("Apply Quality Mode")]
        public void ApplyQualityMode()
        {
            ApplyQualityMode(qualityMode);
        }

        public void ApplyQualityMode(QualityMode mode)
        {
            qualityMode = mode;
            switch (mode)
            {
                case QualityMode.Mobile:
                    segments = 12;
                    radialSegments = 5;
                    useDistanceLod = true;
                    nearDistance = 24f;
                    midDistance = 55f;
                    farDistance = 95f;
                    nearSegments = 12;
                    midSegments = 8;
                    farSegments = 4;
                    nearRadialSegments = 5;
                    midRadialSegments = 4;
                    farRadialSegments = 3;
                    windAmplitude = 0.025f;
                    disableBeyondFarDistance = true;
                    break;
                case QualityMode.Balanced:
                    segments = 24;
                    radialSegments = 8;
                    useDistanceLod = true;
                    nearDistance = 35f;
                    midDistance = 80f;
                    farDistance = 140f;
                    nearSegments = 24;
                    midSegments = 14;
                    farSegments = 8;
                    nearRadialSegments = 8;
                    midRadialSegments = 6;
                    farRadialSegments = 4;
                    windAmplitude = 0.04f;
                    disableBeyondFarDistance = false;
                    break;
                case QualityMode.HighQuality:
                    segments = 36;
                    radialSegments = 10;
                    useDistanceLod = true;
                    nearDistance = 45f;
                    midDistance = 110f;
                    farDistance = 180f;
                    nearSegments = 36;
                    midSegments = 22;
                    farSegments = 12;
                    nearRadialSegments = 10;
                    midRadialSegments = 8;
                    farRadialSegments = 5;
                    windAmplitude = 0.045f;
                    disableBeyondFarDistance = false;
                    break;
                case QualityMode.Cinematic:
                    segments = 48;
                    radialSegments = 12;
                    useDistanceLod = true;
                    nearDistance = 60f;
                    midDistance = 140f;
                    farDistance = 240f;
                    nearSegments = 48;
                    midSegments = 32;
                    farSegments = 18;
                    nearRadialSegments = 12;
                    midRadialSegments = 10;
                    farRadialSegments = 6;
                    windAmplitude = 0.05f;
                    disableBeyondFarDistance = false;
                    break;
            }

            ClampValues();
        }

        public void ApplyQualityModeAndRebuild(QualityMode mode)
        {
            ApplyQualityMode(mode);
            ApplySettingsToExistingGeneratedCables();
            RebuildCables();
        }

        [ContextMenu("Rebuild Pole Chain Cables")]
        public void RebuildCables()
        {
            if (isRebuilding)
            {
                return;
            }

            ClampValues();
            isRebuilding = true;
            try
            {
                Dictionary<CableEndpointKey, bool> normalOverrides = CaptureNormalOverrides();
                EnsureCableMaterial();
                RefreshPoleList(discoveredPoles);

                if (removeOldGeneratedCables)
                {
                    ClearGeneratedCables();
                }

                if (discoveredPoles.Count >= 2 && generateBetweenConsecutivePoles)
                {
                    for (int i = 0; i < discoveredPoles.Count - 1; i++)
                    {
                        GenerateBetween(discoveredPoles[i], discoveredPoles[i + 1], i, normalOverrides);
                    }

                    if (closeLoop && discoveredPoles.Count > 2)
                    {
                        GenerateBetween(discoveredPoles[discoveredPoles.Count - 1], discoveredPoles[0], discoveredPoles.Count - 1, normalOverrides);
                    }
                }

                CaptureCurrentSignature();
                lastGeneratedCablePrefix = generatedCablePrefix;
                rebuildQueued = false;
            }
            finally
            {
                isRebuilding = false;
            }
        }

        [ContextMenu("Apply Settings To Existing Generated Cables")]
        public void ApplySettingsToExistingGeneratedCables()
        {
            if (string.IsNullOrWhiteSpace(generatedCablePrefix))
            {
                return;
            }

            ElectricCableSpline[] cables = GetComponentsInChildren<ElectricCableSpline>(true);
            for (int i = 0; i < cables.Length; i++)
            {
                if (cables[i] == null || !IsGeneratedCableName(cables[i].name))
                {
                    continue;
                }

                ApplyCableSettings(cables[i]);
                cables[i].Rebuild();
            }
        }

        [ContextMenu("Clear Generated Cables")]
        public void ClearGeneratedCables()
        {
            if (string.IsNullOrWhiteSpace(generatedCablePrefix))
            {
                return;
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == null || !IsGeneratedCableName(child.name))
                {
                    continue;
                }

                DestroyObject(child.gameObject);
            }

            lastGeneratedCablePrefix = generatedCablePrefix;
        }

        public void RefreshPoleList(List<ElectricPoleCableHub> result)
        {
            result.Clear();

            if (discoveryMode == DiscoveryMode.ExplicitList)
            {
                if (explicitPoles == null)
                {
                    return;
                }

                for (int i = 0; i < explicitPoles.Length; i++)
                {
                    if (explicitPoles[i] != null)
                    {
                        result.Add(explicitPoles[i]);
                    }
                }

                return;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child == null || IsGeneratedCableName(child.name))
                {
                    continue;
                }

                if (!includeInactivePoles && !child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                ElectricPoleCableHub hub = child.GetComponentInChildren<ElectricPoleCableHub>(includeInactivePoles);
                if (hub != null)
                {
                    result.Add(hub);
                }
            }
        }

        private void GenerateBetween(
            ElectricPoleCableHub fromHub,
            ElectricPoleCableHub toHub,
            int pairIndex,
            Dictionary<CableEndpointKey, bool> normalOverrides)
        {
            if (fromHub == null || toHub == null)
            {
                return;
            }

            int socketCount = Mathf.Min(fromHub.SocketCount, toHub.SocketCount);
            for (int socketIndex = 0; socketIndex < socketCount; socketIndex++)
            {
                Transform fromSocket = fromHub.GetSocket(socketIndex);
                Transform toSocket = toHub.GetSocket(socketIndex);
                if (fromSocket == null || toSocket == null)
                {
                    continue;
                }

                GameObject cableObject = new GameObject($"{generatedCablePrefix}_{pairIndex + 1:00}_{socketIndex + 1:00}");
                cableObject.transform.SetParent(transform, false);

                ElectricCableSpline cable = cableObject.AddComponent<ElectricCableSpline>();
                CableEndpointKey endpointKey = new CableEndpointKey(fromSocket, toSocket, pairIndex, socketIndex);
                if (normalOverrides.TryGetValue(endpointKey, out bool normalsInverted))
                {
                    cable.SetNormalsInvertedWithoutRebuild(normalsInverted);
                }

                cable.StartPoint = fromSocket;
                cable.EndPoint = toSocket;
                ApplyCableSettings(cable);

                if (addPerCableWindVariation)
                {
                    ElectricCableWindVariation variation = cableObject.AddComponent<ElectricCableWindVariation>();
                    variation.ApplyVariation();
                }

                cable.Rebuild();
            }
        }

        private Dictionary<CableEndpointKey, bool> CaptureNormalOverrides()
        {
            Dictionary<CableEndpointKey, bool> result = new Dictionary<CableEndpointKey, bool>();
            if (string.IsNullOrWhiteSpace(generatedCablePrefix))
            {
                return result;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child == null || !IsGeneratedCableName(child.name))
                {
                    continue;
                }

                ElectricCableSpline cable = child.GetComponent<ElectricCableSpline>();
                if (cable == null || cable.StartPoint == null || cable.EndPoint == null)
                {
                    continue;
                }

                if (!TryParseGeneratedCableIndices(child.name, out int pairIndex, out int socketIndex))
                {
                    continue;
                }

                result[new CableEndpointKey(cable.StartPoint, cable.EndPoint, pairIndex, socketIndex)] = cable.NormalsInverted;
            }

            return result;
        }

        private static bool TryParseGeneratedCableIndices(string cableName, out int pairIndex, out int socketIndex)
        {
            pairIndex = -1;
            socketIndex = -1;
            if (string.IsNullOrEmpty(cableName))
            {
                return false;
            }

            int socketSeparator = cableName.LastIndexOf('_');
            int pairSeparator = socketSeparator > 0 ? cableName.LastIndexOf('_', socketSeparator - 1) : -1;
            if (pairSeparator < 0 || socketSeparator <= pairSeparator + 1)
            {
                return false;
            }

            if (!int.TryParse(cableName.Substring(pairSeparator + 1, socketSeparator - pairSeparator - 1), out int pairNumber) ||
                !int.TryParse(cableName.Substring(socketSeparator + 1), out int socketNumber) ||
                pairNumber <= 0 || socketNumber <= 0)
            {
                return false;
            }

            pairIndex = pairNumber - 1;
            socketIndex = socketNumber - 1;
            return true;
        }

        private void ApplyCableSettings(ElectricCableSpline cable)
        {
            if (cable == null)
            {
                return;
            }

            Material resolvedMaterial = EnsureCableMaterial();
            SerializedRuntimeSetter.Set(cable, "segments", segments);
            SerializedRuntimeSetter.Set(cable, "sag", sag);
            SerializedRuntimeSetter.Set(cable, "autoSagFromDistance", autoSagFromDistance);
            SerializedRuntimeSetter.Set(cable, "sagPerMeter", sagPerMeter);
            SerializedRuntimeSetter.Set(cable, "maxAutoSag", maxAutoSag);
            SerializedRuntimeSetter.Set(cable, "visualMode", ElectricCableSpline.CableVisualMode.MeshTube);
            SerializedRuntimeSetter.Set(cable, "cableWidth", cableWidth);
            SerializedRuntimeSetter.Set(cable, "radialSegments", radialSegments);
            SerializedRuntimeSetter.Set(cable, "cableMaterial", resolvedMaterial);
            SerializedRuntimeSetter.Set(cable, "cableColor", cableColor);
            SerializedRuntimeSetter.Set(cable, "windSway", windSway);
            SerializedRuntimeSetter.Set(cable, "windAmplitude", windAmplitude);
            SerializedRuntimeSetter.Set(cable, "windSpeed", windSpeed);
            SerializedRuntimeSetter.Set(cable, "windSpatialFrequency", windSpatialFrequency);
            SerializedRuntimeSetter.Set(cable, "windDirection", windDirection);
            SerializedRuntimeSetter.Set(cable, "endpointWindLock", endpointWindLock);
            SerializedRuntimeSetter.Set(cable, "useDistanceLod", useDistanceLod);
            SerializedRuntimeSetter.Set(cable, "lodCamera", lodCamera);
            SerializedRuntimeSetter.Set(cable, "nearDistance", nearDistance);
            SerializedRuntimeSetter.Set(cable, "midDistance", midDistance);
            SerializedRuntimeSetter.Set(cable, "farDistance", farDistance);
            SerializedRuntimeSetter.Set(cable, "nearSegments", nearSegments);
            SerializedRuntimeSetter.Set(cable, "midSegments", midSegments);
            SerializedRuntimeSetter.Set(cable, "farSegments", farSegments);
            SerializedRuntimeSetter.Set(cable, "nearRadialSegments", nearRadialSegments);
            SerializedRuntimeSetter.Set(cable, "midRadialSegments", midRadialSegments);
            SerializedRuntimeSetter.Set(cable, "farRadialSegments", farRadialSegments);
            SerializedRuntimeSetter.Set(cable, "disableBeyondFarDistance", disableBeyondFarDistance);
            SerializedRuntimeSetter.Set(cable, "bakedKeepsWind", bakedKeepsWind);
            SerializedRuntimeSetter.Set(cable, "brokenCable", false);
        }

        private int CountGeneratedCables()
        {
            if (string.IsNullOrWhiteSpace(generatedCablePrefix))
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child != null && IsGeneratedCableName(child.name) && child.GetComponent<ElectricCableSpline>() != null)
                {
                    count++;
                }
            }

            return count;
        }

        private void ClampValues()
        {
            generatedCablePrefix = string.IsNullOrWhiteSpace(generatedCablePrefix) ? "AutoCable" : generatedCablePrefix;
            lastGeneratedCablePrefix = string.IsNullOrWhiteSpace(lastGeneratedCablePrefix) ? generatedCablePrefix : lastGeneratedCablePrefix;
            segments = Mathf.Max(2, segments);
            sag = Mathf.Max(0f, sag);
            sagPerMeter = Mathf.Max(0f, sagPerMeter);
            maxAutoSag = Mathf.Max(0f, maxAutoSag);
            cableWidth = Mathf.Max(0.001f, cableWidth);
            radialSegments = Mathf.Max(3, radialSegments);
            windAmplitude = Mathf.Max(0f, windAmplitude);
            windSpeed = Mathf.Max(0f, windSpeed);
            windSpatialFrequency = Mathf.Max(0f, windSpatialFrequency);
            endpointWindLock = Mathf.Clamp01(endpointWindLock);
            editorRebuildDelay = Mathf.Max(0.05f, editorRebuildDelay);
            cableSmoothness = Mathf.Clamp01(cableSmoothness);
            nearDistance = Mathf.Max(0f, nearDistance);
            midDistance = Mathf.Max(nearDistance, midDistance);
            farDistance = Mathf.Max(midDistance, farDistance);
            nearSegments = Mathf.Max(2, nearSegments);
            midSegments = Mathf.Max(2, midSegments);
            farSegments = Mathf.Max(2, farSegments);
            nearRadialSegments = Mathf.Max(3, nearRadialSegments);
            midRadialSegments = Mathf.Max(3, midRadialSegments);
            farRadialSegments = Mathf.Max(3, farRadialSegments);
        }

        private bool IsGeneratedCableName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            return (!string.IsNullOrWhiteSpace(generatedCablePrefix) && objectName.StartsWith(generatedCablePrefix)) ||
                   (!string.IsNullOrWhiteSpace(lastGeneratedCablePrefix) && objectName.StartsWith(lastGeneratedCablePrefix));
        }

        private Material EnsureCableMaterial()
        {
            if (cableMaterial != null && (!replaceUnsupportedMaterial || IsMaterialSupported(cableMaterial)))
            {
                return cableMaterial;
            }

            if (!createPipelineCompatibleMaterialWhenEmpty)
            {
                return cableMaterial;
            }

            Shader shader = FindCompatibleShader();
            if (shader == null)
            {
                return cableMaterial;
            }

            if (generatedPipelineMaterial == null || generatedPipelineMaterial.shader != shader)
            {
                DestroyGeneratedMaterial();
                generatedPipelineMaterial = new Material(shader)
                {
                    name = "Generated Black Plastic Cable Material",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            UpdateGeneratedMaterialProperties();
            return generatedPipelineMaterial;
        }

        private void UpdateGeneratedMaterialProperties()
        {
            if (generatedPipelineMaterial == null)
            {
                return;
            }

            SetColorIfExists(generatedPipelineMaterial, "_BaseColor", cableColor);
            SetColorIfExists(generatedPipelineMaterial, "_Color", cableColor);
            SetFloatIfExists(generatedPipelineMaterial, "_Metallic", 0f);
            SetFloatIfExists(generatedPipelineMaterial, "_Smoothness", cableSmoothness);
            SetFloatIfExists(generatedPipelineMaterial, "_Glossiness", cableSmoothness);
            SetFloatIfExists(generatedPipelineMaterial, "_AlphaCutoff", 0.5f);
            SetFloatIfExists(generatedPipelineMaterial, "_Surface", 0f);
            generatedPipelineMaterial.doubleSidedGI = false;
        }

        private static bool IsMaterialSupported(Material material)
        {
            return material != null && material.shader != null && material.shader.isSupported && material.shader.name != "Hidden/InternalErrorShader";
        }

        private static Shader FindCompatibleShader()
        {
            string renderPipelineName = GraphicsSettings.currentRenderPipeline != null
                ? GraphicsSettings.currentRenderPipeline.GetType().Name
                : string.Empty;

            if (renderPipelineName.Contains("HDRenderPipelineAsset") || renderPipelineName.Contains("HDRenderPipeline"))
            {
                return FindFirstSupportedShader("HDRP/Lit", "HDRP/Unlit", "Hidden/InternalErrorShader");
            }

            if (renderPipelineName.Contains("UniversalRenderPipelineAsset") || renderPipelineName.Contains("UniversalRenderPipeline"))
            {
                return FindFirstSupportedShader("Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit", "Sprites/Default");
            }

            return FindFirstSupportedShader("Standard", "Unlit/Color", "Sprites/Default", "Diffuse");
        }

        private static Shader FindFirstSupportedShader(params string[] shaderNames)
        {
            for (int i = 0; i < shaderNames.Length; i++)
            {
                Shader shader = Shader.Find(shaderNames[i]);
                if (shader != null && shader.isSupported && shader.name != "Hidden/InternalErrorShader")
                {
                    return shader;
                }
            }

            return null;
        }

        private static void SetColorIfExists(Material material, string propertyName, Color value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, value);
            }
        }

        private static void SetFloatIfExists(Material material, string propertyName, float value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private void DestroyGeneratedMaterial()
        {
            if (generatedPipelineMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(generatedPipelineMaterial);
            }
            else
            {
                DestroyImmediate(generatedPipelineMaterial);
            }

            generatedPipelineMaterial = null;
        }

        private bool HasPoleChainChanged()
        {
            int currentSignature = CalculatePoleSignature();
            return currentSignature != lastPoleSignature;
        }

        private int CalculatePoleSignature()
        {
            unchecked
            {
                int signature = 17;
                RefreshPoleList(discoveredPoles);
                signature = signature * 31 + discoveredPoles.Count;

                for (int i = 0; i < discoveredPoles.Count; i++)
                {
                    ElectricPoleCableHub hub = discoveredPoles[i];
                    signature = signature * 31 + (hub != null ? hub.GetInstanceID() : 0);
                    signature = signature * 31 + (hub != null ? hub.SocketCount : 0);
                    if (hub == null)
                    {
                        continue;
                    }

                    for (int socketIndex = 0; socketIndex < hub.SocketCount; socketIndex++)
                    {
                        Transform socket = hub.GetSocket(socketIndex);
                        if (socket == null)
                        {
                            continue;
                        }

                        Vector3 position = socket.position;
                        Quaternion rotation = socket.rotation;
                        signature = signature * 31 + Mathf.RoundToInt(position.x * 100f);
                        signature = signature * 31 + Mathf.RoundToInt(position.y * 100f);
                        signature = signature * 31 + Mathf.RoundToInt(position.z * 100f);
                        signature = signature * 31 + Mathf.RoundToInt(rotation.eulerAngles.x * 10f);
                        signature = signature * 31 + Mathf.RoundToInt(rotation.eulerAngles.y * 10f);
                        signature = signature * 31 + Mathf.RoundToInt(rotation.eulerAngles.z * 10f);
                    }
                }

                return signature;
            }
        }

        private void CaptureCurrentSignature()
        {
            lastPoleSignature = CalculatePoleSignature();
        }

        private void QueueRebuild()
        {
            if (Application.isPlaying || isRebuilding)
            {
                return;
            }

            rebuildQueued = true;
            nextEditorRebuildTime = Time.realtimeSinceStartup + editorRebuildDelay;
        }

        private static void DestroyObject(GameObject objectToDestroy)
        {
            if (objectToDestroy == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(objectToDestroy);
            }
            else
            {
                DestroyImmediate(objectToDestroy);
            }
        }

        private static class SerializedRuntimeSetter
        {
            public static void Set<T>(Object target, string fieldName, T value)
            {
                if (target == null || string.IsNullOrWhiteSpace(fieldName))
                {
                    return;
                }

                System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (field == null)
                {
                    return;
                }

                field.SetValue(target, value);
            }
        }
    }
}
