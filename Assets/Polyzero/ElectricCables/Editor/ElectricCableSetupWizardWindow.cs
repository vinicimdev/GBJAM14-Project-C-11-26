using Game.Spline;
using UnityEditor;
using UnityEngine;

namespace Polyzero.ElectricCables.Editor
{
    public sealed class ElectricCableSetupWizardWindow : EditorWindow
    {
        private GameObject polePrefab;
        private int socketCount = 3;
        private float socketHeight = 4.2f;
        private float socketSpacing = 0.35f;
        private float socketForwardOffset;
        private string sceneChainName = "PoleChain";
        private bool instantiateFirstPole = true;
        private bool addLocalBrokenCables = true;
        private ElectricPoleChainCableGenerator.QualityMode qualityMode = ElectricPoleChainCableGenerator.QualityMode.Balanced;
        private ElectricCableProfile cableProfile;
        private Vector2 scroll;
        private int step;

        [MenuItem("Tools/Polyzero/Electric Cables/Electric Cable Setup Wizard")]
        public static void Open()
        {
            GetWindow<ElectricCableSetupWizardWindow>("Cable Setup Wizard");
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawHeader();
            DrawStepTabs();
            EditorGUILayout.Space(10f);

            switch (step)
            {
                case 0:
                    DrawPrefabStep();
                    break;
                case 1:
                    DrawSocketsStep();
                    break;
                case 2:
                    DrawChainStep();
                    break;
                case 3:
                    DrawQualityStep();
                    break;
                case 4:
                    DrawBuildStep();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 38f);
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));
            GUI.Label(new Rect(rect.x + 10f, rect.y + 9f, rect.width - 20f, 22f), "⚡ ELECTRIC CABLE SETUP WIZARD", EditorStyles.boldLabel);
        }

        private void DrawStepTabs()
        {
            string[] labels = { "1 Prefab", "2 Sockets", "3 Chain", "4 Quality", "5 Build" };
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < labels.Length; i++)
                {
                    Color old = GUI.backgroundColor;
                    if (step == i) GUI.backgroundColor = new Color(0.75f, 0.75f, 0.75f);
                    if (GUILayout.Button(labels[i], GUILayout.Height(24f))) step = i;
                    GUI.backgroundColor = old;
                }
            }
        }

        private void DrawPrefabStep()
        {
            EditorGUILayout.LabelField("Step 1 — Pole Prefab", EditorStyles.boldLabel);
            polePrefab = (GameObject)EditorGUILayout.ObjectField("Pole Prefab", polePrefab, typeof(GameObject), false);
            addLocalBrokenCables = EditorGUILayout.Toggle("Add Local Broken Cables", addLocalBrokenCables);
            EditorGUILayout.HelpBox("Drag a prefab asset from the Project window. The wizard will add sockets and required components to this prefab.", MessageType.Info);
            DrawNavigationButtons(false, true);
        }

        private void DrawSocketsStep()
        {
            EditorGUILayout.LabelField("Step 2 — Socket Defaults", EditorStyles.boldLabel);
            socketCount = EditorGUILayout.IntSlider("Sockets Per Pole", socketCount, 1, 8);
            socketHeight = EditorGUILayout.FloatField("Socket Height", socketHeight);
            socketSpacing = EditorGUILayout.FloatField("Socket Horizontal Spacing", socketSpacing);
            socketForwardOffset = EditorGUILayout.FloatField("Socket Forward Offset", socketForwardOffset);
            EditorGUILayout.HelpBox("These are only starting positions. After setup, move CableSocket_01/02/03 on the prefab to the exact attachment points.", MessageType.None);
            DrawNavigationButtons(true, true);
        }

        private void DrawChainStep()
        {
            EditorGUILayout.LabelField("Step 3 — Scene Chain", EditorStyles.boldLabel);
            sceneChainName = EditorGUILayout.TextField("Scene Chain Name", sceneChainName);
            instantiateFirstPole = EditorGUILayout.Toggle("Instantiate First Pole", instantiateFirstPole);
            EditorGUILayout.HelpBox("The chain object is the parent that holds all pole instances. Duplicate poles as direct children of this object.", MessageType.Info);
            DrawNavigationButtons(true, true);
        }

        private void DrawQualityStep()
        {
            EditorGUILayout.LabelField("Step 4 — Quality / Preset", EditorStyles.boldLabel);
            qualityMode = (ElectricPoleChainCableGenerator.QualityMode)EditorGUILayout.EnumPopup("Quality Mode", qualityMode);
            cableProfile = (ElectricCableProfile)EditorGUILayout.ObjectField("Optional Cable Profile", cableProfile, typeof(ElectricCableProfile), false);
            if (GUILayout.Button("Create Default Profiles", GUILayout.Height(28f))) ElectricCableProfileFactory.CreateDefaultProfiles();
            EditorGUILayout.HelpBox("Quality mode controls segments, roundness and LOD defaults. A profile can override cable appearance/wind values.", MessageType.None);
            DrawNavigationButtons(true, true);
        }

        private void DrawBuildStep()
        {
            EditorGUILayout.LabelField("Step 5 — Build", EditorStyles.boldLabel);
            DrawStatusLine("Pole prefab assigned", polePrefab != null);
            DrawStatusLine("Socket count valid", socketCount > 0);
            DrawStatusLine("Scene chain name valid", !string.IsNullOrWhiteSpace(sceneChainName));

            GUI.enabled = CanBuild();
            if (GUILayout.Button("Setup Prefab + Scene Chain", GUILayout.Height(36f))) BuildSetup();
            GUI.enabled = true;
            DrawNavigationButtons(true, false);
        }

        private void DrawStatusLine(string text, bool ok)
        {
            EditorGUILayout.LabelField((ok ? "✓ " : "✕ ") + text);
        }

        private void DrawNavigationButtons(bool previous, bool next)
        {
            EditorGUILayout.Space(10f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = previous;
                if (GUILayout.Button("Back", GUILayout.Height(28f))) step = Mathf.Max(0, step - 1);
                GUI.enabled = next;
                if (GUILayout.Button("Next", GUILayout.Height(28f))) step = Mathf.Min(4, step + 1);
                GUI.enabled = true;
            }
        }

        private bool CanBuild()
        {
            if (polePrefab == null || string.IsNullOrWhiteSpace(sceneChainName) || socketCount <= 0) return false;
            string prefabPath = AssetDatabase.GetAssetPath(polePrefab);
            return !string.IsNullOrWhiteSpace(prefabPath) && PrefabUtility.GetPrefabAssetType(polePrefab) != PrefabAssetType.NotAPrefab;
        }

        private void BuildSetup()
        {
            string prefabPath = AssetDatabase.GetAssetPath(polePrefab);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                ElectricCableWizardUtility.EnsurePolePrefabSetup(prefabRoot.transform, socketCount, socketHeight, socketSpacing, socketForwardOffset, addLocalBrokenCables);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            GameObject chainRoot = GameObject.Find(sceneChainName);
            if (chainRoot == null)
            {
                chainRoot = new GameObject(sceneChainName);
                Undo.RegisterCreatedObjectUndo(chainRoot, "Create Pole Chain");
            }

            ElectricPoleChainCableGenerator generator = chainRoot.GetComponent<ElectricPoleChainCableGenerator>();
            if (generator == null) generator = Undo.AddComponent<ElectricPoleChainCableGenerator>(chainRoot);

            Undo.RecordObject(generator, "Configure Pole Chain");
            generator.ApplyQualityMode(qualityMode);
            if (cableProfile != null) ElectricCableEditorReflection.ApplyProfileToGenerator(generator, cableProfile);
            generator.FixCommonIssues();
            EditorUtility.SetDirty(generator);

            if (instantiateFirstPole && chainRoot.transform.childCount == 0)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(polePrefab);
                Undo.RegisterCreatedObjectUndo(instance, "Create First Pole Instance");
                instance.name = polePrefab.name + "_01";
                instance.transform.SetParent(chainRoot.transform, false);
                Selection.activeGameObject = instance;
            }
            else
            {
                Selection.activeGameObject = chainRoot;
            }

            Debug.Log("Electric Cable setup complete. Adjust sockets, duplicate poles under " + sceneChainName + ", then Rebuild Chain.", chainRoot);
        }
    }
}
