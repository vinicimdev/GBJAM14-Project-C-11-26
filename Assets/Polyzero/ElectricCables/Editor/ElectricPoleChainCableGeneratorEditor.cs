using Game.Spline;
using UnityEditor;
using UnityEngine;

namespace Polyzero.ElectricCables.Editor
{
    [CustomEditor(typeof(ElectricPoleChainCableGenerator))]
    public sealed class ElectricPoleChainCableGeneratorEditor : UnityEditor.Editor
    {
        private enum Tab
        {
            Start,
            Look,
            Motion,
            Optimize,
            Damage,
            Advanced
        }

        private Tab currentTab = Tab.Start;
        private ElectricCableProfile selectedProfile;
        private bool showRawInspector;
        private bool showGeneratedCables = true;
        private ElectricCableSetupReport lastReport;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawCableChainHeader();
            DrawTabs();
            EditorGUILayout.Space(8f);

            if (showRawInspector)
            {
                DrawDefaultInspector();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            switch (currentTab)
            {
                case Tab.Start:
                    DrawStartTab();
                    break;
                case Tab.Look:
                    DrawLookTab();
                    break;
                case Tab.Motion:
                    DrawMotionTab();
                    break;
                case Tab.Optimize:
                    DrawOptimizeTab();
                    break;
                case Tab.Damage:
                    DrawDamageTab();
                    break;
                case Tab.Advanced:
                    DrawAdvancedTab();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCableChainHeader()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 36f);
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));
            GUI.Label(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 22f), "⚡ ELECTRIC CABLE CHAIN", EditorStyles.boldLabel);
        }

        private void DrawTabs()
        {
            string[] names = { "Start", "Look", "Motion", "Optimize", "Damage", "Advanced" };
            Tab[] values = { Tab.Start, Tab.Look, Tab.Motion, Tab.Optimize, Tab.Damage, Tab.Advanced };

            for (int row = 0; row < 2; row++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int col = 0; col < 3; col++)
                    {
                        int index = row * 3 + col;
                        bool selected = currentTab == values[index];
                        Color oldColor = GUI.backgroundColor;
                        if (selected)
                        {
                            GUI.backgroundColor = new Color(0.75f, 0.75f, 0.75f);
                        }

                        if (GUILayout.Button(names[index], selected ? EditorStyles.miniButtonMid : EditorStyles.miniButton, GUILayout.Height(24f)))
                        {
                            currentTab = values[index];
                        }

                        GUI.backgroundColor = oldColor;
                    }
                }
            }
        }

        private void DrawStartTab()
        {
            EditorGUILayout.LabelField("Setup Health", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate Setup", GUILayout.Height(30f)))
                {
                    ElectricPoleChainCableGenerator generator = (ElectricPoleChainCableGenerator)target;
                    lastReport = generator.ValidateSetup();
                    Debug.Log(lastReport.ToConsoleString(), generator);
                }

                if (GUILayout.Button("Fix Common Issues", GUILayout.Height(30f)))
                {
                    ForEachGenerator(generator => generator.FixCommonIssues(), "Fix Common Cable Setup Issues");
                    lastReport = ((ElectricPoleChainCableGenerator)target).ValidateSetup();
                }
            }

            DrawReport(lastReport);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Preset", EditorStyles.boldLabel);
            selectedProfile = (ElectricCableProfile)EditorGUILayout.ObjectField("Cable Profile", selectedProfile, typeof(ElectricCableProfile), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = selectedProfile != null;
                if (GUILayout.Button("Apply Profile", GUILayout.Height(30f)))
                {
                    foreach (Object targetObject in targets)
                    {
                        ElectricPoleChainCableGenerator generator = (ElectricPoleChainCableGenerator)targetObject;
                        Undo.RecordObject(generator, "Apply Cable Profile");
                        ElectricCableEditorReflection.ApplyProfileToGenerator(generator, selectedProfile);
                        EditorUtility.SetDirty(generator);
                    }
                }

                if (GUILayout.Button("Apply Profile + Rebuild", GUILayout.Height(30f)))
                {
                    foreach (Object targetObject in targets)
                    {
                        ElectricPoleChainCableGenerator generator = (ElectricPoleChainCableGenerator)targetObject;
                        Undo.RecordObject(generator, "Apply Cable Profile And Rebuild");
                        ElectricCableEditorReflection.ApplyProfileToGenerator(generator, selectedProfile);
                        generator.RebuildCables();
                        EditorUtility.SetDirty(generator);
                    }
                }

                GUI.enabled = true;
            }

            if (GUILayout.Button("Create Default Profiles", GUILayout.Height(28f)))
            {
                ElectricCableProfileFactory.CreateDefaultProfiles();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Quality Mode", EditorStyles.boldLabel);
            DrawProperty("qualityMode", "Mode");
            if (GUILayout.Button("Apply Quality + Rebuild", GUILayout.Height(30f)))
            {
                ForEachGenerator(generator => generator.ApplyQualityModeAndRebuild(ElectricCableEditorReflection.Get(generator, "qualityMode", ElectricPoleChainCableGenerator.QualityMode.Balanced)), "Apply Quality Mode");
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Chain", EditorStyles.boldLabel);
            DrawProperty("autoRebuildInEditMode", "Auto Rebuild");
            DrawProperty("autoRebuildWhenChildrenChange", "Rebuild When Poles Move/Duplicate");
            DrawProperty("closeLoop", "Close Loop");

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebuild Chain", GUILayout.Height(34f)))
                {
                    ForEachGenerator(generator => generator.RebuildCables(), "Rebuild Cable Chain");
                }

                if (GUILayout.Button("Clear", GUILayout.Height(34f)))
                {
                    ForEachGenerator(generator => generator.ClearGeneratedCables(), "Clear Generated Cables");
                }
            }

            DrawGeneratedCableControls();

            EditorGUILayout.HelpBox("Keep pole instances as direct children of this object. The tool connects socket 1 to socket 1, socket 2 to socket 2, etc.", MessageType.Info);
        }

        private void DrawGeneratedCableControls()
        {
            ElectricPoleChainCableGenerator generator = (ElectricPoleChainCableGenerator)target;
            SerializedProperty prefixProperty = serializedObject.FindProperty("generatedCablePrefix");
            string prefix = prefixProperty != null ? prefixProperty.stringValue : "AutoCable";
            int cableCount = 0;

            if (string.IsNullOrWhiteSpace(prefix))
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.HelpBox("Set a Generated Cable Prefix before managing individual cables.", MessageType.Warning);
                return;
            }

            for (int i = 0; i < generator.transform.childCount; i++)
            {
                Transform child = generator.transform.GetChild(i);
                if (child != null && child.name.StartsWith(prefix) && child.GetComponent<ElectricCableSpline>() != null)
                {
                    cableCount++;
                }
            }

            EditorGUILayout.Space(8f);
            showGeneratedCables = EditorGUILayout.Foldout(showGeneratedCables, $"Generated Cables ({cableCount})", true);
            if (!showGeneratedCables)
            {
                return;
            }

            if (cableCount == 0)
            {
                EditorGUILayout.HelpBox("No generated cables in this chain.", MessageType.None);
                return;
            }

            EditorGUI.indentLevel++;
            for (int i = 0; i < generator.transform.childCount; i++)
            {
                Transform child = generator.transform.GetChild(i);
                if (child == null || !child.name.StartsWith(prefix))
                {
                    continue;
                }

                ElectricCableSpline cable = child.GetComponent<ElectricCableSpline>();
                if (cable == null)
                {
                    continue;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(cable.name, EditorStyles.miniButtonLeft))
                    {
                        Selection.activeGameObject = cable.gameObject;
                        EditorGUIUtility.PingObject(cable.gameObject);
                    }

                    Color previousColor = GUI.backgroundColor;
                    if (cable.NormalsInverted)
                    {
                        GUI.backgroundColor = new Color(0.75f, 0.9f, 1f);
                    }

                    if (GUILayout.Button(cable.NormalsInverted ? "Restore" : "Invert", EditorStyles.miniButtonRight, GUILayout.Width(82f)))
                    {
                        ToggleCableNormals(cable);
                    }

                    GUI.backgroundColor = previousColor;
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawReport(ElectricCableSetupReport report)
        {
            if (report == null)
            {
                return;
            }

            MessageType type = report.HasErrors ? MessageType.Error : report.HasWarnings ? MessageType.Warning : MessageType.Info;
            EditorGUILayout.HelpBox(report.HasErrors ? "Setup has errors. See Console for full report." : report.HasWarnings ? "Setup has warnings. See Console for full report." : "Setup looks good.", type);
        }

        private void DrawLookTab()
        {
            EditorGUILayout.LabelField("Main Visual Settings", EditorStyles.boldLabel);
            DrawProperty("cableWidth", "Cable Width");
            DrawProperty("sag", "Base Sag");
            DrawProperty("sagPerMeter", "Sag Per Meter");
            DrawProperty("maxAutoSag", "Max Sag");
            DrawProperty("cableMaterial", "Material");
            DrawProperty("cableColor", "Fallback Color");

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Quality", EditorStyles.boldLabel);
            DrawProperty("segments", "Length Segments");
            DrawProperty("radialSegments", "Roundness");

            if (GUILayout.Button("Apply Look To Existing Cables", GUILayout.Height(28f)))
            {
                ForEachGenerator(generator => generator.ApplySettingsToExistingGeneratedCables(), "Apply Look To Existing Cables");
            }
        }

        private void DrawMotionTab()
        {
            EditorGUILayout.LabelField("Wind", EditorStyles.boldLabel);
            DrawProperty("windSway", "Enable Wind");
            DrawProperty("windAmplitude", "Amount");
            DrawProperty("windSpeed", "Speed");
            DrawProperty("windSpatialFrequency", "Wave Length");
            DrawProperty("windDirection", "Direction");
            DrawProperty("endpointWindLock", "Lock Ends");
            DrawProperty("addPerCableWindVariation", "Vary Each Cable");

            if (GUILayout.Button("Apply Wind To Existing Cables", GUILayout.Height(28f)))
            {
                ForEachGenerator(generator => generator.ApplySettingsToExistingGeneratedCables(), "Apply Wind To Existing Cables");
            }

            EditorGUILayout.HelpBox("Keep variation enabled so parallel wires do not move with the exact same animation.", MessageType.None);
        }

        private void DrawOptimizeTab()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Bake All Cables", GUILayout.Height(34f)))
                {
                    ForEachCable(cable => cable.BakeMeshSnapshot(), "Bake All Cables");
                }

                if (GUILayout.Button("Return To Dynamic", GUILayout.Height(34f)))
                {
                    ForEachCable(cable => cable.ReturnToDynamic(), "Return All Cables To Dynamic");
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("LOD", EditorStyles.boldLabel);
            DrawProperty("useDistanceLod", "Use LOD");
            DrawProperty("lodCamera", "LOD Camera");
            DrawProperty("nearDistance", "Near Distance");
            DrawProperty("midDistance", "Mid Distance");
            DrawProperty("farDistance", "Far Distance");
            DrawProperty("nearSegments", "Near Quality");
            DrawProperty("midSegments", "Mid Quality");
            DrawProperty("farSegments", "Far Quality");
            DrawProperty("nearRadialSegments", "Near Roundness");
            DrawProperty("midRadialSegments", "Mid Roundness");
            DrawProperty("farRadialSegments", "Far Roundness");
            DrawProperty("disableBeyondFarDistance", "Disable When Far");
            DrawProperty("bakedKeepsWind", "Baked Keeps Wind");

            if (GUILayout.Button("Apply Optimize Settings To Existing Cables", GUILayout.Height(28f)))
            {
                ForEachGenerator(generator => generator.ApplySettingsToExistingGeneratedCables(), "Apply Optimize To Existing Cables");
            }

            EditorGUILayout.Space(8f);
            selectedProfile = (ElectricCableProfile)EditorGUILayout.ObjectField("Profile", selectedProfile, typeof(ElectricCableProfile), false);
            GUI.enabled = selectedProfile != null;
            if (GUILayout.Button("Apply Profile To Existing Cables", GUILayout.Height(28f)))
            {
                ApplyProfileToExistingCables(selectedProfile);
            }
            GUI.enabled = true;

            EditorGUILayout.HelpBox("Bake freezes cable topology/LOD but keeps wind animation when enabled on the cable.", MessageType.Info);
        }

        private void DrawDamageTab()
        {
            EditorGUILayout.HelpBox("Recommended workflow: use Local Broken Cables on the pole prefab, near the sockets. Avoid breaking long AutoCable objects.", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Randomize Local Damage On Poles", GUILayout.Height(30f)))
                {
                    ForEachLocalDamage(component => component.RandomizeLocalDamage(), "Randomize Local Damage");
                }

                if (GUILayout.Button("Clear Local Damage On Poles", GUILayout.Height(30f)))
                {
                    ForEachLocalDamage(component => component.ClearLocalDamage(), "Clear Local Damage");
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Mark Selected Cable Broken", GUILayout.Height(30f)))
                {
                    SetSelectedCablesBroken(true);
                }

                if (GUILayout.Button("Repair Selected Cable", GUILayout.Height(30f)))
                {
                    SetSelectedCablesBroken(false);
                }
            }
        }

        private void DrawAdvancedTab()
        {
            DrawProperty("discoveryMode", "Pole Discovery");
            DrawProperty("explicitPoles", "Explicit Poles");
            DrawProperty("includeInactivePoles", "Include Inactive Poles");
            DrawProperty("generatedCablePrefix", "Generated Cable Prefix");
            DrawProperty("removeOldGeneratedCables", "Replace Old Cables On Rebuild");
            DrawProperty("rebuildAtRuntimeStart", "Rebuild At Runtime Start");
            DrawProperty("replaceUnsupportedMaterial", "Replace Unsupported Material");
            DrawProperty("createPipelineCompatibleMaterialWhenEmpty", "Auto Material If Empty");
            DrawProperty("cableSmoothness", "Smoothness");

            EditorGUILayout.Space(8f);
            showRawInspector = EditorGUILayout.Toggle("Show Raw Inspector", showRawInspector);
        }

        private void DrawProperty(string propertyName, string label = null)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(label))
            {
                EditorGUILayout.PropertyField(property, true);
            }
            else
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label), true);
            }
        }

        private void ForEachGenerator(System.Action<ElectricPoleChainCableGenerator> action, string undoName)
        {
            foreach (Object targetObject in targets)
            {
                ElectricPoleChainCableGenerator generator = (ElectricPoleChainCableGenerator)targetObject;
                Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, undoName);
                action(generator);
                EditorUtility.SetDirty(generator);
            }
        }

        private void ForEachCable(System.Action<ElectricCableSpline> action, string undoName)
        {
            foreach (Object targetObject in targets)
            {
                ElectricPoleChainCableGenerator generator = (ElectricPoleChainCableGenerator)targetObject;
                ElectricCableSpline[] cables = generator.GetComponentsInChildren<ElectricCableSpline>(true);
                foreach (ElectricCableSpline cable in cables)
                {
                    Undo.RecordObject(cable, undoName);
                    action(cable);
                    EditorUtility.SetDirty(cable);
                }
            }
        }

        private void ForEachLocalDamage(System.Action<ElectricPoleLocalBrokenCables> action, string undoName)
        {
            foreach (Object targetObject in targets)
            {
                ElectricPoleChainCableGenerator generator = (ElectricPoleChainCableGenerator)targetObject;
                ElectricPoleLocalBrokenCables[] damageComponents = generator.GetComponentsInChildren<ElectricPoleLocalBrokenCables>(true);
                foreach (ElectricPoleLocalBrokenCables damageComponent in damageComponents)
                {
                    Undo.RegisterFullObjectHierarchyUndo(damageComponent.gameObject, undoName);
                    action(damageComponent);
                    EditorUtility.SetDirty(damageComponent);
                }
            }
        }

        private void ApplyProfileToExistingCables(ElectricCableProfile profile)
        {
            ForEachCable(cable => ElectricCableEditorReflection.ApplyProfileToCable(cable, profile), "Apply Profile To Cables");
        }

        private void SetSelectedCablesBroken(bool broken)
        {
            foreach (GameObject selected in Selection.gameObjects)
            {
                ElectricCableSpline cable = selected.GetComponent<ElectricCableSpline>();
                if (cable == null)
                {
                    continue;
                }

                Undo.RecordObject(cable, broken ? "Mark Cable Broken" : "Repair Cable");
                ElectricCableEditorReflection.Set(cable, "brokenCable", broken);
                if (broken && selected.GetComponent<ElectricCableBrokenVisualOverride>() == null)
                {
                    Undo.AddComponent<ElectricCableBrokenVisualOverride>(selected);
                }

                cable.Rebuild();
                EditorUtility.SetDirty(cable);
            }
        }

        private static void ToggleCableNormals(ElectricCableSpline cable)
        {
            const string undoName = "Toggle Generated Cable Normals";
            Undo.RecordObject(cable, undoName);

            Mesh bakedMesh = cable.IsBakedStatic && cable.CableMeshFilter != null
                ? cable.CableMeshFilter.sharedMesh
                : null;
            if (bakedMesh != null)
            {
                Undo.RecordObject(bakedMesh, undoName);
            }

            cable.SetNormalsInverted(!cable.NormalsInverted);
            PrefabUtility.RecordPrefabInstancePropertyModifications(cable);
            EditorUtility.SetDirty(cable);
            if (bakedMesh != null)
            {
                EditorUtility.SetDirty(bakedMesh);
            }
        }
    }
}
