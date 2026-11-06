using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.EditorTests
{
    /// <summary>
    /// Tests the actual bundled artwork that the official WorldMap
    /// attempts to render (not a mock sprite or a design preview).
    /// </summary>
    public sealed class PollinationFloralArtworkEditorTests
    {
        [Test]
        public void PollinationFloralArtworkIsImportedAndSizedForMobile()
        {
            string[] names = {
                "field_bloom", "field_recovering", "field_resting", "forager"
            };
            foreach (string name in names)
            {
                Texture2D texture = Resources.Load<Texture2D>("Pollination/" + name);
                Assert.IsNotNull(texture, "Missing bundled pollination artwork " + name);
                Assert.GreaterOrEqual(texture.width, 80, name);
                Assert.GreaterOrEqual(texture.height, 60, name);
                Assert.LessOrEqual(texture.width, 512, "Oversized mobile texture: " + name);
                Assert.LessOrEqual(texture.height, 512, "Oversized mobile texture: " + name);
            }
        }
    }
}
