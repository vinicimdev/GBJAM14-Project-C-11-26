using UnityEngine;

namespace Game.Spline
{
    [CreateAssetMenu(fileName = "ElectricCableProfile", menuName = "Polyzero/Spline/Electric Cable Profile")]
    public sealed class ElectricCableProfile : ScriptableObject
    {
        [Header("Cable Shape")]
        [Min(2)] public int segments = 24;
        [Min(0f)] public float sag = 0.45f;
        public bool autoSagFromDistance = true;
        [Min(0f)] public float sagPerMeter = 0.08f;
        [Min(0f)] public float maxAutoSag = 2.5f;

        [Header("Visual")]
        [Min(0.001f)] public float cableWidth = 0.045f;
        [Min(3)] public int radialSegments = 8;
        public Material cableMaterial;
        public bool replaceUnsupportedMaterial = true;
        public bool createPipelineCompatibleMaterialWhenEmpty = true;
        public Color cableColor = new Color(0.005f, 0.005f, 0.004f, 1f);
        [Range(0f, 1f)] public float cableSmoothness = 0.42f;

        [Header("Wind")]
        public bool windSway = true;
        [Min(0f)] public float windAmplitude = 0.04f;
        [Min(0f)] public float windSpeed = 3.32f;
        [Min(0f)] public float windSpatialFrequency = 0.14f;
        public Vector3 windDirection = new Vector3(1f, 0f, 0.25f);
        public bool addPerCableWindVariation = true;

        [Header("LOD")]
        public bool useDistanceLod = true;
        [Min(0f)] public float nearDistance = 35f;
        [Min(0f)] public float midDistance = 80f;
        [Min(0f)] public float farDistance = 140f;
        [Min(2)] public int nearSegments = 24;
        [Min(2)] public int midSegments = 14;
        [Min(2)] public int farSegments = 8;
        [Min(3)] public int nearRadialSegments = 8;
        [Min(3)] public int midRadialSegments = 6;
        [Min(3)] public int farRadialSegments = 4;
        public bool disableBeyondFarDistance;

        [Header("Broken Cable")]
        public bool useBreakGapMeters = true;
        [Min(0.05f)] public float breakGapMeters = 0.55f;
        [Range(0.01f, 0.35f)] public float breakGapNormalized = 0.035f;
        [Min(0.05f)] public float brokenLooseEndLength = 1.25f;
        [Min(0f)] public float brokenEndDrop = 0.75f;
        [Min(0f)] public float brokenEndCurl = 0.18f;
        public Vector3 brokenEndSideOffset = new Vector3(0.08f, 0f, 0f);

        private void OnValidate()
        {
            segments = Mathf.Max(2, segments);
            sag = Mathf.Max(0f, sag);
            sagPerMeter = Mathf.Max(0f, sagPerMeter);
            maxAutoSag = Mathf.Max(0f, maxAutoSag);
            cableWidth = Mathf.Max(0.001f, cableWidth);
            radialSegments = Mathf.Max(3, radialSegments);
            cableSmoothness = Mathf.Clamp01(cableSmoothness);
            windAmplitude = Mathf.Max(0f, windAmplitude);
            windSpeed = Mathf.Max(0f, windSpeed);
            windSpatialFrequency = Mathf.Max(0f, windSpatialFrequency);
            nearDistance = Mathf.Max(0f, nearDistance);
            midDistance = Mathf.Max(nearDistance, midDistance);
            farDistance = Mathf.Max(midDistance, farDistance);
            nearSegments = Mathf.Max(2, nearSegments);
            midSegments = Mathf.Max(2, midSegments);
            farSegments = Mathf.Max(2, farSegments);
            nearRadialSegments = Mathf.Max(3, nearRadialSegments);
            midRadialSegments = Mathf.Max(3, midRadialSegments);
            farRadialSegments = Mathf.Max(3, farRadialSegments);
            breakGapMeters = Mathf.Max(0.05f, breakGapMeters);
            brokenLooseEndLength = Mathf.Max(0.05f, brokenLooseEndLength);
            brokenEndDrop = Mathf.Max(0f, brokenEndDrop);
            brokenEndCurl = Mathf.Max(0f, brokenEndCurl);
        }
    }
}
