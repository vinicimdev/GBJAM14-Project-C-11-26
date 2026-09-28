using Game.Spline;
using UnityEditor;
using UnityEngine;

namespace Polyzero.ElectricCables.Editor
{
    public sealed class ElectricCableGeneratorWindow : EditorWindow
    {
        private GameObject polePrefab;
        private bool instantiatePoleInScene = true;
        private bool addLocalBrokenCablesToPrefab = true;
        private string sceneChainName = "PoleChain";
        private int socketCount = 3;
        private float socketHeight = 4.2f;
        private float socketSpacing = 0.35f;
        private float socketForwardOffset;
        private float cableWidth = 0.045f;
        private int segments = 24;
        private int radialSegments = 8;
        private float sag = 0.45f;
        private float sagPerMeter = 0.08f;
        private float maxAutoSag = 2.5f;
        private bool windSway = true;
        private float windAmplitude = 0.04f;
        private Material cableMaterial;

        [MenuItem("Tools/Polyzero/Electric Cables/Electric Cable Generator")]
        public static void Open()
        {
            GetWindow<ElectricCableGeneratorWindow>("Electric Cables");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Prefab Pole Chain Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Prepare a pole prefab with cable sockets, create a PoleChain in the scene and generate socket-to-socket cables.", MessageType.Info);

            polePrefab = (GameObject)EditorGUILayout.ObjectField("Pole Prefab", polePrefab, typeof(GameObject), false);
            sceneChainName = EditorGUILayout.TextField("Scene Chain Name", sceneChainName);
            instantiatePoleInScene = EditorGUILayout.Toggle("Instantiate First Pole", instantiatePoleInScene);
            addLocalBrokenCablesToPrefab = EditorGUILayout.Toggle("Add Local Broken Cables", addLocalBrokenCablesToPrefab);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Socket Defaults", EditorStyles.boldLabel);
            socketCount = EditorGUILayout.IntSlider("Sockets Per Pole", socketCount, 1, 8);
            socketHeight = EditorGUILayout.FloatField("Socket Height", socketHeight);
            socketSpacing = EditorGUILayout.FloatField("Socket Horizontal Spacing", socketSpacing);
            socketForwardOffset = EditorGUILayout.FloatField("Socket Forward Offset", socketForwardOffset);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Cable Defaults", EditorStyles.boldLabel);
            cableWidth = EditorGUILayout.FloatField("Cable Width", cableWidth);
            segments = EditorGUILayout.IntSlider("Segments", segments, 2, 96);
            radialSegments = EditorGUILayout.IntSlider("Radial Segments", radialSegments, 3, 16);
            sag = EditorGUILayout.FloatField("Base Sag", sag);
            sagPerMeter = EditorGUILayout.FloatField("Sag Per Meter", sagPerMeter);
            maxAutoSag = EditorGUILayout.FloatField("Max Auto Sag", maxAutoSag);
            windSway = EditorGUILayout.Toggle("Wind Sway", windSway);
            windAmplitude = EditorGUILayout.FloatField("Wind Amplitude", windAmplitude);
            cableMaterial = (Material)EditorGUILayout.ObjectField("Cable Material", cableMaterial, typeof(Material), false);

            EditorGUILayout.Space(10f);
            if (GUILayout.Button("Setup Prefab + Scene Chain", GUILayout.Height(32f)))
            {
                SetupPrefabAndSceneChain();
            }
        }

        private void SetupPrefabAndSceneChain()
        {
            if (polePrefab == null)
            {
                Debug.LogWarning("Assign a pole prefab first.");
                return;
            }

            string prefabPath = AssetDatabase.GetAssetPath(polePrefab);
            if (string.IsNullOrWhiteSpace(prefabPath) || PrefabUtility.GetPrefabAssetType(polePrefab) == PrefabAssetType.NotAPrefab)
            {
                Debug.LogWarning("Pole Prefab must be a prefab asset from the Project window, not a scene object.");
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                ElectricCableWizardUtility.EnsurePolePrefabSetup(prefabRoot.transform, socketCount, socketHeight, socketSpacing, socketForwardOffset, addLocalBrokenCablesToPrefab);
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
            if (generator == null)
            {
                generator = Undo.AddComponent<ElectricPoleChainCableGenerator>(chainRoot);
            }

            ConfigureChainGenerator(generator);

            if (instantiatePoleInScene && chainRoot.transform.childCount == 0)
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

            EditorUtility.SetDirty(chainRoot);
            Debug.Log("Prefab and scene chain are ready. Adjust sockets, duplicate poles under PoleChain, then Rebuild Chain.");
        }

        private void ConfigureChainGenerator(ElectricPoleChainCableGenerator generator)
        {
            SerializedObject serializedGenerator = new SerializedObject(generator);
            SetEnum(serializedGenerator, "discoveryMode", 0);
            SetBool(serializedGenerator, "autoRebuildInEditMode", true);
            SetBool(serializedGenerator, "rebuildAtRuntimeStart", true);
            SetBool(serializedGenerator, "removeOldGeneratedCables", true);
            SetString(serializedGenerator, "generatedCablePrefix", "AutoCable");
            SetInt(serializedGenerator, "segments", segments);
            SetFloat(serializedGenerator, "sag", sag);
            SetBool(serializedGenerator, "autoSagFromDistance", true);
            SetFloat(serializedGenerator, "sagPerMeter", sagPerMeter);
            SetFloat(serializedGenerator, "maxAutoSag", maxAutoSag);
            SetFloat(serializedGenerator, "cableWidth", cableWidth);
            SetInt(serializedGenerator, "radialSegments", radialSegments);
            SetObject(serializedGenerator, "cableMaterial", cableMaterial);
            SetBool(serializedGenerator, "windSway", windSway);
            SetFloat(serializedGenerator, "windAmplitude", windAmplitude);
            serializedGenerator.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(generator);
        }

        private static void SetObject(SerializedObject obj, string name, Object value)
        {
            SerializedProperty property = obj.FindProperty(name);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void SetString(SerializedObject obj, string name, string value)
        {
            SerializedProperty property = obj.FindProperty(name);
            if (property != null) property.stringValue = value;
        }

        private static void SetBool(SerializedObject obj, string name, bool value)
        {
            SerializedProperty property = obj.FindProperty(name);
            if (property != null) property.boolValue = value;
        }

        private static void SetInt(SerializedObject obj, string name, int value)
        {
            SerializedProperty property = obj.FindProperty(name);
            if (property != null) property.intValue = value;
        }

        private static void SetFloat(SerializedObject obj, string name, float value)
        {
            SerializedProperty property = obj.FindProperty(name);
            if (property != null) property.floatValue = value;
        }

        private static void SetEnum(SerializedObject obj, string name, int value)
        {
            SerializedProperty property = obj.FindProperty(name);
            if (property != null) property.enumValueIndex = value;
        }
    }
}
