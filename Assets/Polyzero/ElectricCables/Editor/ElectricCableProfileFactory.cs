using System.IO;
using Game.Spline;
using UnityEditor;
using UnityEngine;

namespace Polyzero.ElectricCables.Editor
{
    public static class ElectricCableProfileFactory
    {
        private const string Folder = "Assets/Polyzero/ElectricCables/Profiles";

        [MenuItem("Tools/Polyzero/Electric Cables/Create Default Cable Profiles")]
        public static void CreateDefaultProfiles()
        {
            EnsureFolder("Assets/Polyzero", "ElectricCables");
            EnsureFolder("Assets/Polyzero/ElectricCables", "Profiles");

            CreateOrUpdate("Thin_Power_Cable", profile =>
            {
                profile.cableWidth = 0.035f;
                profile.segments = 24;
                profile.radialSegments = 8;
                profile.sag = 0.38f;
                profile.sagPerMeter = 0.06f;
                profile.maxAutoSag = 1.8f;
                profile.windAmplitude = 0.035f;
                profile.windSpeed = 3.1f;
                profile.windSpatialFrequency = 0.16f;
                profile.addPerCableWindVariation = true;
            });

            CreateOrUpdate("Thick_Power_Cable", profile =>
            {
                profile.cableWidth = 0.065f;
                profile.segments = 28;
                profile.radialSegments = 10;
                profile.sag = 0.52f;
                profile.sagPerMeter = 0.075f;
                profile.maxAutoSag = 2.4f;
                profile.windAmplitude = 0.025f;
                profile.windSpeed = 2.4f;
                profile.windSpatialFrequency = 0.12f;
                profile.addPerCableWindVariation = true;
            });

            CreateOrUpdate("Loose_Old_Cable", profile =>
            {
                profile.cableWidth = 0.045f;
                profile.segments = 32;
                profile.radialSegments = 8;
                profile.sag = 0.8f;
                profile.sagPerMeter = 0.11f;
                profile.maxAutoSag = 3.8f;
                profile.windAmplitude = 0.055f;
                profile.windSpeed = 2.8f;
                profile.windSpatialFrequency = 0.11f;
                profile.breakGapMeters = 0.6f;
                profile.brokenLooseEndLength = 1.6f;
                profile.brokenEndDrop = 1.05f;
                profile.brokenEndCurl = 0.28f;
                profile.addPerCableWindVariation = true;
            });

            CreateOrUpdate("Telephone_Wire", profile =>
            {
                profile.cableWidth = 0.025f;
                profile.segments = 20;
                profile.radialSegments = 6;
                profile.sag = 0.28f;
                profile.sagPerMeter = 0.045f;
                profile.maxAutoSag = 1.4f;
                profile.windAmplitude = 0.045f;
                profile.windSpeed = 3.5f;
                profile.windSpatialFrequency = 0.2f;
                profile.addPerCableWindVariation = true;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Created/updated default Electric Cable Profiles in " + Folder);
        }

        private static void CreateOrUpdate(string assetName, System.Action<ElectricCableProfile> configure)
        {
            string path = Path.Combine(Folder, assetName + ".asset").Replace("\\", "/");
            ElectricCableProfile profile = AssetDatabase.LoadAssetAtPath<ElectricCableProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<ElectricCableProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            configure(profile);
            EditorUtility.SetDirty(profile);
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string path = parent + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
