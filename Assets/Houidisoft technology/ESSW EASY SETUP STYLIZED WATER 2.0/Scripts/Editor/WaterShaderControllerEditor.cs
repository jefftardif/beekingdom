using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using ESSW.Welcome;

namespace ESSW.Editorcontroller
{
    [CustomEditor(typeof(WaterShaderController))]
    public class WaterShaderControllerEditor : UnityEditor.Editor
    {
        private const string UXML_PATH =
            "Assets/Houidisoft technology/ESSW EASY SETUP STYLIZED WATER 2.0/Scripts/Editor/WaterShaderControllerInspector.uxml";

        public override VisualElement CreateInspectorGUI()
        {
            var controller = (WaterShaderController)target;
            var root = new VisualElement();

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML_PATH);
            if (visualTree != null)
            {
                visualTree.CloneTree(root);
                root.Bind(serializedObject);
            }
            else
            {
                InspectorElement.FillDefaultInspector(root, serializedObject, this);
                return root;
            }

            // ===== Generate Gradient Asset =====
            var btnGenerate = root.Q<Button>("btnGenerateGradient");
            if (btnGenerate != null)
            {
                btnGenerate.clicked += () =>
                {
                    Undo.RecordObject(controller, "Generate Gradient Asset");
                    controller.RegenerateGradientAsAsset();
                };
            }

            // ===== Open Welcome Window =====
            var btnWelcome = root.Q<Button>("btnOpenWelcome");
            if (btnWelcome != null)
            {
                btnWelcome.clicked += WaterShaderWelcome.ShowWindow;
            }

            return root;
        }
    }
}