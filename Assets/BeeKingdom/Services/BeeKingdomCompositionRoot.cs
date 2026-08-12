using System.Collections.Generic;
using System.IO;
using BeeKingdom.Config;
using BeeKingdom.Config.Loaders;
using BeeKingdom.Config.Runtime;
using BeeKingdom.Config.Validators;
using BeeKingdom.Core.Config;
using BeeKingdom.Core.Logging;
using BeeKingdom.Core.Save;
using BeeKingdom.Core.Services;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Data;
using UnityEngine;

namespace BeeKingdom.Services
{
    /// <summary>
    /// Single composition point for the new architecture.
    /// This class wires infrastructure services only; gameplay systems are intentionally not created here yet.
    /// </summary>
    public sealed class BeeKingdomCompositionRoot : MonoBehaviour
    {
        [SerializeField] private BeeKingdomRuntimeConfig runtimeConfig;
        [SerializeField] private List<GameConfigAsset> configAssets = new List<GameConfigAsset>();
        [SerializeField] private BeeLogLevel fallbackMinimumLogLevel = BeeLogLevel.Info;

        private ServiceContainer container;
        private ServiceLifecycleOrchestrator lifecycle;
        private bool isPaused;

        public IServiceRegistry Services => container;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Compose();
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private void Compose()
        {
            if (container != null)
            {
                return;
            }

            container = new ServiceContainer();
            BeeLogLevel minimumLogLevel = runtimeConfig != null ? runtimeConfig.MinimumLogLevel : fallbackMinimumLogLevel;
            IBeeLogger logger = new UnityBeeLogger(minimumLogLevel);
            container.Register(logger);
            lifecycle = new ServiceLifecycleOrchestrator(container, logger);

            if (runtimeConfig != null && !configAssets.Contains(runtimeConfig))
            {
                configAssets.Add(runtimeConfig);
            }

            lifecycle.Register<IConfigurationService>(
                new ConfigurationService(
                    new ResourcesConfigurationLoader(),
                    new ConfigurationValidator(),
                    new ConfigurationCache()
                )
            );
            lifecycle.Register<IDataRegistry>(new DataRegistry());
            lifecycle.Register<IConfigService>(new ConfigService(configAssets));
            lifecycle.Register<ITimeService>(new UnityTimeService());
            lifecycle.Register<IRandomService>(new UnityRandomService());
            lifecycle.Register<IEventBus>(new EventBus());
            lifecycle.Register<ISimulationScheduler>(new SimulationScheduler());
            string savePath = Path.Combine(Application.persistentDataPath, "Saves");
            lifecycle.Register<ISaveService>(new SaveEngine(new FileSaveRepository(savePath)));
            lifecycle.Register<IAudioService>(new NullAudioService());
            lifecycle.Register<ISceneService>(new UnitySceneService());
            lifecycle.Register<ISimulationEngine>(new SimulationEngine());
            lifecycle.Bootstrap();

            logger.Log(BeeLogLevel.Info, "Composition root initialized.");
        }

        private void Update()
        {
            if (!isPaused && lifecycle != null)
            {
                lifecycle.Tick(Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
            if (!isPaused && lifecycle != null)
            {
                lifecycle.FixedTick(Time.fixedDeltaTime);
            }
        }

        private void LateUpdate()
        {
            if (!isPaused && lifecycle != null)
            {
                lifecycle.LateTick(Time.deltaTime);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (lifecycle == null || isPaused == pauseStatus)
            {
                return;
            }

            isPaused = pauseStatus;
            if (pauseStatus)
            {
                lifecycle.Pause();
            }
            else
            {
                lifecycle.Resume();
            }
        }

        private void Shutdown()
        {
            if (container == null)
            {
                return;
            }

            lifecycle?.Shutdown();
            lifecycle = null;
            container.Clear();
            container = null;
        }
    }
}
