using NUnit.Framework;
using System;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxPremiumRuntimeIntegrationTests
    {
        [TearDown]
        public void TearDown()
        {
            DestroyRuntimeRoot();
        }

        [Test]
        public void HivePresenterCreatesPremiumRuntimeHiveForSandboxPlayMode()
        {
            DestroyRuntimeRoot();

            HiveViewProductUiPresenter.EnsureSceneObjects();

            GameObject root = GameObject.Find(HiveViewProductUiPresenter.RootName);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.transform.childCount, Is.GreaterThan(20));
            Assert.That(root.GetComponentsInChildren<HiveViewCellMarker>(true).Length, Is.GreaterThanOrEqualTo(15));
            Assert.That(ContainsChild(root.transform, "Hive Hex Outer Wax Rim"), Is.True);
            Assert.That(ContainsChild(root.transform, "Hive Hex Left Wax Wall"), Is.True);
            Assert.That(ContainsChild(root.transform, "Hive Zone Landmark Honey Specular"), Is.True);
            Assert.That(ContainsChild(root.transform, "Hive Queen Core Product Marker"), Is.True);
        }

        public static void ValidatePremiumRuntimeIntegration()
        {
            DestroyRuntimeRoot();
            HiveViewProductUiPresenter.EnsureSceneObjects();

            GameObject root = GameObject.Find(HiveViewProductUiPresenter.RootName);
            Ensure(root != null, "Premium runtime root was not created.");
            Ensure(root.transform.childCount > 20, "Premium runtime root does not contain enough visual children.");
            Ensure(root.GetComponentsInChildren<HiveViewCellMarker>(true).Length >= 15, "Premium runtime cell markers are missing.");
            Ensure(ContainsChild(root.transform, "Hive Hex Outer Wax Rim"), "Premium wax rims are missing.");
            Ensure(ContainsChild(root.transform, "Hive Hex Left Wax Wall"), "Premium wax depth walls are missing.");
            Ensure(ContainsChild(root.transform, "Hive Zone Landmark Honey Specular"), "Premium honey highlights are missing.");
            Ensure(ContainsChild(root.transform, "Hive Queen Core Product Marker"), "Premium queen core marker is missing.");
            Debug.Log("Sandbox premium runtime integration validation completed.");
            DestroyRuntimeRoot();
        }

        private static void Ensure(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static bool ContainsChild(Transform root, string name)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name.StartsWith(name, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void DestroyRuntimeRoot()
        {
            GameObject root = GameObject.Find(HiveViewProductUiPresenter.RootName);
            if (root != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
