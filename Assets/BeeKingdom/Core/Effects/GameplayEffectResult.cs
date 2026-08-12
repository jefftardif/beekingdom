namespace BeeKingdom.Core.Effects
{
    public readonly struct GameplayEffectResult
    {
        public bool Success { get; }
        public GameplayEffectState State { get; }
        public string Message { get; }

        public GameplayEffectResult(bool success, GameplayEffectState state, string message)
        {
            Success = success;
            State = state;
            Message = message ?? string.Empty;
        }

        public static GameplayEffectResult Ok(GameplayEffectState state) => new GameplayEffectResult(true, state, string.Empty);
        public static GameplayEffectResult Fail(GameplayEffectState state, string message) => new GameplayEffectResult(false, state, message);
    }
}
