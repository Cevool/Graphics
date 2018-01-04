﻿using UnityEditor.AnimatedValues;

namespace UnityEditor.Experimental.Rendering
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<SerializedLightLoopSettingsUI, SerializedLightLoopSettings>;

    class SerializedLightLoopSettingsUI : SerializedUIBase<SerializedLightLoopSettings>
    {
        public static CED.IDrawer SectionLightLoopSettings = CED.FoldoutGroup(
            "Light Loop Settings",
            (s, p, o) => s.isSectionExpandedLightLoopSettings,
            true,
            CED.Action(Drawer_SectionLightLoopSettings));

        public AnimBool isSectionExpandedLightLoopSettings { get { return m_AnimBools[0]; } }
        public AnimBool isSectionExpandedEnableTileAndCluster { get { return m_AnimBools[1]; } }
        public AnimBool isSectionExpandedComputeLightEvaluation { get { return m_AnimBools[2]; } }

        public SerializedLightLoopSettingsUI()
            : base(3)
        {
        }

        public override void Update()
        {
            isSectionExpandedEnableTileAndCluster.target = data.enableTileAndCluster.boolValue;
            isSectionExpandedComputeLightEvaluation.target = data.enableComputeLightEvaluation.boolValue;
            base.Update();
        }

        static void Drawer_SectionLightLoopSettings(SerializedLightLoopSettingsUI s, SerializedLightLoopSettings p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.enableTileAndCluster, _.GetContent("Enable Tile And Cluster"));
            if (EditorGUILayout.BeginFadeGroup(s.isSectionExpandedEnableTileAndCluster.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(p.enableBigTilePrepass, _.GetContent("Enable Big Tile Prepass"));
                // Allow to disable cluster for forward opaque when in forward only (option have no effect when MSAA is enabled)
                // Deferred opaque are always tiled
                EditorGUILayout.PropertyField(p.enableFptlForForwardOpaque, _.GetContent("Enable FPTL For Forward Opaque"));
                EditorGUILayout.PropertyField(p.enableComputeLightEvaluation, _.GetContent("Enable Compute Light Evaluation"));
                if (EditorGUILayout.BeginFadeGroup(s.isSectionExpandedComputeLightEvaluation.faded))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(p.enableComputeLightVariants, _.GetContent("Enable Compute Light Variants"));
                    EditorGUILayout.PropertyField(p.enableComputeMaterialVariants, _.GetContent("Enable Compute Material Variants"));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFadeGroup();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();

            EditorGUILayout.PropertyField(p.isFptlEnabled, _.GetContent("Enable FPTL"));
            EditorGUILayout.PropertyField(p.enableFptlForForwardOpaque, _.GetContent("Enable FPTL For Forward Opaque"));
        }
    }
}
