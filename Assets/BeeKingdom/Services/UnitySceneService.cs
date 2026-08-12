using BeeKingdom.Core.Services;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Services
{
    public sealed class UnitySceneService : GameServiceBase, ISceneService
    {
        public override int Priority => 70;

        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
