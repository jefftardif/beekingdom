namespace BeeKingdom.Core.Services
{
    public interface IRandomService : IGameService
    {
        int Range(int minInclusive, int maxExclusive);
        float Value();
    }
}
