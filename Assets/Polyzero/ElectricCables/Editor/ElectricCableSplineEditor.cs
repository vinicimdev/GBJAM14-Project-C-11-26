using Game.Spline;
using UnityEditor;
using UnityEngine;

namespace Polyzero.ElectricCables.Editor
{
    [CustomEditor(typeof(ElectricCableSpline))]
    public sealed class ElectricCableSplineEditor : UnityEditor.Editor
    {
        private enum Tab
        {
            Basic,
            Wind,
            Optimize,
            Advanced
        }

        private ElectricCableSpline cable;
        private Tab currentTab = Tab.Basic;
        private bool showRawInspector;

        private void OnEnable()
        {
            cable = (ElectricCableSpline)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawCableHeader();
            DrawTabs();
            EditorGUILayout.Space(8f);

            if (showRawInspector)
            {
                DrawDefaultInspector();
                DrawActionButtons();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            switch (currentTab)
            {
                case Tab.Basic:
                    DrawBasicTab();
                    break;
                case Tab.Wind:
                    DrawWindTab();
                    break;
                case Tab.Optimize:
                    DrawOptimizeTab();
                    break;
                case Tab.Advanced:
                    DrawAdvancedTab();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCableHeader()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 34f);
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));
            GUI.Label(new Rect(rect.x + 10f, rect.y + 7f, rect.width - 20f, 22f), "⚡ GENERATED CABLE", EditorStyles.boldLabel);
        }

        private void DrawTabs()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawTabButton("Basic", Tab.Basic);
                DrawTabButton("Wind", Tab.Wind);
                DrawTabButton("Optimize", Tab.Optimize);
                DrawTabButton("Advanced", Tab.Advanced);
            }
        }

        private void DrawTabButton(string label, Tab tab)
        {
            bool selected = currentTab == tab;
            Color oldColor = GUI.backgroundColor;
            if (selected)
            {
                GUI.backgroundColor = new Color(0.75f, 0.75f, 0.75f);
            }

            if (GUILayout.Button(label, selected ? EditorStyles.miniButtonMid : EditorStyles.miniButton, GUILayout.Height(24f)))
            {
                currentTab = tab;
            }

            GUI.backgroundColor = oldColor;
        }

        private void DrawBasicTab()
        {
            DrawProperty("startPoint", "Start Socket");
            DrawProperty("endPoint", "End Socket");
            DrawProperty("cableWidth", "Width");
            DrawProperty("sag", "Sag");
            DrawProperty("cableMaterial", "Material");
            DrawProperty("cableColor", "Fallback Color");

            EditorGUILayout.Space(8f);
            DrawActionButtons();
        }

        private void DrawWindTab()
        {
            DrawProperty("windSway", "Enable Wind");
            DrawProperty("windAmplitude", "Amount");
            DrawProperty("windSpeed", "Speed");
            DrawProperty("windSpatialFrequency", "Wave Length");
            DrawProperty("windDirection", "Direction");
            DrawProperty("endpointWindLock", "Lock Ends");
        }

        private void DrawOptimizeTab()
        {
            DrawProperty("useDistanceLod", "Use LOD");
            DrawProperty("nearDistance", "Near Distance");
            DrawProperty("midDistance", "Mid Distance");
            DrawProperty("farDistance", "Far Distance");
            DrawProperty("nearSegments", "Near Quality");
            DrawProperty("midSegments", "Mid Quality");
            DrawProperty("farSegments", "Far Quality");
            DrawProperty("disableBeyondFarDistance", "Disable When Far");

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = !cable.IsBakedStatic;
                if (GUILayout.Button("Bake", GUILayout.Height(30f)))
                {
                    Undo.RecordObject(cable, "Bake Cable Mesh");
                    cable.BakeMeshSnapshot();
                    EditorUtility.SetDirty(cable);
                }

                GUI.enabled = cable.IsBakedStatic;
                if (GUILayout.Button("Return Dynamic", GUILayout.Height(30f)))
                {
                    Undo.RecordObject(cable, "Return Cable To Dynamic");
                    cable.ReturnToDynamic();
                    EditorUtility.SetDirty(cable);
                }

                GUI.enabled = true;
            }
        }

        private void DrawAdvancedTab()
        {
            DrawProperty("mode", "Endpoint Mode");
            DrawProperty("visualMode", "Visual Mode");
            DrawProperty("segments", "Segments");
            DrawProperty("radialSegments", "Roundness");
            DrawProperty("autoSagFromDistance", "Auto Sag From Distance");
            DrawProperty("sagPerMeter", "Sag Per Meter");
            DrawProperty("maxAutoSag", "Max Auto Sag");
            DrawProperty("worldSideOffset", "Side Offset");
            DrawProperty("useEndpointRotation", "Use Endpoint Rotation");
            DrawProperty("startRollDegrees", "Start Roll");
            DrawProperty("endRollDegrees", "End Roll");
            DrawProperty("bakedKeepsWind", "Baked Keeps Wind");
            DrawProperty("rebuildInEditMode", "Rebuild In Edit Mode");
            showRawInspector = EditorGUILayout.Toggle("Show Raw Inspector", showRawInspector);
        }

        private void DrawActionButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebuild", GUILayout.Height(30f)))
                {
                    cable.Rebuild();
                    EditorUtility.SetDirty(cable);
                }

                if (GUILayout.Button("Create Visuals", GUILayout.Height(30f)))
                {
                    cable.EnsureVisualComponents();
                    cable.Rebuild();
                    EditorUtility.SetDirty(cable);
                }
            }

            Color previousColor = GUI.backgroundColor;
            if (cable.NormalsInverted)
            {
                GUI.backgroundColor = new Color(0.75f, 0.9f, 1f);
            }

            if (GUILayout.Button(cable.NormalsInverted ? "Restore Normals" : "Invert Normals", GUILayout.Height(30f)))
            {
                ToggleCableNormals(cable);
            }

            GUI.backgroundColor = previousColor;
        }

        private static void ToggleCableNormals(ElectricCableSpline targetCable)
        {
            const string undoName = "Toggle Cable Normals";
            Undo.RecordObject(targetCable, undoName);

            Mesh bakedMesh = targetCable.IsBakedStatic && targetCable.CableMeshFilter != null
                ? targetCable.CableMeshFilter.sharedMesh
                : null;
            if (bakedMesh != null)
            {
                Undo.RecordObject(bakedMesh, undoName);
            }

            targetCable.SetNormalsInverted(!targetCable.NormalsInverted);
            PrefabUtility.RecordPrefabInstancePropertyModifications(targetCable);
            EditorUtility.SetDirty(targetCable);
            if (bakedMesh != null)
            {
                EditorUtility.SetDirty(bakedMesh);
            }
        }

        private void DrawProperty(string propertyName, string label = null)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            EditorGUILayout.PropertyField(property, string.IsNullOrWhiteSpace(label) ? GUIContent.none : new GUIContent(label), true);
        }

        private void OnSceneGUI()
        {
            if (cable == null || !cable.TryGetEndpoints(out Vector3 start, out Vector3 end))
            {
                return;
            }

            Handles.color = cable.IsBrokenCable ? Color.red : new Color(1f, 0.72f, 0.15f, 1f);
            Handles.DrawAAPolyLine(4f, BuildPreviewPoints(cable, 32));

            EditorGUI.BeginChangeCheck();
            Vector3 newStart = Handles.PositionHandle(start, Quaternion.identity);
            Vector3 newEnd = Handles.PositionHandle(end, Quaternion.identity);
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Undo.RecordObject(cable, "Move Cable Endpoint");

            SerializedObject serializedCable = new SerializedObject(cable);
            SerializedProperty modeProperty = serializedCable.FindProperty("mode");
            bool usesTransforms = modeProperty != null && modeProperty.enumValueIndex == (int)ElectricCableSpline.CableMode.TransformEndpoints;

            if (usesTransforms)
            {
                MoveEndpointTransform(serializedCable, "startPoint", start, newStart, "Move Cable Start Point");
                MoveEndpointTransform(serializedCable, "endPoint", end, newEnd, "Move Cable End Point");
            }
            else
            {
                SerializedProperty localStartProperty = serializedCable.FindProperty("localStartPoint");
                SerializedProperty localEndProperty = serializedCable.FindProperty("localEndPoint");
                if (localStartProperty != null)
                {
                    localStartProperty.vector3Value = cable.transform.InverseTransformPoint(newStart);
                }

                if (localEndProperty != null)
                {
                    localEndProperty.vector3Value = cable.transform.InverseTransformPoint(newEnd);
                }
            }

            serializedCable.ApplyModifiedProperties();
            cable.Rebuild();
            EditorUtility.SetDirty(cable);
        }

        private static Vector3[] BuildPreviewPoints(ElectricCableSpline cable, int segmentCount)
        {
            Vector3[] points = new Vector3[segmentCount + 1];
            for (int i = 0; i <= segmentCount; i++)
            {
                points[i] = cable.EvaluatePoint(i / (float)segmentCount);
            }

            return points;
        }

        private static void MoveEndpointTransform(SerializedObject serializedCable, string propertyName, Vector3 oldPosition, Vector3 newPosition, string undoName)
        {
            SerializedProperty property = serializedCable.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == null)
            {
                return;
            }

            Transform endpoint = property.objectReferenceValue as Transform;
            if (endpoint == null)
            {
                return;
            }

            if ((oldPosition - newPosition).sqrMagnitude < 0.00001f)
            {
                return;
            }

            Undo.RecordObject(endpoint, undoName);
            endpoint.position = newPosition;
            EditorUtility.SetDirty(endpoint);
        }
    }
}
