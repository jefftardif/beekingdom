using BeeKingdom.Core.Services;
using BeeKingdom.Core.Simulation;

namespace BeeKingdom.Gameplay
{
    public sealed class PlayableHiveBootstrap
    {
        private readonly NewGameInitializer initializer;

        public PlayableHiveBootstrap(NewGameInitializer initializer = null)
        {
            this.initializer = initializer ?? new NewGameInitializer();
        }

        public PlayableHiveState StartNewGame(StarterHiveProfile hiveProfile, StarterPopulationProfile populationProfile, StarterResourceProfile resourceProfile, ISimulationScheduler scheduler = null)
        {
            PlayableHiveState state = initializer.CreateNewGame(hiveProfile, populationProfile, resourceProfile);
            scheduler?.RegisterSystem(state.GrowthManager);
            scheduler?.RegisterSystem(state.TaskManager);
            scheduler?.RegisterSystem(state.AIManager);
            scheduler?.RegisterSystem(state.Controller);
            return state;
        }

        public void Save(ISaveService saveService, PlayableHiveState state, string slot)
        {
            if (saveService != null && state?.Controller != null)
            {
                saveService.Save(slot, state.Controller.CreateSavePayload());
            }
        }
    }
}
