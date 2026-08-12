using BeeKingdom.Core.Config;
using BeeKingdom.Core.Logging;
using UnityEngine;

namespace BeeKingdom.Config
{
    [CreateAssetMenu(fileName = "BeeKingdomRuntimeConfig", menuName = "Bee Kingdom/Config/Runtime Config")]
    public sealed class BeeKingdomRuntimeConfig : GameConfigAsset
    {
        [SerializeField] private BeeLogLevel minimumLogLevel = BeeLogLevel.Info;

        public BeeLogLevel MinimumLogLevel => minimumLogLevel;
    }
}
