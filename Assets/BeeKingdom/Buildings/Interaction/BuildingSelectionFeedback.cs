using System;
using UnityEngine;

namespace BeeKingdom.Buildings.Interaction
{
    // Retour de sélection visuel minimal (CLICK -> SELECTION -> HIGHLIGHT).
    //
    // Ne crée aucun nouveau système de sélection : il écoute les événements existants de
    // BuildingSelectionService (SelectionChanged) et pilote BuildingSelectionHighlight
    // (l'overlay translucent doré) sur le GameObject runtime du bâtiment sélectionné.
    // Il est câblé par BuildingRuntimeViewBootstrap directement sur le GameObject du
    // contrôleur d'interaction, à la matérialisation des 14 bâtiments.
    public sealed class BuildingSelectionFeedback : MonoBehaviour, IBuildingVisualFeedback
    {
        private BuildingInteractionController _controller;
        private BuildingSelectionHighlight _highlight;
        private bool _wired;

        public bool IsShowing
        {
            get { return _highlight != null && _highlight.IsShowing; }
        }

        public void Initialize(BuildingInteractionController controller)
        {
            if (_wired) return;
            _controller = controller;
            if (_highlight == null) _highlight = gameObject.AddComponent<BuildingSelectionHighlight>();
            if (_controller != null)
            {
                _controller.Selection.SelectionChanged += OnSelectionChanged;
                _wired = true;
            }
        }

        public void Show(BuildingDefinition definition, GameObject target)
        {
            Initialize(_controller);
            if (_highlight != null && target != null) _highlight.Show(definition, target);
        }

        public void Hide()
        {
            if (_highlight != null) _highlight.Hide();
        }

        private void Awake()
        {
            Initialize(GetComponent<BuildingInteractionController>());
        }

        private void OnDestroy()
        {
            if (_controller != null && _wired)
                _controller.Selection.SelectionChanged -= OnSelectionChanged;
            _wired = false;
        }

        private void OnSelectionChanged(SelectionChangedEventArgs args)
        {
            if (_highlight == null) return;

            if (!args.IsSelected || args.Building == null)
            {
                _highlight.Hide();
                return;
            }

            GameObject target = null;
            if (_controller != null && _controller.Registry != null)
            {
                try
                {
                    target = _controller.Registry.GetGameObjectByBuildingType(args.Building.BuildingType);
                }
                catch (Exception)
                {
                    target = null;
                }
            }

            if (target == null)
            {
                _highlight.Hide();
                return;
            }

            _highlight.Show(args.Building, target);
        }
    }
}