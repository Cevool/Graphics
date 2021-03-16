using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    class ProbeVolumeMenuItems
    {
        [MenuItem("GameObject/Light/Experimental/Probe Volume", priority = CoreUtils.Sections.section8)]
        static void CreateProbeVolumeGameObject(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var probeVolume = CoreEditorUtils.CreateGameObject("Probe Volume", parent);
            probeVolume.AddComponent<ProbeVolume>();
        }

        [MenuItem("GameObject/Light/Experimental/Probe Hint Volume", priority = CoreUtils.Sections.section8)]
        static void CreateProbeHintVolumeGameObject(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var densityVolume = CoreEditorUtils.CreateGameObject("Probe Hint Volume", parent);
            densityVolume.AddComponent<ProbeHintVolume>();
        }
    }
}
