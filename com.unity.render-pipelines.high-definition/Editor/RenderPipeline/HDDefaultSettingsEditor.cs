using UnityEngine.Rendering.HighDefinition;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(HDGlobalSettings))]
    [CanEditMultipleObjects]
    sealed class HDGlobalSettingsEditor : Editor
    {
        SerializedHDGlobalSettings m_SerializedHDGlobalSettings;

        internal bool largeLabelWidth = true;

        void OnEnable()
        {
            m_SerializedHDGlobalSettings = new SerializedHDGlobalSettings(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            var serialized = m_SerializedHDGlobalSettings;

            serialized.serializedObject.Update();

            // In the quality window use more space for the labels
            if (!largeLabelWidth)
                EditorGUIUtility.labelWidth *= 2;
            DefaultSettingsPanelIMGUI.Inspector.Draw(serialized, this);
            if (!largeLabelWidth)
                EditorGUIUtility.labelWidth *= 0.5f;

            serialized.serializedObject.ApplyModifiedProperties();
        }
    }
}
