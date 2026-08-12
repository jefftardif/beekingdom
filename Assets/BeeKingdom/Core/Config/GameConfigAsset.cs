using UnityEngine;

namespace BeeKingdom.Core.Config
{
    /// <summary>
    /// Base class for data-driven configuration assets.
    /// Concrete configs should inherit from this rather than hardcoding tuning values.
    /// </summary>
    public abstract class GameConfigAsset : ScriptableObject
    {
        [SerializeField] private string configId;

        public string ConfigId => configId;
    }
}
