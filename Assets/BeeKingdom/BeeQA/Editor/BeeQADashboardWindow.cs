using UnityEditor;
using UnityEngine;

namespace BeeKingdom.BeeQA
{
    public sealed class BeeQADashboardWindow : EditorWindow
    {
        private Vector2 scroll;

        public static void Open()
        {
            BeeQADashboardWindow window = GetWindow<BeeQADashboardWindow>("BeeQA");
            window.minSize = new Vector2(640f, 460f);
            window.Show();
        }

        private void OnEnable()
        {
            BeeQAModuleRegistry.EnsureDiscovered();
        }

        private void OnGUI()
        {
            if (!BeeQAEntryPoint.IsDebugAvailable)
            {
                EditorGUILayout.HelpBox("BeeQA est disponible uniquement dans l'Editor ou une build Debug.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("BeeQA", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("QA Module Framework", EditorStyles.miniLabel);
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", GUILayout.Width(80f)))
                {
                    BeeQAModuleRegistry.RefreshDiscovery();
                    Repaint();
                }
                if (GUILayout.Button("Run All", GUILayout.Width(80f)))
                {
                    BeeQAModuleRegistry.RunAll();
                    Repaint();
                }
            }
            EditorGUILayout.HelpBox("Les modules sont découverts automatiquement. Le Dashboard ne référence aucun module concret.", MessageType.Info);
            EditorGUILayout.Space(6f);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Modules", EditorStyles.boldLabel);
            if (BeeQAModuleRegistry.Modules.Count == 0)
            {
                EditorGUILayout.HelpBox("Aucun module QA enregistré.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < BeeQAModuleRegistry.Modules.Count; i++)
                    DrawModule(BeeQAModuleRegistry.Modules[i]);
            }

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Catégories disponibles", EditorStyles.boldLabel);
            for (int i = 0; i < BeeQACatalog.Categories.Count; i++)
            {
                BeeQACategoryDefinition category = BeeQACatalog.Categories[i];
                EditorGUILayout.LabelField(category.DisplayName, "Modules: " + BeeQAModuleRegistry.CountFor(category.Id));
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawModule(IBeeQAModule module)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(module.DisplayName, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(!module.CanExecute || module.Status == BeeQAModuleStatus.Running))
                    {
                        if (GUILayout.Button("Run", GUILayout.Width(70f)))
                        {
                            BeeQAModuleRegistry.Run(module);
                            Repaint();
                        }
                    }
                }
                EditorGUILayout.LabelField(module.Description, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("Catégorie", module.Category.ToString());
                EditorGUILayout.LabelField("Version", module.Version);
                EditorGUILayout.LabelField("Auteur", module.Author);
                EditorGUILayout.LabelField("Statut", module.Status.ToString());

                BeeQAResult result = module.LastResult;
                if (result == null)
                {
                    EditorGUILayout.LabelField("Dernier résultat", "Jamais exécuté");
                }
                else
                {
                    EditorGUILayout.LabelField("Dernier résultat", result.Status);
                    EditorGUILayout.LabelField("Durée", result.DurationMilliseconds.ToString("0.###") + " ms");
                    EditorGUILayout.LabelField("Date", result.UtcDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
                    EditorGUILayout.LabelField("Message", result.Message, EditorStyles.wordWrappedMiniLabel);
                }
            }
            EditorGUILayout.Space(4f);
        }
    }
}
