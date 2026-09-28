using System.Reflection;
using UnityEngine;

namespace Game.Spline
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class ElectricCableWindVariation : MonoBehaviour
    {
        [Header("Wind Base")]
        [SerializeField] private bool applyOnEnable = true;
        [SerializeField] private bool applyInEditMode = true;
        [SerializeField] private bool overwriteExistingWindValues = true;
        [SerializeField] private float baseAmplitude = 0.04f;
        [SerializeField] private float baseSpeed = 3.32f;
        [SerializeField] private float baseSpatialFrequency = 0.14f;
        [SerializeField] private Vector3 baseDirection = new Vector3(1f, 0f, 0.25f);
        [SerializeField] [Range(0f, 1f)] private float endpointWindLock = 1f;

        [Header("Per Cable Variation")]
        [SerializeField] private int seedOffset;
        [SerializeField] [Range(0f, 1f)] private float amplitudeVariation = 0.45f;
        [SerializeField] [Range(0f, 1f)] private float speedVariation = 0.35f;
        [SerializeField] [Range(0f, 1f)] private float spatialFrequencyVariation = 0.5f;
        [SerializeField] [Range(0f, 90f)] private float directionYawVariationDegrees = 22f;
        [SerializeField] [Range(0f, 1f)] private float lineIndexOffsetStrength = 0.65f;

        private ElectricCableSpline cable;
#if UNITY_EDITOR
        private bool editorApplyQueued;
#endif

        private void OnEnable()
        {
            if (applyOnEnable)
            {
                RequestSafeApply();
            }
        }

        private void OnValidate()
        {
            baseAmplitude = Mathf.Max(0f, baseAmplitude);
            baseSpeed = Mathf.Max(0f, baseSpeed);
            baseSpatialFrequency = Mathf.Max(0f, baseSpatialFrequency);

            if (!Application.isPlaying && applyInEditMode)
            {
                RequestSafeApply();
            }
        }

        private void RequestSafeApply()
        {
            if (Application.isPlaying)
            {
                ApplyVariation();
                return;
            }

#if UNITY_EDITOR
            if (editorApplyQueued)
            {
                return;
            }

            editorApplyQueued = true;
            UnityEditor.EditorApplication.delayCall += RunQueuedEditorApply;
#endif
        }

#if UNITY_EDITOR
        private void RunQueuedEditorApply()
        {
            editorApplyQueued = false;
            if (this == null || Application.isPlaying || !isActiveAndEnabled || !applyInEditMode)
            {
                return;
            }

            ApplyVariation();
        }
#endif

        [ContextMenu("Apply Wind Variation")]
        public void ApplyVariation()
        {
            if (cable == null)
            {
                cable = GetComponent<ElectricCableSpline>();
            }

            if (cable == null)
            {
                return;
            }

            int seed = CalculateStableSeed();
            float r1 = Hash01(seed, 11);
            float r2 = Hash01(seed, 23);
            float r3 = Hash01(seed, 37);
            float r4 = Hash01(seed, 53);
            float r5 = Hash01(seed, 71);

            float amplitude = baseAmplitude * Mathf.Lerp(1f - amplitudeVariation, 1f + amplitudeVariation, r1);
            float speed = baseSpeed * Mathf.Lerp(1f - speedVariation, 1f + speedVariation, r2);
            float spatialFrequency = baseSpatialFrequency * Mathf.Lerp(1f - spatialFrequencyVariation, 1f + spatialFrequencyVariation, r3);

            float yaw = Mathf.Lerp(-directionYawVariationDegrees, directionYawVariationDegrees, r4);
            Vector3 direction = Quaternion.Euler(0f, yaw, 0f) * (baseDirection.sqrMagnitude > 0.0001f ? baseDirection.normalized : Vector3.right);

            // This offsets cables in the same group so parallel wires do not crest/trough together.
            // It works by slightly shifting frequency/speed per line while preserving the global wind feel.
            float lineOffset = Mathf.Lerp(-lineIndexOffsetStrength, lineIndexOffsetStrength, r5);
            speed = Mathf.Max(0f, speed + lineOffset * 0.23f);
            spatialFrequency = Mathf.Max(0.01f, spatialFrequency + lineOffset * 0.035f);

            if (!overwriteExistingWindValues && !LooksLikeDefaultWind())
            {
                return;
            }

            SetField("windSway", true);
            SetField("windAmplitude", amplitude);
            SetField("windSpeed", speed);
            SetField("windSpatialFrequency", spatialFrequency);
            SetField("windDirection", direction);
            SetField("endpointWindLock", endpointWindLock);
            cable.Rebuild();
        }

        private bool LooksLikeDefaultWind()
        {
            float currentAmplitude = GetFloat("windAmplitude", baseAmplitude);
            float currentSpeed = GetFloat("windSpeed", baseSpeed);
            float currentSpatialFrequency = GetFloat("windSpatialFrequency", baseSpatialFrequency);

            bool oldDefault = Mathf.Abs(currentAmplitude - 0.08f) < 0.002f &&
                              Mathf.Abs(currentSpeed - 1.3f) < 0.01f &&
                              Mathf.Abs(currentSpatialFrequency - 1.6f) < 0.01f;

            bool preferredDefault = Mathf.Abs(currentAmplitude - baseAmplitude) < 0.002f &&
                                    Mathf.Abs(currentSpeed - baseSpeed) < 0.01f &&
                                    Mathf.Abs(currentSpatialFrequency - baseSpatialFrequency) < 0.01f;

            return oldDefault || preferredDefault;
        }

        private int CalculateStableSeed()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + seedOffset;
                hash = hash * 31 + gameObject.name.GetHashCode();
                Vector3 p = transform.position;
                hash = hash * 31 + Mathf.RoundToInt(p.x * 10f);
                hash = hash * 31 + Mathf.RoundToInt(p.y * 10f);
                hash = hash * 31 + Mathf.RoundToInt(p.z * 10f);
                return hash;
            }
        }

        private static float Hash01(int seed, int salt)
        {
            unchecked
            {
                int x = seed + salt * 374761393;
                x = (x ^ (x >> 13)) * 1274126177;
                x ^= x >> 16;
                return (x & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        private float GetFloat(string fieldName, float fallback)
        {
            FieldInfo field = GetField(fieldName);
            return field != null && field.GetValue(cable) is float value ? value : fallback;
        }

        private void SetField(string fieldName, object value)
        {
            FieldInfo field = GetField(fieldName);
            if (field != null)
            {
                field.SetValue(cable, value);
            }
        }

        private static FieldInfo GetField(string fieldName)
        {
            return typeof(ElectricCableSpline).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }
    }
}
