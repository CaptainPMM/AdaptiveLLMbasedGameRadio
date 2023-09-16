using UnityEngine;
using UnityEditor;

namespace AdBlocker.FMOD.Radio.RadioStations.Editors {
    [CustomEditor(typeof(RadioStation))]
    public class RadioStationEditor : Editor {
        public override void OnInspectorGUI() {
            if (GUILayout.Button("Open Seperate Window", GUILayout.Height(75f))) EditorUtility.OpenPropertyEditor(target);
            base.OnInspectorGUI();
        }
    }
}