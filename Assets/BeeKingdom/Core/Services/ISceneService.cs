namespace BeeKingdom.Core.Services
{
    public interface ISceneService : IGameService
    {
        void LoadScene(string sceneName);
    }
}
