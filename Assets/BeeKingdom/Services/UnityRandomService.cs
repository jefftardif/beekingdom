using BeeKingdom.Core.Services;
using UnityEngine;

namespace BeeKingdom.Services
{
    public sealed class UnityRandomService : GameServiceBase, IRandomService
    {
        public override int Priority => 30;

        public int Range(int minInclusive, int maxExclusive)
        {
            return Random.Range(minInclusive, maxExclusive);
        }

        public float Value()
        {
            return Random.value;
        }
    }
}
