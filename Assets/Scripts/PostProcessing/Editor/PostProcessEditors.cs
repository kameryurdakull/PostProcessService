using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Core.PostProcessing.Editor
{
    [CustomEditor(typeof(PostProcessPreset))]
    public sealed class PostProcessPresetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var preset = (PostProcessPreset)target;
            if (preset.Profile == null)
            {
                EditorGUILayout.HelpBox("Assign a Volume Profile before using this preset.", MessageType.Error);
                return;
            }

            if (preset.Profile.components.Count == 0)
                EditorGUILayout.HelpBox("This profile has no Volume Overrides.", MessageType.Warning);

            if (!preset.Profile.TryGet<ColorLookup>(out var lookup) || !lookup.active) return;

            if (lookup.texture.value is not Texture2D lut)
            {
                EditorGUILayout.HelpBox("Color Lookup needs a 2D LUT texture.", MessageType.Error);
                return;
            }

            if (lut.width != lut.height * lut.height)
                EditorGUILayout.HelpBox("The LUT width must equal its height squared.", MessageType.Error);
        }
    }

    [CustomEditor(typeof(PostProcessVolumeExtension))]
    public sealed class PostProcessVolumeExtensionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var service = (PostProcessVolumeExtension)target;
            if (!service.TryGetComponent<Volume>(out var volume) || volume.sharedProfile == null)
            {
                EditorGUILayout.HelpBox("The service needs a Volume with a Profile.", MessageType.Error);
                return;
            }

            if (!HasPostProcessingCamera(service.gameObject.layer))
            {
                EditorGUILayout.HelpBox(
                    "No active camera currently renders post-processing from this Volume layer.",
                    MessageType.Warning);
            }
        }

        private static bool HasPostProcessingCamera(int volumeLayer)
        {
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            for (var index = 0; index < cameras.Length; index++)
            {
                if (!cameras[index].TryGetComponent<UniversalAdditionalCameraData>(out var data)) continue;
                if (!data.renderPostProcessing) continue;
                if ((data.volumeLayerMask.value & (1 << volumeLayer)) == 0) continue;

                return true;
            }

            return false;
        }
    }
}
