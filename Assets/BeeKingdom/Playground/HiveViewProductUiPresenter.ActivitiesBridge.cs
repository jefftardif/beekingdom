using UnityEngine;

namespace BeeKingdom.Playground
{
    // Minimal scoped bridge; preserve the existing in-progress presenter.
    public static partial class HiveViewProductUiPresenter
    {
        public static Texture2D PremiumTextureForActivities(string id) => GetPremiumTexture(id);
        public static Texture2D PremiumIconForActivities(string id) => GetIconTexture(id);
    }
}
