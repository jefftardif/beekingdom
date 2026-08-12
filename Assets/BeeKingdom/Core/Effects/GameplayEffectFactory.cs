namespace BeeKingdom.Core.Effects
{
    public sealed class GameplayEffectFactory
    {
        private long nextHandle = 1L;

        public GameplayEffectInstance Create(GameplayEffectDefinition definition, GameplayEffectContext context)
        {
            return new GameplayEffectInstance(new GameplayEffectHandle(nextHandle++), definition, context);
        }
    }
}
