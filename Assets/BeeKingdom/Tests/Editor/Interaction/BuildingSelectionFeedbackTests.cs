using BeeKingdom.Buildings.Interaction;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Tests.Editor.Interaction
{
    public class BuildingSelectionFeedbackTests
    {
        private GameObject _controllerGo;
        private GameObject _feedbackGo;
        private BuildingInteractionController _controller;
        private BuildingSelectionFeedback _feedback;

        private void Setup()
        {
            _controllerGo = new GameObject("TestController");
            _controller = _controllerGo.AddComponent<BuildingInteractionController>();
            _feedbackGo = new GameObject("TestFeedback");
            _feedbackGo.transform.SetParent(_controllerGo.transform, false);
            _feedback = _feedbackGo.AddComponent<BuildingSelectionFeedback>();
            _feedback.Initialize(_controller);
        }

        [TearDown]
        public void TearDown()
        {
            if (_feedback != null && _feedback.IsShowing) _feedback.Hide();
            foreach (UnityEngine.SceneManagement.Scene scene in UnityEngine.SceneManagement.SceneManager.GetAllScenes())
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root != null && root.name.StartsWith("RuntimeVisual_"))
                        Object.DestroyImmediate(root);
                }
            }
            if (_controllerGo != null) Object.DestroyImmediate(_controllerGo);
            _controllerGo = null;
            _feedbackGo = null;
            _controller = null;
            _feedback = null;
        }

        private static GameObject FindOverlay(GameObject building)
        {
            if (building == null) return null;
            Transform[] children = building.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == "SelectionOverlay")
                    return children[i].gameObject;
            }
            return null;
        }

        [Test]
        public void InteractionLayerMaskIsolation()
        {
            int layer = LayerMask.NameToLayer("BuildingInteraction");
            Assert.That(layer, Is.EqualTo(BuildingInteractionController.BuildingLayerIndex),
                "TagManager doit définir le layer 'BuildingInteraction'.");
            Assert.That(BuildingInteractionController.InteractionLayer, Is.EqualTo(layer));
            Assert.That(BuildingInteractionController.InteractionLayerMask, Is.EqualTo(1 << layer));
            Assert.That(BuildingInteractionController.InteractionLayerMask &
                        (1 << LayerMask.NameToLayer("Default")), Is.Zero,
                "Le masque d'interaction ne doit PAS inclure le layer Default (décor).");
        }

        [Test]
        public void SelectShowsHighlightOnRegisteredBuilding()
        {
            Setup();
            GameObject building = new GameObject("RuntimeVisual_NURSERY");
            _controller.Registry.Register(building, BuildingTypes.Nursery);

            _controller.Selection.Select(BuildingCatalog.GetByBuildingType(BuildingTypes.Nursery));

            Assert.That(_feedback.IsShowing, Is.True,
                "Sélectionner un bâtiment doit activer le highlight.");
            GameObject overlay = FindOverlay(building);
            Assert.That(overlay, Is.Not.Null,
                "L'overlay de sélection doit être enfant du bâtiment sélectionné.");
            Assert.That(overlay.transform.parent, Is.SameAs(building.transform));
        }

        [Test]
        public void SwitchingSelectionMovesHighlight()
        {
            Setup();
            GameObject bank = new GameObject("RuntimeVisual_BANK");
            _controller.Registry.Register(bank, BuildingTypes.Bank);
            GameObject academy = new GameObject("RuntimeVisual_ACADEMY");
            _controller.Registry.Register(academy, BuildingTypes.Academy);

            _controller.Selection.Select(BuildingCatalog.GetByBuildingType(BuildingTypes.Bank));
            Assert.That(FindOverlay(bank), Is.Not.Null, "BANK doit être surligné.");
            Assert.That(FindOverlay(academy), Is.Null, "ACADEMY ne doit pas être surligné.");

            _controller.Selection.Select(BuildingCatalog.GetByBuildingType(BuildingTypes.Academy));
            Assert.That(FindOverlay(academy), Is.Not.Null, "ACADEMY doit devenir surligné.");
            Assert.That(FindOverlay(bank), Is.Null, "BANK ne doit plus être surligné.");
        }

        [Test]
        public void DeselectHidesHighlight()
        {
            Setup();
            GameObject building = new GameObject("RuntimeVisual_MISC");
            _controller.Registry.Register(building, BuildingTypes.Warehouse);

            _controller.Selection.Select(BuildingCatalog.GetByBuildingType(BuildingTypes.Warehouse));
            Assert.That(_feedback.IsShowing, Is.True);

            _controller.Selection.Deselect();
            Assert.That(_feedback.IsShowing, Is.False,
                "Désélectionner doit masquer le highlight.");
        }
    }
}