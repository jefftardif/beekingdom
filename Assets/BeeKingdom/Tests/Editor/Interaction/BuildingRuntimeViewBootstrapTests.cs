using BeeKingdom.Buildings.Interaction;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Tests.Editor.Interaction
{
    public class BuildingRuntimeViewBootstrapTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.SceneManagement.Scene scene in UnityEngine.SceneManagement.SceneManager.GetAllScenes())
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root == null) continue;
                    if (root.name.StartsWith("RuntimeVisual_"))
                    {
                        Object.DestroyImmediate(root);
                        continue;
                    }
                    if (root.name == "BaseDisc" || root.name == "Tip")
                    {
                        Object.DestroyImmediate(root);
                        continue;
                    }
                    DestroyRuntimeVisualChildren(root);
                }
            }

            GameObject[] markers = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            for (int i = 0; i < markers.Length; i++)
            {
                if (markers[i] != null && markers[i].name.StartsWith("SurfaceRep", System.StringComparison.Ordinal))
                    Object.DestroyImmediate(markers[i]);
            }
        }

        private static void DestroyRuntimeVisualChildren(GameObject go)
        {
            if (go == null) return;
            for (int i = go.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = go.transform.GetChild(i).gameObject;
                if (child.name.StartsWith("RuntimeVisual_")) Object.DestroyImmediate(child);
            }
        }

        [Test]
        public void MaterializeRuntimeVisualBuildingsCreatesFourteen()
        {
            BuildingInteractionRegistry registry = new BuildingInteractionRegistry();
            int materialized = BuildingRuntimeViewBootstrap.MaterializeRuntimeVisualBuildings(registry);
            Assert.That(materialized, Is.EqualTo(14),
                "Le sidecar réel doit matérialiser 14 bâtiments visibles.");
            Assert.That(registry.Count, Is.EqualTo(14));
        }

        [Test]
        public void MaterializedVisualsCarryRendererColliderAndInteraction()
        {
            BuildingInteractionRegistry registry = new BuildingInteractionRegistry();
            BuildingRuntimeViewBootstrap.MaterializeRuntimeVisualBuildings(registry);

            for (int i = 0; i < BuildingTypes.All.Length; i++)
            {
                GameObject go = registry.GetGameObjectByBuildingType(BuildingTypes.All[i]);
                Assert.That(go, Is.Not.Null, "Bâtiment manquant : " + BuildingTypes.All[i]);
                Assert.That(go.name.StartsWith("RuntimeVisual_", System.StringComparison.Ordinal), Is.True);

                MeshRenderer meshRenderer = go.GetComponentInChildren<MeshRenderer>(true);
                Assert.That(meshRenderer, Is.Not.Null,
                    "Le bâtiment " + BuildingTypes.All[i] + " doit être visible (MeshRenderer).");

                MeshFilter meshFilter = go.GetComponentInChildren<MeshFilter>(true);
                Assert.That(meshFilter, Is.Not.Null);
                Assert.That(meshFilter.sharedMesh, Is.Not.Null);
                Assert.That(meshFilter.sharedMesh.vertexCount, Is.EqualTo(4));

                BoxCollider collider = go.GetComponent<BoxCollider>();
                Assert.That(collider, Is.Not.Null,
                    "Le bâtiment " + BuildingTypes.All[i] + " doit être cliquable (BoxCollider).");

                BuildingInteractionComponent interaction = go.GetComponent<BuildingInteractionComponent>();
                Assert.That(interaction, Is.Not.Null);
                Assert.That(interaction.BuildingType, Is.EqualTo(BuildingTypes.All[i]));
            }
        }

        [Test]
        public void HideDevMarkersDisablesSurfaceReperesOnly()
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            marker.name = "SurfaceRep\x00E8re_TEST";
            marker.transform.position = new Vector3(0f, 30f, 30f);

            GameObject unrelated = GameObject.CreatePrimitive(PrimitiveType.Quad);
            unrelated.name = "NotADevMarker";
            unrelated.transform.position = new Vector3(100f, 0f, 0f);

            int hidden = BuildingRuntimeViewBootstrap.HideDevMarkers();

            Assert.That(hidden, Is.GreaterThanOrEqualTo(1));
            Assert.That(marker.GetComponent<Renderer>().enabled, Is.False,
                "La croix marqueur SurfaceRepère doit être masquée.");
            Assert.That(unrelated.GetComponent<Renderer>().enabled, Is.True,
                "Un objet non-dev ne doit pas être masqué.");
        }

        [Test]
        public void HideDevMarkersHidesBaseDiscAndTip()
        {
            GameObject baseDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseDisc.name = "BaseDisc";
            baseDisc.transform.position = new Vector3(0f, 30f, 30f);

            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = "Tip";
            tip.transform.position = new Vector3(0f, 32f, 30f);

            int hidden = BuildingRuntimeViewBootstrap.HideDevMarkers();

            Assert.That(hidden, Is.GreaterThanOrEqualTo(1));
            Assert.That(baseDisc.GetComponent<Renderer>().enabled, Is.False,
                "BaseDisc (base du marqueur) doit être masqué.");
            Assert.That(tip.GetComponent<Renderer>().enabled, Is.False,
                "Tip (tête du marqueur) doit être masqué.");
        }

        [Test]
        public void MaterializedVisualsUseDedicatedInteractionLayer()
        {
            BuildingInteractionRegistry registry = new BuildingInteractionRegistry();
            BuildingRuntimeViewBootstrap.MaterializeRuntimeVisualBuildings(registry);

            Assert.That(LayerMask.NameToLayer("BuildingInteraction"),
                Is.EqualTo(BuildingInteractionController.BuildingLayerIndex),
                "Le layer 'BuildingInteraction' doit exister dans TagManager.");
            Assert.That(BuildingInteractionController.InteractionLayerMask,
                Is.EqualTo(1 << BuildingInteractionController.InteractionLayer),
                "Le raycast d'interaction doit considérer uniquement le layer bâtiment.");

            for (int i = 0; i < BuildingTypes.All.Length; i++)
            {
                GameObject go = registry.GetGameObjectByBuildingType(BuildingTypes.All[i]);
                Assert.That(go.layer, Is.EqualTo(BuildingInteractionController.InteractionLayer),
                    "Les bâtiments runtime doivent porter le layer d'interaction dédié : " + BuildingTypes.All[i]);
            }
        }
    }
}