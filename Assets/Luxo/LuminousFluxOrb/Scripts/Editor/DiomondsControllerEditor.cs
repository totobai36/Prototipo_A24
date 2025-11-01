#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace LuminousFluxOrb
{
    [CustomEditor(typeof(DiomondsController))]
    public class DiomondsControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DiomondsController controller = (DiomondsController)target;

            if (GUILayout.Button("Reinitialize Now"))
            {
                controller.Reinitialize();
            }

            EditorGUILayout.Space();
        }
    }
}
#endif