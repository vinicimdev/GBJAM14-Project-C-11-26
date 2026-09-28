using Game.Spline;
using UnityEditor;
using UnityEngine;

namespace Polyzero.ElectricCables.Editor
{
    public static class ElectricCableWizardUtility
    {
        public static ElectricPoleCableHub EnsurePolePrefabSetup(Transform pole, int socketCount, float socketHeight, float socketSpacing, float socketForwardOffset, bool addLocalBrokenCables)
        {
            ElectricPoleCableHub hub = pole.GetComponent<ElectricPoleCableHub>();
            if (hub == null)
            {
                hub = pole.gameObject.AddComponent<ElectricPoleCableHub>();
            }

            Transform socketsRoot = pole.Find("CableSockets");
            if (socketsRoot == null)
            {
                GameObject socketsRootObject = new GameObject("CableSockets");
                socketsRoot = socketsRootObject.transform;
                socketsRoot.SetParent(pole, false);
                socketsRoot.localPosition = Vector3.zero;
                socketsRoot.localRotation = Quaternion.identity;
                socketsRoot.localScale = Vector3.one;
            }

            socketCount = Mathf.Max(1, socketCount);
            Transform[] sockets = new Transform[socketCount];
            float center = (socketCount - 1) * 0.5f;
            for (int i = 0; i < socketCount; i++)
            {
                string socketName = $"CableSocket_{i + 1:00}";
                Transform socket = socketsRoot.Find(socketName);
                if (socket == null)
                {
                    GameObject socketObject = new GameObject(socketName);
                    socket = socketObject.transform;
                    socket.SetParent(socketsRoot, false);
                }

                socket.localPosition = new Vector3((i - center) * socketSpacing, socketHeight, socketForwardOffset);
                socket.localRotation = Quaternion.identity;
                socket.localScale = Vector3.one;
                sockets[i] = socket;
            }

            SerializedObject serializedHub = new SerializedObject(hub);
            SerializedProperty socketsProperty = serializedHub.FindProperty("sockets");
            if (socketsProperty != null)
            {
                socketsProperty.arraySize = sockets.Length;
                for (int i = 0; i < sockets.Length; i++)
                {
                    socketsProperty.GetArrayElementAtIndex(i).objectReferenceValue = sockets[i];
                }
            }
            serializedHub.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hub);

            if (addLocalBrokenCables)
            {
                ElectricPoleLocalBrokenCables localBroken = pole.GetComponent<ElectricPoleLocalBrokenCables>();
                if (localBroken == null)
                {
                    localBroken = pole.gameObject.AddComponent<ElectricPoleLocalBrokenCables>();
                }

                SerializedObject serializedLocalBroken = new SerializedObject(localBroken);
                SetObject(serializedLocalBroken, "hub", hub);
                serializedLocalBroken.ApplyModifiedPropertiesWithoutUndo();
                localBroken.ApplySelectedDamagePreset();
                EditorUtility.SetDirty(localBroken);
            }

            return hub;
        }

        private static void SetObject(SerializedObject obj, string name, Object value)
        {
            SerializedProperty property = obj.FindProperty(name);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }
    }
}
