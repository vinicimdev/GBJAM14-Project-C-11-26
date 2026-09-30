using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Spline
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ElectricPoleLocalBrokenCables : MonoBehaviour
    {
        public enum DamagePreset { None, OneLooseWire, TwoLooseWires, HangingJumper, MessyOldPole }
        public enum LocalCableType { HangingDrop, Jumper }

        [Serializable]
        public sealed class LocalBrokenCable
        {
            public bool enabled = true;
            public LocalCableType cableType = LocalCableType.HangingDrop;
            [Min(0)] public int fromSocketIndex;
            [Min(0)] public int toSocketIndex = 1;
            public Vector3 fromSocketOffset = new Vector3(0f, -0.03f, 0f);
            public Vector3 toSocketOffset = new Vector3(0f, -0.02f, 0f);
            [Min(2)] public int segments = 16;
            [Min(0f)] public float sag = 0.85f;
            public Vector3 sideOffset = new Vector3(0.08f, 0f, 0.02f);
            [Min(0.05f)] public float dropLength = 1.65f;
            public Vector3 looseEndOffset = new Vector3(0.12f, -1.65f, 0.04f);
            [Range(0f, 1f)] public float verticalGravity = 1.5f;
            [Range(0f, 1f)] public float curlAmount = 0.22f;
            [Min(0f)] public float poleClearance = 0.34f;
            [Range(0f, 1f)] public float poleClearanceFalloff = 0.35f;
        }

        [Header("References")]
        [SerializeField] private ElectricPoleCableHub hub;
        [SerializeField] private Transform generatedRoot;

        [Header("Local Broken Cables")]
        [SerializeField] private bool rebuildInEditMode = true;
        [SerializeField] private DamagePreset damagePreset = DamagePreset.TwoLooseWires;
        [SerializeField] private LocalBrokenCable[] cables =
        {
            new LocalBrokenCable { cableType = LocalCableType.HangingDrop, fromSocketIndex = 0, segments = 15, dropLength = 1.55f, looseEndOffset = new Vector3(0.1f, -1.55f, 0.03f) },
            new LocalBrokenCable { cableType = LocalCableType.HangingDrop, fromSocketIndex = 2, segments = 17, dropLength = 1.9f, looseEndOffset = new Vector3(-0.12f, -1.9f, -0.02f), sideOffset = new Vector3(-0.08f, 0f, -0.02f), curlAmount = 0.28f }
        };

        [Header("Visual")]
        [SerializeField] [Min(0.001f)] private float cableWidth = 0.035f;
        [SerializeField] private Material cableMaterial;
        [SerializeField] private bool inheritMaterialFromGeneratedCables = true;
        [SerializeField] private bool createFallbackMaterial = true;
        [SerializeField] private Color cableColor = new Color(0.005f, 0.005f, 0.004f, 1f);
        [SerializeField] [Range(0f, 1f)] private float cableSmoothness = 0.42f;
        [SerializeField] [Min(0)] private int capVertices = 5;
        [SerializeField] [Min(0)] private int cornerVertices = 4;

        [Header("Wind")]
        [SerializeField] private bool windSway = true;
        [SerializeField] [Min(0f)] private float windAmplitude = 0.018f;
        [SerializeField] [Min(0f)] private float windSpeed = 2.15f;
        [SerializeField] [Min(0f)] private float windSpatialFrequency = 0.32f;
        [SerializeField] private Vector3 windDirection = new Vector3(1f, 0f, 0.25f);

        private const string RootName = "__LocalBrokenCables";
        private const string CablePrefix = "LocalBrokenCable_";
        private Material generatedFallbackMaterial;
#if UNITY_EDITOR
        private bool editorRebuildQueued;
        private bool recordEditorUndo;
#endif

        private void Reset()
        {
            hub = GetComponent<ElectricPoleCableHub>();
            ApplyDamagePresetInternal(DamagePreset.TwoLooseWires, false);
            RequestSafeEditorRebuild();
        }

        private void OnEnable()
        {
            if (hub == null) hub = GetComponent<ElectricPoleCableHub>();
            if (CanModifyHierarchy())
            {
                if (Application.isPlaying) Rebuild(); else RequestSafeEditorRebuild();
            }
        }

        private void OnDisable() => DestroyGeneratedFallbackMaterial();

        private void OnValidate()
        {
            cableWidth = Mathf.Max(0.001f, cableWidth);
            cableSmoothness = Mathf.Clamp01(cableSmoothness);
            capVertices = Mathf.Max(0, capVertices);
            cornerVertices = Mathf.Max(0, cornerVertices);
            windAmplitude = Mathf.Max(0f, windAmplitude);
            windSpeed = Mathf.Max(0f, windSpeed);
            windSpatialFrequency = Mathf.Max(0f, windSpatialFrequency);

            if (cables != null)
            {
                foreach (LocalBrokenCable cable in cables)
                {
                    if (cable == null) continue;
                    cable.segments = Mathf.Max(2, cable.segments);
                    cable.sag = Mathf.Max(0f, cable.sag);
                    cable.dropLength = Mathf.Max(0.05f, cable.dropLength);
                    cable.verticalGravity = Mathf.Clamp01(cable.verticalGravity);
                    cable.curlAmount = Mathf.Clamp01(cable.curlAmount);
                    cable.poleClearance = Mathf.Max(0f, cable.poleClearance);
                    cable.poleClearanceFalloff = Mathf.Clamp01(cable.poleClearanceFalloff);
                }
            }

            UpdateGeneratedFallbackMaterial();
            if (!Application.isPlaying && rebuildInEditMode && CanModifyHierarchy()) RequestSafeEditorRebuild();
        }

        private void Update()
        {
            if (CanModifyHierarchy() && (Application.isPlaying || rebuildInEditMode)) Rebuild();
        }

        [ContextMenu("Apply Selected Damage Preset")]
        public void ApplySelectedDamagePreset() => ApplyDamagePreset(damagePreset);

        public void ApplyDamagePreset(DamagePreset preset)
        {
#if UNITY_EDITOR
            bool previousUndo = recordEditorUndo;
            recordEditorUndo = !Application.isPlaying;
            try
            {
                ApplyDamagePresetInternal(preset);
            }
            finally
            {
                recordEditorUndo = previousUndo;
            }
#else
            ApplyDamagePresetInternal(preset);
#endif
        }

        private void ApplyDamagePresetInternal(DamagePreset preset, bool rebuild = true)
        {
            damagePreset = preset;
            switch (preset)
            {
                case DamagePreset.None:
                    cables = Array.Empty<LocalBrokenCable>();
                    break;
                case DamagePreset.OneLooseWire:
                    cables = new[] { Hanging(0, 1.65f, new Vector3(0.12f, -1.65f, 0.04f), new Vector3(0.08f, 0f, 0.02f), 0.22f) };
                    break;
                case DamagePreset.TwoLooseWires:
                    cables = new[]
                    {
                        Hanging(0, 1.55f, new Vector3(0.1f, -1.55f, 0.03f), new Vector3(0.08f, 0f, 0.02f), 0.2f),
                        Hanging(2, 1.9f, new Vector3(-0.12f, -1.9f, -0.02f), new Vector3(-0.08f, 0f, -0.02f), 0.28f)
                    };
                    break;
                case DamagePreset.HangingJumper:
                    cables = new[] { Jumper(0, 2, 0.85f, new Vector3(0.1f, 0f, 0.02f)) };
                    break;
                case DamagePreset.MessyOldPole:
                    cables = new[]
                    {
                        Hanging(0, 2.05f, new Vector3(0.16f, -2.05f, 0.08f), new Vector3(0.12f, 0f, 0.04f), 0.34f),
                        Hanging(1, 1.55f, new Vector3(-0.14f, -1.55f, -0.06f), new Vector3(-0.1f, 0f, -0.03f), 0.24f),
                        Jumper(0, 2, 0.95f, new Vector3(0.04f, 0f, 0.09f))
                    };
                    break;
            }

            if (rebuild && CanModifyHierarchy()) Rebuild();
        }

        [ContextMenu("Randomize Local Damage")]
        public void RandomizeLocalDamage() => ApplyDamagePreset((DamagePreset)UnityEngine.Random.Range(0, 5));

        [ContextMenu("Clear Local Damage")]
        public void ClearLocalDamage()
        {
#if UNITY_EDITOR
            bool previousUndo = recordEditorUndo;
            recordEditorUndo = !Application.isPlaying;
            try
            {
                ApplyDamagePresetInternal(DamagePreset.None, false);
                if (CanModifyHierarchy()) ClearGenerated();
            }
            finally
            {
                recordEditorUndo = previousUndo;
            }
#else
            ApplyDamagePresetInternal(DamagePreset.None, false);
            if (CanModifyHierarchy()) ClearGenerated();
#endif
        }

        [ContextMenu("Rebuild Local Broken Cables")]
        public void Rebuild()
        {
            if (!CanModifyHierarchy()) return;
            if (hub == null) hub = GetComponent<ElectricPoleCableHub>();
            if (hub == null || cables == null) return;
            EnsureRoot();

            for (int i = 0; i < cables.Length; i++)
            {
                LocalBrokenCable definition = cables[i];
                if (definition == null || !definition.enabled)
                {
                    SetCableObjectVisible(i, false);
                    continue;
                }
                RebuildCable(i, definition);
            }

            HideExtraGeneratedObjects();
        }

        private void RequestSafeEditorRebuild()
        {
            if (Application.isPlaying)
            {
                Rebuild();
                return;
            }

#if UNITY_EDITOR
            if (editorRebuildQueued)
            {
                return;
            }

            editorRebuildQueued = true;
            UnityEditor.EditorApplication.delayCall += RunQueuedEditorRebuild;
#endif
        }

#if UNITY_EDITOR
        private void RunQueuedEditorRebuild()
        {
            editorRebuildQueued = false;
            if (this == null || Application.isPlaying || !isActiveAndEnabled ||
                !rebuildInEditMode || !CanModifyHierarchy())
            {
                return;
            }

            Rebuild();
        }
#endif

        [ContextMenu("Clear Local Broken Cables")]
        public void ClearGenerated()
        {
            if (!CanModifyHierarchy()) return;
            EnsureRoot();
            if (generatedRoot == null) return;
            for (int i = generatedRoot.childCount - 1; i >= 0; i--) DestroyObject(generatedRoot.GetChild(i).gameObject);
        }

        private static LocalBrokenCable Hanging(int socket, float drop, Vector3 end, Vector3 side, float curl)
        {
            return new LocalBrokenCable
            {
                cableType = LocalCableType.HangingDrop,
                fromSocketIndex = socket,
                segments = Mathf.Max(14, Mathf.RoundToInt(drop * 9f)),
                dropLength = drop,
                looseEndOffset = end,
                verticalGravity = 0.9f,
                curlAmount = curl,
                sideOffset = side,
                poleClearance = 0.34f,
                poleClearanceFalloff = 0.35f
            };
        }

        private static LocalBrokenCable Jumper(int from, int to, float sagValue, Vector3 side)
        {
            return new LocalBrokenCable
            {
                cableType = LocalCableType.Jumper,
                fromSocketIndex = from,
                toSocketIndex = to,
                segments = 18,
                sag = sagValue,
                sideOffset = side,
                fromSocketOffset = new Vector3(0f, -0.02f, 0f),
                toSocketOffset = new Vector3(0f, -0.02f, 0f)
            };
        }

        private void RebuildCable(int index, LocalBrokenCable definition)
        {
            Transform fromSocket = hub.GetSocket(definition.fromSocketIndex);
            if (fromSocket == null)
            {
                SetCableObjectVisible(index, false);
                return;
            }

            Transform toSocket = definition.cableType == LocalCableType.Jumper ? hub.GetSocket(definition.toSocketIndex) : null;
            if (definition.cableType == LocalCableType.Jumper && toSocket == null)
            {
                SetCableObjectVisible(index, false);
                return;
            }

            GameObject cableObject = GetOrCreateCableObject(index);
            LineRenderer line = cableObject.GetComponent<LineRenderer>();
            if (line == null) line = cableObject.AddComponent<LineRenderer>();
            ApplyLineSettings(line);

            int count = Mathf.Max(2, definition.segments);
            line.positionCount = count + 1;
            Vector3 start = fromSocket.TransformPoint(definition.fromSocketOffset);
            Vector3 end = definition.cableType == LocalCableType.Jumper
                ? toSocket.TransformPoint(definition.toSocketOffset)
                : CalculateHangingEnd(start, fromSocket, definition);
            float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;

            for (int i = 0; i <= count; i++)
            {
                float t = i / (float)count;
                line.SetPosition(i, definition.cableType == LocalCableType.HangingDrop
                    ? EvaluateHangingPoint(start, end, fromSocket, t, definition, time)
                    : EvaluateJumperPoint(start, end, t, definition, time));
            }
        }

        private GameObject GetOrCreateCableObject(int index)
        {
            string cableName = CablePrefix + (index + 1).ToString("00");
            Transform existing = generatedRoot.Find(cableName);
            GameObject cableObject = existing != null ? existing.gameObject : new GameObject(cableName);
#if UNITY_EDITOR
            if (existing == null && recordEditorUndo)
            {
                UnityEditor.Undo.RegisterCreatedObjectUndo(cableObject, "Create Local Broken Cable");
            }
#endif
            cableObject.transform.SetParent(generatedRoot, false);
            cableObject.transform.localPosition = Vector3.zero;
            cableObject.transform.localRotation = Quaternion.identity;
            cableObject.transform.localScale = Vector3.one;
            cableObject.SetActive(true);
            return cableObject;
        }

        private Vector3 CalculateHangingEnd(Vector3 start, Transform socket, LocalBrokenCable definition)
        {
            Vector3 local = definition.looseEndOffset;
            Vector3 horizontal = socket != null ? socket.TransformDirection(new Vector3(local.x, 0f, local.z)) : new Vector3(local.x, 0f, local.z);
            horizontal = Vector3.ProjectOnPlane(horizontal, Vector3.up);
            if (horizontal.sqrMagnitude < 0.0001f) horizontal = CalculatePoleAvoidanceDirection(start, socket) * Mathf.Max(0.08f, definition.poleClearance * 0.5f);
            float maxHorizontal = Mathf.Max(0.05f, definition.dropLength * 0.45f);
            if (horizontal.magnitude > maxHorizontal) horizontal = horizontal.normalized * maxHorizontal;
            return start + Vector3.down * definition.dropLength + horizontal;
        }

        private Vector3 EvaluateJumperPoint(Vector3 start, Vector3 end, float t, LocalBrokenCable definition, float time)
        {
            Vector3 point = Vector3.Lerp(start, end, t);
            float curve = 4f * t * (1f - t);
            point += Vector3.down * definition.sag * curve;
            point += definition.sideOffset * curve;
            return ApplyWind(point, t, curve, time);
        }

        private Vector3 EvaluateHangingPoint(Vector3 start, Vector3 end, Transform socket, float t, LocalBrokenCable definition, float time)
        {
            Vector3 straight = Vector3.Lerp(start, end, Smooth01(t));
            Vector3 gravityEnd = start + Vector3.down * definition.dropLength;
            Vector3 gravityPath = Vector3.Lerp(start, gravityEnd, Mathf.Pow(t, Mathf.Lerp(0.75f, 1.35f, definition.verticalGravity)));
            Vector3 point = Vector3.Lerp(straight, gravityPath, definition.verticalGravity * (1f - Mathf.Pow(t, 3f)));

            Vector3 side = socket != null ? socket.TransformDirection(definition.sideOffset) : definition.sideOffset;
            side = Vector3.ProjectOnPlane(side, Vector3.up);
            point += side * (Mathf.Sin(t * Mathf.PI * 0.85f) * Mathf.Clamp01(t * 1.6f));

            Vector3 avoid = CalculatePoleAvoidanceDirection(start, socket);
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / Mathf.Max(0.05f, definition.poleClearanceFalloff)));
            float fade = 1f - Mathf.SmoothStep(0.72f, 1f, t);
            point += avoid * (definition.poleClearance * rise * fade);

            Vector3 curlAxis = socket != null ? Vector3.ProjectOnPlane(socket.right, Vector3.up) : Vector3.ProjectOnPlane(transform.right, Vector3.up);
            if (curlAxis.sqrMagnitude < 0.0001f) curlAxis = avoid;
            point += curlAxis.normalized * (Mathf.Sin(t * Mathf.PI * 2.2f + 0.4f) * definition.curlAmount * t);

            if (point.y > start.y + 0.025f) point.y = start.y + 0.025f;
            return ApplyWind(point, t, Mathf.SmoothStep(0f, 1f, t), time);
        }

        private Vector3 ApplyWind(Vector3 point, float t, float mask, float time)
        {
            if (!windSway || windAmplitude <= 0f) return point;
            Vector3 wind = Vector3.ProjectOnPlane(windDirection.sqrMagnitude > 0.0001f ? windDirection.normalized : Vector3.right, Vector3.up);
            if (wind.sqrMagnitude < 0.0001f) wind = Vector3.right;
            float phase = t * windSpatialFrequency * Mathf.PI * 2f + time * windSpeed + transform.GetInstanceID() * 0.017f;
            float wave = Mathf.Sin(phase) + Mathf.Sin(phase * 0.51f + 2.2f) * 0.28f;
            return point + wind.normalized * (wave * windAmplitude * mask);
        }

        private Vector3 CalculatePoleAvoidanceDirection(Vector3 start, Transform socket)
        {
            Vector3 away = Vector3.ProjectOnPlane(start - transform.position, Vector3.up);
            if (away.sqrMagnitude > 0.0001f) return away.normalized;
            if (socket != null)
            {
                Vector3 forward = Vector3.ProjectOnPlane(socket.forward, Vector3.up);
                if (forward.sqrMagnitude > 0.0001f) return forward.normalized;
                Vector3 right = Vector3.ProjectOnPlane(socket.right, Vector3.up);
                if (right.sqrMagnitude > 0.0001f) return right.normalized;
            }
            Vector3 fallback = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.right;
        }

        private void ApplyLineSettings(LineRenderer line)
        {
            line.useWorldSpace = true;
            line.widthMultiplier = cableWidth;
            line.numCapVertices = capVertices;
            line.numCornerVertices = cornerVertices;
            line.shadowCastingMode = ShadowCastingMode.On;
            line.receiveShadows = true;
            line.startColor = cableColor;
            line.endColor = cableColor;
            Material resolved = ResolveCableMaterial();
            if (resolved != null) line.sharedMaterial = resolved;
        }

        private Material ResolveCableMaterial()
        {
            if (cableMaterial != null) return cableMaterial;
            if (inheritMaterialFromGeneratedCables)
            {
                Material inherited = FindGeneratedCableMaterial();
                if (inherited != null) return inherited;
            }
            return createFallbackMaterial ? EnsureFallbackMaterial() : null;
        }

        private Material FindGeneratedCableMaterial()
        {
            Transform searchRoot = transform.parent != null ? transform.parent : transform.root;
            if (searchRoot == null) return null;
            ElectricCableSpline[] generated = searchRoot.GetComponentsInChildren<ElectricCableSpline>(true);
            foreach (ElectricCableSpline cable in generated)
            {
                if (cable == null) continue;
                MeshRenderer mesh = cable.GetComponent<MeshRenderer>() ?? cable.GetComponentInChildren<MeshRenderer>(true);
                if (mesh != null && mesh.sharedMaterial != null) return mesh.sharedMaterial;
                LineRenderer line = cable.GetComponent<LineRenderer>() ?? cable.GetComponentInChildren<LineRenderer>(true);
                if (line != null && line.sharedMaterial != null) return line.sharedMaterial;
            }
            return null;
        }

        private Material EnsureFallbackMaterial()
        {
            Shader shader = FindCompatibleShader();
            if (shader == null) return null;
            if (generatedFallbackMaterial == null || generatedFallbackMaterial.shader != shader)
            {
                DestroyGeneratedFallbackMaterial();
                generatedFallbackMaterial = new Material(shader) { name = "Generated Local Broken Cable Material", hideFlags = HideFlags.HideAndDontSave };
            }
            UpdateGeneratedFallbackMaterial();
            return generatedFallbackMaterial;
        }

        private void UpdateGeneratedFallbackMaterial()
        {
            if (generatedFallbackMaterial == null) return;
            SetColorIfExists(generatedFallbackMaterial, "_BaseColor", cableColor);
            SetColorIfExists(generatedFallbackMaterial, "_Color", cableColor);
            SetFloatIfExists(generatedFallbackMaterial, "_Metallic", 0f);
            SetFloatIfExists(generatedFallbackMaterial, "_Smoothness", cableSmoothness);
            SetFloatIfExists(generatedFallbackMaterial, "_Glossiness", cableSmoothness);
        }

        private void DestroyGeneratedFallbackMaterial()
        {
            if (generatedFallbackMaterial == null) return;
            if (Application.isPlaying) Destroy(generatedFallbackMaterial); else DestroyImmediate(generatedFallbackMaterial);
            generatedFallbackMaterial = null;
        }

        private static Shader FindCompatibleShader()
        {
            string rp = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.GetType().Name : string.Empty;
            if (rp.Contains("HDRenderPipeline")) return FindFirstSupportedShader("HDRP/Lit", "HDRP/Unlit", "Sprites/Default");
            if (rp.Contains("UniversalRenderPipeline")) return FindFirstSupportedShader("Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit", "Sprites/Default");
            return FindFirstSupportedShader("Standard", "Unlit/Color", "Sprites/Default", "Diffuse");
        }

        private static Shader FindFirstSupportedShader(params string[] names)
        {
            foreach (string name in names)
            {
                Shader shader = Shader.Find(name);
                if (shader != null && shader.isSupported && shader.name != "Hidden/InternalErrorShader") return shader;
            }
            return null;
        }

        private static void SetColorIfExists(Material material, string property, Color value)
        {
            if (material != null && material.HasProperty(property)) material.SetColor(property, value);
        }

        private static void SetFloatIfExists(Material material, string property, float value)
        {
            if (material != null && material.HasProperty(property)) material.SetFloat(property, value);
        }

        private void SetCableObjectVisible(int index, bool visible)
        {
            if (generatedRoot == null) return;
            Transform child = generatedRoot.Find(CablePrefix + (index + 1).ToString("00"));
            if (child != null) child.gameObject.SetActive(visible);
        }

        private void HideExtraGeneratedObjects()
        {
            if (generatedRoot == null || cables == null) return;
            for (int i = generatedRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = generatedRoot.GetChild(i);
                if (!child.name.StartsWith(CablePrefix)) continue;
                if (int.TryParse(child.name.Substring(CablePrefix.Length), out int oneBased) && oneBased > cables.Length) child.gameObject.SetActive(false);
            }
        }

        private void EnsureRoot()
        {
            if (generatedRoot != null) return;
            Transform existing = transform.Find(RootName);
            if (existing != null)
            {
                generatedRoot = existing;
                return;
            }
            GameObject root = new GameObject(RootName);
#if UNITY_EDITOR
            if (recordEditorUndo)
            {
                UnityEditor.Undo.RegisterCreatedObjectUndo(root, "Create Local Broken Cable Root");
            }
#endif
            generatedRoot = root.transform;
            generatedRoot.SetParent(transform, false);
        }

        private bool CanModifyHierarchy()
        {
            return Application.isPlaying || (gameObject.scene.IsValid() && gameObject.scene.isLoaded);
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private void DestroyObject(GameObject objectToDestroy)
        {
            if (objectToDestroy == null) return;
            if (Application.isPlaying)
            {
                Destroy(objectToDestroy);
                return;
            }

#if UNITY_EDITOR
            if (recordEditorUndo)
            {
                UnityEditor.Undo.DestroyObjectImmediate(objectToDestroy);
                return;
            }
#endif
            DestroyImmediate(objectToDestroy);
        }
    }
}
