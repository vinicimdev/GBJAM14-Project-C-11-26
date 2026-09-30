using System.Reflection;
using UnityEngine;

namespace Game.Spline
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    public sealed class ElectricCableBrokenVisualOverride : MonoBehaviour
    {
        [Header("Broken Visual Override")]
        [SerializeField] private bool enableOverride = true;
        [SerializeField] private bool useMeterBasedGap = true;
        [SerializeField] [Min(0.05f)] private float breakGapMeters = 0.55f;
        [SerializeField] [Min(0.05f)] private float looseEndLength = 1.4f;
        [SerializeField] [Min(0f)] private float looseEndDrop = 0.75f;
        [SerializeField] [Min(0f)] private float looseEndCurl = 0.22f;
        [SerializeField] private Vector3 looseEndSideOffset = new Vector3(0.08f, 0f, 0f);
        [SerializeField] [Min(3)] private int radialSegmentsOverride = 0;
        [SerializeField] [Min(2)] private int segmentsOverride = 0;

        [Header("Wind Defaults")]
        [SerializeField] private bool applyPreferredWindDefaults = true;
        [SerializeField] private float preferredWindAmplitude = 0.04f;
        [SerializeField] private float preferredWindSpeed = 3.32f;
        [SerializeField] private float preferredWindSpatialFrequency = 0.14f;
        [SerializeField] private Vector3 preferredWindDirection = new Vector3(1f, 0f, 0.25f);
        [SerializeField] [Range(0f, 1f)] private float preferredEndpointWindLock = 1f;

        private const string VisualName = "__BrokenCableVisualOverride";
        private ElectricCableSpline cable;
        private MeshFilter overrideMeshFilter;
        private MeshRenderer overrideMeshRenderer;
        private Mesh overrideMesh;
        private MeshRenderer originalMeshRenderer;
        private LineRenderer originalLineRenderer;
        private Vector3[] vertices;
        private Vector3[] normals;
        private Vector2[] uvs;
        private int[] triangles;
        private Vector3[] frameTangents;
        private Vector3[] frameNormals;
        private Vector3[] frameBinormals;

        private void OnEnable()
        {
            CacheReferences();
            ApplyWindDefaultsIfOldValues();
            Rebuild();
        }

        private void OnDisable()
        {
            SetOriginalVisible(true);
            SetOverrideVisible(false);
        }

        private void OnValidate()
        {
            breakGapMeters = Mathf.Max(0.05f, breakGapMeters);
            looseEndLength = Mathf.Max(0.05f, looseEndLength);
            looseEndDrop = Mathf.Max(0f, looseEndDrop);
            looseEndCurl = Mathf.Max(0f, looseEndCurl);
            radialSegmentsOverride = Mathf.Max(0, radialSegmentsOverride);
            segmentsOverride = Mathf.Max(0, segmentsOverride);
            ApplyWindDefaultsIfOldValues();
            Rebuild();
        }

        private void LateUpdate()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            CacheReferences();
            if (!enableOverride || cable == null || !IsBrokenCableEnabled() ||
                !cable.TryGetEndpoints(out Vector3 start, out Vector3 end) ||
                (end - start).sqrMagnitude <= 0.00000001f)
            {
                SetOriginalVisible(true);
                SetOverrideVisible(false);
                return;
            }

            SetOriginalVisible(false);
            EnsureOverrideVisual();
            SetOverrideVisible(true);
            BuildBrokenMesh(start, end);
        }

        private void CacheReferences()
        {
            if (cable == null)
            {
                cable = GetComponent<ElectricCableSpline>();
            }

            if (originalMeshRenderer == null)
            {
                originalMeshRenderer = GetComponent<MeshRenderer>();
            }

            if (originalLineRenderer == null)
            {
                originalLineRenderer = GetComponent<LineRenderer>();
            }
        }

        private void EnsureOverrideVisual()
        {
            if (overrideMeshFilter != null && overrideMeshRenderer != null)
            {
                return;
            }

            Transform existing = transform.Find(VisualName);
            GameObject visualObject = existing != null ? existing.gameObject : new GameObject(VisualName);
            visualObject.transform.SetParent(transform, false);
            visualObject.transform.localPosition = Vector3.zero;
            visualObject.transform.localRotation = Quaternion.identity;
            visualObject.transform.localScale = Vector3.one;

            overrideMeshFilter = visualObject.GetComponent<MeshFilter>();
            if (overrideMeshFilter == null)
            {
                overrideMeshFilter = visualObject.AddComponent<MeshFilter>();
            }

            overrideMeshRenderer = visualObject.GetComponent<MeshRenderer>();
            if (overrideMeshRenderer == null)
            {
                overrideMeshRenderer = visualObject.AddComponent<MeshRenderer>();
            }

            if (overrideMesh == null)
            {
                overrideMesh = new Mesh
                {
                    name = "Improved Broken Electric Cable Mesh"
                };
                overrideMesh.MarkDynamic();
            }

            overrideMeshFilter.sharedMesh = overrideMesh;
            if (originalMeshRenderer != null && originalMeshRenderer.sharedMaterial != null)
            {
                overrideMeshRenderer.sharedMaterial = originalMeshRenderer.sharedMaterial;
            }

            overrideMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            overrideMeshRenderer.receiveShadows = true;
        }

        private void SetOriginalVisible(bool visible)
        {
            if (originalMeshRenderer != null)
            {
                originalMeshRenderer.enabled = visible;
            }

            if (originalLineRenderer != null)
            {
                originalLineRenderer.enabled = visible;
            }
        }

        private void SetOverrideVisible(bool visible)
        {
            if (overrideMeshRenderer != null)
            {
                overrideMeshRenderer.enabled = visible;
            }
        }

        private void BuildBrokenMesh(Vector3 start, Vector3 end)
        {
            EnsureOverrideVisual();
            if (overrideMesh == null)
            {
                return;
            }

            if ((end - start).sqrMagnitude <= 0.00000001f)
            {
                overrideMesh.Clear();
                return;
            }

            float cableLength = Mathf.Max(0.001f, Vector3.Distance(start, end));
            float breakT = GetFloat("breakNormalizedPosition", 0.5f);
            float fullGapNormalized = useMeterBasedGap ? breakGapMeters / cableLength : GetFloat("breakGapNormalized", 0.035f);
            fullGapNormalized = Mathf.Clamp(fullGapNormalized, 0.01f, 0.16f);
            float halfGap = fullGapNormalized * 0.5f;
            float leftEnd = Mathf.Clamp01(breakT - halfGap);
            float rightStart = Mathf.Clamp01(breakT + halfGap);

            if (leftEnd <= 0.02f || rightStart >= 0.98f || leftEnd >= rightStart)
            {
                overrideMesh.Clear();
                return;
            }

            int baseSegments = segmentsOverride > 0 ? segmentsOverride : Mathf.Max(10, GetInt("segments", 24));
            int radialSegments = radialSegmentsOverride > 0 ? radialSegmentsOverride : Mathf.Max(5, GetInt("radialSegments", 8));
            int leftSegments = Mathf.Max(4, Mathf.RoundToInt(baseSegments * leftEnd));
            int rightSegments = Mathf.Max(4, Mathf.RoundToInt(baseSegments * (1f - rightStart)));

            Vector3[] left = BuildSpan(start, end, 0f, leftEnd, leftSegments, -1f, cableLength);
            Vector3[] right = BuildSpan(start, end, rightStart, 1f, rightSegments, 1f, cableLength);
            BuildTubeMesh(new[] { left, right }, radialSegments);
        }

        private Vector3[] BuildSpan(Vector3 start, Vector3 end, float t0, float t1, int segmentCount, float brokenSign, float cableLength)
        {
            Vector3[] points = new Vector3[segmentCount + 1];
            float resolvedSag = ResolveSag(start, end);
            float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
            float looseLengthNormalized = Mathf.Clamp01(looseEndLength / cableLength);

            for (int i = 0; i <= segmentCount; i++)
            {
                float localT = i / (float)segmentCount;
                float t = Mathf.Lerp(t0, t1, localT);
                float looseInfluence;

                if (brokenSign < 0f)
                {
                    looseInfluence = Mathf.InverseLerp(Mathf.Max(t0, t1 - looseLengthNormalized), t1, t);
                }
                else
                {
                    looseInfluence = 1f - Mathf.InverseLerp(t0, Mathf.Min(t1, t0 + looseLengthNormalized), t);
                }

                points[i] = EvaluatePoint(start, end, t, resolvedSag, time, Smooth01(looseInfluence), brokenSign);
            }

            return points;
        }

        private Vector3 EvaluatePoint(Vector3 start, Vector3 end, float t, float resolvedSag, float time, float looseInfluence, float brokenSign)
        {
            Vector3 point = Vector3.Lerp(start, end, t);
            float sagCurve = 4f * t * (1f - t);
            point += Vector3.down * (resolvedSag * sagCurve);
            point += GetVector3("worldSideOffset", Vector3.zero) * sagCurve;

            if (looseInfluence > 0f)
            {
                Vector3 cableDirection = (end - start).sqrMagnitude > 0.0001f ? (end - start).normalized : transform.forward;
                Vector3 curlAxis = Vector3.Cross(Vector3.up, cableDirection);
                if (curlAxis.sqrMagnitude < 0.0001f)
                {
                    curlAxis = transform.right;
                }

                curlAxis.Normalize();
                point += Vector3.down * (looseEndDrop * looseInfluence);
                point += looseEndSideOffset * (brokenSign * looseInfluence);
                point += curlAxis * (looseEndCurl * brokenSign * Mathf.Sin(looseInfluence * Mathf.PI * 0.5f));
            }

            if (GetBool("windSway", true) && GetFloat("windAmplitude", preferredWindAmplitude) > 0f)
            {
                Vector3 wind = GetVector3("windDirection", preferredWindDirection);
                wind = wind.sqrMagnitude > 0.0001f ? wind.normalized : Vector3.right;
                float windAmplitude = GetFloat("windAmplitude", preferredWindAmplitude);
                float windSpeed = GetFloat("windSpeed", preferredWindSpeed);
                float windSpatialFrequency = GetFloat("windSpatialFrequency", preferredWindSpatialFrequency);
                float endpointWindLock = Mathf.Clamp01(GetFloat("endpointWindLock", preferredEndpointWindLock));
                float endpointMask = Mathf.Pow(sagCurve, Mathf.Lerp(1f, 3f, endpointWindLock));
                float phase = t * windSpatialFrequency * Mathf.PI * 2f + time * windSpeed;
                float wave = Mathf.Sin(phase) + Mathf.Sin(phase * 0.43f + 1.7f) * 0.35f;
                point += wind * (wave * windAmplitude * endpointMask);
            }

            return point;
        }

        private float ResolveSag(Vector3 start, Vector3 end)
        {
            float sag = GetFloat("sag", 0.65f);
            if (!GetBool("autoSagFromDistance", true))
            {
                return sag;
            }

            return Mathf.Min(GetFloat("maxAutoSag", 2.5f), sag + Vector3.Distance(start, end) * GetFloat("sagPerMeter", 0.08f));
        }

        private void BuildTubeMesh(Vector3[][] spans, int radialSegments)
        {
            int totalRings = 0;
            int totalSegments = 0;
            for (int i = 0; i < spans.Length; i++)
            {
                spans[i] = RemoveConsecutiveDuplicatePoints(spans[i]);
                if (spans[i] == null || spans[i].Length < 2)
                {
                    continue;
                }

                totalRings += spans[i].Length;
                totalSegments += spans[i].Length - 1;
            }

            if (totalRings <= 1)
            {
                overrideMesh.Clear();
                return;
            }

            int vertexCount = totalRings * radialSegments;
            int triangleCount = totalSegments * radialSegments * 6;
            if (vertices == null || vertices.Length != vertexCount)
            {
                vertices = new Vector3[vertexCount];
                normals = new Vector3[vertexCount];
                uvs = new Vector2[vertexCount];
            }

            if (triangles == null || triangles.Length != triangleCount)
            {
                triangles = new int[triangleCount];
            }

            float radius = Mathf.Max(0.001f, GetFloat("cableWidth", 0.045f)) * 0.5f;
            bool invertNormals = cable != null && cable.NormalsInverted;
            Matrix4x4 worldNormalToLocal = transform.localToWorldMatrix.transpose;
            int vertexCursor = 0;
            int triangleCursor = 0;

            for (int spanIndex = 0; spanIndex < spans.Length; spanIndex++)
            {
                Vector3[] points = spans[spanIndex];
                if (points == null || points.Length < 2)
                {
                    continue;
                }

                int spanVertexStart = vertexCursor;
                float totalLength = CalculatePolylineLength(points);
                float traversedLength = 0f;
                BuildFrames(points, out Vector3[] spanNormals, out Vector3[] spanBinormals);

                for (int ring = 0; ring < points.Length; ring++)
                {
                    Vector3 point = points[ring];
                    Vector3 normal = spanNormals[ring];
                    Vector3 binormal = spanBinormals[ring];

                    if (ring > 0)
                    {
                        traversedLength += Vector3.Distance(points[ring - 1], point);
                    }

                    float v = totalLength > 0.0001f ? traversedLength / totalLength : 0f;
                    for (int side = 0; side < radialSegments; side++)
                    {
                        float angle = side / (float)radialSegments * Mathf.PI * 2f;
                        Vector3 radial = normal * Mathf.Cos(angle) + binormal * Mathf.Sin(angle);
                        Vector3 worldVertex = point + radial * radius;
                        vertices[vertexCursor] = transform.InverseTransformPoint(worldVertex);
                        Vector3 localNormal = WorldNormalToLocal(radial, worldNormalToLocal);
                        normals[vertexCursor] = invertNormals ? -localNormal : localNormal;
                        uvs[vertexCursor] = new Vector2(side / (float)radialSegments, v);
                        vertexCursor++;
                    }
                }

                for (int ring = 0; ring < points.Length - 1; ring++)
                {
                    for (int side = 0; side < radialSegments; side++)
                    {
                        int current = spanVertexStart + ring * radialSegments + side;
                        int next = spanVertexStart + ring * radialSegments + (side + 1) % radialSegments;
                        int currentUp = spanVertexStart + (ring + 1) * radialSegments + side;
                        int nextUp = spanVertexStart + (ring + 1) * radialSegments + (side + 1) % radialSegments;

                        triangles[triangleCursor++] = current;
                        triangles[triangleCursor++] = next;
                        triangles[triangleCursor++] = currentUp;
                        triangles[triangleCursor++] = next;
                        triangles[triangleCursor++] = nextUp;
                        triangles[triangleCursor++] = currentUp;
                    }
                }
            }

            if (invertNormals)
            {
                ReverseTriangleWinding(triangles, triangleCursor);
            }

            overrideMesh.Clear();
            overrideMesh.vertices = vertices;
            overrideMesh.normals = normals;
            overrideMesh.uv = uvs;
            overrideMesh.triangles = triangles;
            overrideMesh.RecalculateBounds();
        }

        private void GetFrame(Vector3 tangent, out Vector3 normal, out Vector3 binormal)
        {
            tangent = tangent.sqrMagnitude > 0.0001f ? tangent.normalized : Vector3.forward;
            normal = Vector3.ProjectOnPlane(Vector3.up, tangent);
            if (normal.sqrMagnitude < 0.0001f)
            {
                normal = Vector3.ProjectOnPlane(transform.up, tangent);
            }

            if (normal.sqrMagnitude < 0.0001f)
            {
                normal = Vector3.ProjectOnPlane(Vector3.forward, tangent);
            }

            normal.Normalize();
            binormal = Vector3.Cross(tangent, normal).normalized;
            normal = Vector3.Cross(binormal, tangent).normalized;
        }

        private void BuildFrames(Vector3[] points, out Vector3[] normals, out Vector3[] binormals)
        {
            int count = points.Length;
            if (frameTangents == null || frameTangents.Length < count)
            {
                frameTangents = new Vector3[count];
                frameNormals = new Vector3[count];
                frameBinormals = new Vector3[count];
            }

            Vector3[] tangents = frameTangents;
            normals = frameNormals;
            binormals = frameBinormals;

            Vector3 tangentFallback = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                tangents[i] = CalculateTangent(points, i, tangentFallback);
                tangentFallback = tangents[i];
            }

            GetFrame(tangents[0], out normals[0], out binormals[0]);
            for (int i = 1; i < count; i++)
            {
                Vector3 transported = Quaternion.FromToRotation(tangents[i - 1], tangents[i]) * normals[i - 1];
                Vector3 normal = ProjectOnPlaneSafe(transported, tangents[i]);
                Vector3 binormal = Vector3.Cross(tangents[i], normal).normalized;
                normals[i] = Vector3.Cross(binormal, tangents[i]).normalized;
                binormals[i] = binormal;
            }
        }

        private static Vector3 ProjectOnPlaneSafe(Vector3 vector, Vector3 planeNormal)
        {
            Vector3 projected = Vector3.ProjectOnPlane(vector, planeNormal);
            if (projected.sqrMagnitude > 0.00000001f)
            {
                return projected.normalized;
            }

            projected = Vector3.ProjectOnPlane(Vector3.up, planeNormal);
            if (projected.sqrMagnitude > 0.00000001f)
            {
                return projected.normalized;
            }

            return Vector3.ProjectOnPlane(Vector3.forward, planeNormal).normalized;
        }

        private bool IsBrokenCableEnabled()
        {
            return GetBool("brokenCable", false);
        }

        private void ApplyWindDefaultsIfOldValues()
        {
            if (!applyPreferredWindDefaults || cable == null)
            {
                cable = GetComponent<ElectricCableSpline>();
            }

            if (!applyPreferredWindDefaults || cable == null)
            {
                return;
            }

            SetFloatIfApproximately("windAmplitude", 0.08f, preferredWindAmplitude);
            SetFloatIfApproximately("windSpeed", 1.3f, preferredWindSpeed);
            SetFloatIfApproximately("windSpatialFrequency", 1.6f, preferredWindSpatialFrequency);
            SetVector3IfApproximately("windDirection", new Vector3(1f, 0f, 0.25f), preferredWindDirection);
            SetFloatIfApproximately("endpointWindLock", 1f, preferredEndpointWindLock);
        }

        private bool GetBool(string fieldName, bool fallback)
        {
            FieldInfo field = GetField(fieldName);
            return field != null && field.GetValue(cable) is bool value ? value : fallback;
        }

        private int GetInt(string fieldName, int fallback)
        {
            FieldInfo field = GetField(fieldName);
            return field != null && field.GetValue(cable) is int value ? value : fallback;
        }

        private float GetFloat(string fieldName, float fallback)
        {
            FieldInfo field = GetField(fieldName);
            return field != null && field.GetValue(cable) is float value ? value : fallback;
        }

        private Vector3 GetVector3(string fieldName, Vector3 fallback)
        {
            FieldInfo field = GetField(fieldName);
            return field != null && field.GetValue(cable) is Vector3 value ? value : fallback;
        }

        private void SetFloatIfApproximately(string fieldName, float oldValue, float newValue)
        {
            FieldInfo field = GetField(fieldName);
            if (field == null || field.GetValue(cable) is not float currentValue)
            {
                return;
            }

            if (Mathf.Abs(currentValue - oldValue) <= 0.001f)
            {
                field.SetValue(cable, newValue);
            }
        }

        private void SetVector3IfApproximately(string fieldName, Vector3 oldValue, Vector3 newValue)
        {
            FieldInfo field = GetField(fieldName);
            if (field == null || field.GetValue(cable) is not Vector3 currentValue)
            {
                return;
            }

            if ((currentValue - oldValue).sqrMagnitude <= 0.0001f)
            {
                field.SetValue(cable, newValue);
            }
        }

        private FieldInfo GetField(string fieldName)
        {
            return typeof(ElectricCableSpline).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private Vector3 WorldNormalToLocal(Vector3 worldNormal, Matrix4x4 worldNormalToLocal)
        {
            Vector3 localNormal = worldNormalToLocal.MultiplyVector(worldNormal);
            if (localNormal.sqrMagnitude > 0.00000001f)
            {
                return localNormal.normalized;
            }

            return transform.InverseTransformDirection(worldNormal).normalized;
        }

        private static Vector3 CalculateTangent(Vector3[] points, int index, Vector3 fallback)
        {
            Vector3 tangent;
            if (index <= 0)
            {
                tangent = points[1] - points[0];
            }
            else if (index >= points.Length - 1)
            {
                tangent = points[index] - points[index - 1];
            }
            else
            {
                tangent = points[index + 1] - points[index - 1];
            }

            if (tangent.sqrMagnitude > 0.00000001f)
            {
                return tangent.normalized;
            }

            for (int offset = 1; offset < points.Length; offset++)
            {
                int next = index + offset;
                if (next < points.Length)
                {
                    tangent = points[next] - points[index];
                    if (tangent.sqrMagnitude > 0.00000001f)
                    {
                        return tangent.normalized;
                    }
                }

                int previous = index - offset;
                if (previous >= 0)
                {
                    tangent = points[index] - points[previous];
                    if (tangent.sqrMagnitude > 0.00000001f)
                    {
                        return tangent.normalized;
                    }
                }
            }

            return fallback.sqrMagnitude > 0.00000001f ? fallback.normalized : Vector3.forward;
        }

        private static Vector3[] RemoveConsecutiveDuplicatePoints(Vector3[] points)
        {
            if (points == null || points.Length < 2)
            {
                return points;
            }

            int uniqueCount = 1;
            Vector3 lastUnique = points[0];
            for (int i = 1; i < points.Length; i++)
            {
                if ((points[i] - lastUnique).sqrMagnitude > 0.00000001f)
                {
                    uniqueCount++;
                    lastUnique = points[i];
                }
            }

            if (uniqueCount == points.Length)
            {
                return points;
            }

            if (uniqueCount < 2)
            {
                return null;
            }

            Vector3[] result = new Vector3[uniqueCount];
            int cursor = 1;
            result[0] = points[0];
            for (int i = 1; i < points.Length; i++)
            {
                if ((points[i] - result[cursor - 1]).sqrMagnitude <= 0.00000001f)
                {
                    continue;
                }

                result[cursor++] = points[i];
            }

            return result;
        }

        private static void ReverseTriangleWinding(int[] triangleIndices, int indexCount)
        {
            for (int i = 0; i + 2 < indexCount; i += 3)
            {
                int second = triangleIndices[i + 1];
                triangleIndices[i + 1] = triangleIndices[i + 2];
                triangleIndices[i + 2] = second;
            }
        }

        private static float CalculatePolylineLength(Vector3[] points)
        {
            float length = 0f;
            for (int i = 1; i < points.Length; i++)
            {
                length += Vector3.Distance(points[i - 1], points[i]);
            }

            return length;
        }
    }
}
