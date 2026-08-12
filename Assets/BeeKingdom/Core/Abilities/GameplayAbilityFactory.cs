namespace BeeKingdom.Core.Abilities
{
    public sealed class GameplayAbilityFactory
    {
        private long nextHandle = 1L;

        public GameplayAbilityInstance Create(GameplayAbilityDefinition definition, GameplayAbilityContext context)
        {
            return new GameplayAbilityInstance(new GameplayAbilityHandle(nextHandle++), definition, context);
        }
    }
}
