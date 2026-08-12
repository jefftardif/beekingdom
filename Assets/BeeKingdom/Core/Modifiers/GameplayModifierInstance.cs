namespace BeeKingdom.Core.Modifiers
{
    public sealed class GameplayModifierInstance
    {
        public GameplayModifierDefinition Definition { get; }
        public bool IsEnabled { get; private set; }

        public GameplayModifierInstance(GameplayModifierDefinition definition)
        {
            Definition = definition;
            IsEnabled = true;
        }

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
        }
    }
}
