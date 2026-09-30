using System.Reflection;
using Game.Spline;
using UnityEngine;

namespace Polyzero.ElectricCables.Editor
{
    public static class ElectricCableEditorReflection
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        public static void ApplyProfileToGenerator(ElectricPoleChainCableGenerator generator, ElectricCableProfile profile)
        {
            if (generator == null || profile == null)
            {
                return;
            }

            Set(generator, "segments", profile.segments);
            Set(generator, "sag", profile.sag);
            Set(generator, "autoSagFromDistance", profile.autoSagFromDistance);
            Set(generator, "sagPerMeter", profile.sagPerMeter);
            Set(generator, "maxAutoSag", profile.maxAutoSag);
            Set(generator, "cableWidth", profile.cableWidth);
            Set(generator, "radialSegments", profile.radialSegments);
            Set(generator, "cableMaterial", profile.cableMaterial);
            Set(generator, "replaceUnsupportedMaterial", profile.replaceUnsupportedMaterial);
            Set(generator, "createPipelineCompatibleMaterialWhenEmpty", profile.createPipelineCompatibleMaterialWhenEmpty);
            Set(generator, "cableColor", profile.cableColor);
            Set(generator, "cableSmoothness", profile.cableSmoothness);
            Set(generator, "windSway", profile.windSway);
            Set(generator, "windAmplitude", profile.windAmplitude);
            Set(generator, "windSpeed", profile.windSpeed);
            Set(generator, "windSpatialFrequency", profile.windSpatialFrequency);
            Set(generator, "windDirection", profile.windDirection);
            Set(generator, "addPerCableWindVariation", profile.addPerCableWindVariation);
        }

        public static void ApplyProfileToCable(ElectricCableSpline cable, ElectricCableProfile profile)
        {
            if (cable == null || profile == null)
            {
                return;
            }

            Set(cable, "segments", profile.segments);
            Set(cable, "sag", profile.sag);
            Set(cable, "autoSagFromDistance", profile.autoSagFromDistance);
            Set(cable, "sagPerMeter", profile.sagPerMeter);
            Set(cable, "maxAutoSag", profile.maxAutoSag);
            Set(cable, "cableWidth", profile.cableWidth);
            Set(cable, "radialSegments", profile.radialSegments);
            Set(cable, "cableMaterial", profile.cableMaterial);
            Set(cable, "cableColor", profile.cableColor);
            Set(cable, "windSway", profile.windSway);
            Set(cable, "windAmplitude", profile.windAmplitude);
            Set(cable, "windSpeed", profile.windSpeed);
            Set(cable, "windSpatialFrequency", profile.windSpatialFrequency);
            Set(cable, "windDirection", profile.windDirection);
            Set(cable, "useDistanceLod", profile.useDistanceLod);
            Set(cable, "nearDistance", profile.nearDistance);
            Set(cable, "midDistance", profile.midDistance);
            Set(cable, "farDistance", profile.farDistance);
            Set(cable, "nearSegments", profile.nearSegments);
            Set(cable, "midSegments", profile.midSegments);
            Set(cable, "farSegments", profile.farSegments);
            Set(cable, "nearRadialSegments", profile.nearRadialSegments);
            Set(cable, "midRadialSegments", profile.midRadialSegments);
            Set(cable, "farRadialSegments", profile.farRadialSegments);
            Set(cable, "disableBeyondFarDistance", profile.disableBeyondFarDistance);
            Set(cable, "useBreakGapMeters", profile.useBreakGapMeters);
            Set(cable, "breakGapMeters", profile.breakGapMeters);
            Set(cable, "breakGapNormalized", profile.breakGapNormalized);
            Set(cable, "brokenLooseEndLength", profile.brokenLooseEndLength);
            Set(cable, "brokenEndDrop", profile.brokenEndDrop);
            Set(cable, "brokenEndCurl", profile.brokenEndCurl);
            Set(cable, "brokenEndSideOffset", profile.brokenEndSideOffset);
            cable.Rebuild();
        }

        public static void Set(Object target, string fieldName, object value)
        {
            if (target == null)
            {
                return;
            }

            FieldInfo field = target.GetType().GetField(fieldName, Flags);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        public static T Get<T>(Object target, string fieldName, T fallback)
        {
            if (target == null)
            {
                return fallback;
            }

            FieldInfo field = target.GetType().GetField(fieldName, Flags);
            if (field == null || field.GetValue(target) is not T value)
            {
                return fallback;
            }

            return value;
        }
    }
}
