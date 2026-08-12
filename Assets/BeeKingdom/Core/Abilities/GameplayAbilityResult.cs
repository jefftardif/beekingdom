namespace BeeKingdom.Core.Abilities
{
    public readonly struct GameplayAbilityResult
    {
        public bool Success { get; }
        public GameplayAbilityState State { get; }
        public string Message { get; }

        public GameplayAbilityResult(bool success, GameplayAbilityState state, string message)
        {
            Success = success;
            State = state;
            Message = message ?? string.Empty;
        }

        public static GameplayAbilityResult Ok(GameplayAbilityState state)
        {
            return new GameplayAbilityResult(true, state, string.Empty);
        }

        public static GameplayAbilityResult Fail(GameplayAbilityState state, string message)
        {
            return new GameplayAbilityResult(false, state, message);
        }
    }
}
