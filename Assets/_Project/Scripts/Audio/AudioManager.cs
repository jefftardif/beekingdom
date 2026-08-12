using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace BeeKingdom.Audio
{
    /// <summary>
    /// Simple Audio Manager pour Bee Kingdom
    /// BONUS - Pour Session 4: Sons et Musique
    /// 
    /// Usage:
    /// AudioManager.Instance.PlaySound("bee_recruited");
    /// AudioManager.Instance.PlayMusic("background_music");
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        #region Singleton

        public static AudioManager Instance { get; private set; }

        /// <summary>
        /// Garantit qu'un AudioManager existe, meme si la scene active n'en contient pas
        /// (ex: la scene _Boot n'est pas chargee dans le flux de jeu reel).
        /// </summary>
        public static AudioManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("AudioManager");
            return go.AddComponent<AudioManager>();
        }

        /// <summary>
        /// Garantit qu'un AudioManager existe avant meme le chargement de la toute premiere scene
        /// (meme convention que MusicManager) - AudioManager.Instance ne devrait donc jamais etre
        /// null en pratique, y compris pour les sons d'interface appeles des le premier ecran.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            CreateAudioSourcesIfMissing();
        }

        #endregion

        #region Configuration

        private const string UiSoundLibraryResourcePath = "UiSoundLibrary";

        // Son du MONDE (jamais UI) - collecte de miel/cire/pollen sur les batiments de la ruche.
        // Charge automatiquement depuis Resources (meme convention que UiSoundLibrary) pour ne
        // jamais dependre d'une assignation manuelle dans l'Inspecteur, qui n'etait pas maintenue
        // pour les autres champs de sons de gameplay (tous a fileID 0 avant ce correctif).
        private const string ResourceGainSoundResourcePath = "collect";

        // Delai minimal entre deux lectures du MEME son d'interface - absorbe les doubles
        // declenchements (double-clic, plusieurs boutons presses dans la meme frame) sans jamais
        // etre perceptible comme un retard (retour de Jeff, Sprint Audio Polish, 2026-08-05 :
        // "aucun doublon si plusieurs boutons sont presses rapidement", "aucune latence perceptible").
        private const float UiSoundMinRetriggerSeconds = 0.05f;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [SerializeField]
        private UiSoundLibrary uiSoundLibrary;

        private readonly Dictionary<UiSoundId, float> lastUiSoundTimeById = new();

        [Header("Sound Effects")]
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip beeRecruitedSound;
        [SerializeField] private AudioClip errorSound;
        [SerializeField] private AudioClip resourceGainSound;
        [SerializeField] private AudioClip productionIncreaseSound;

        [Header("Music")]
        [SerializeField] private AudioClip backgroundMusic;

        [Header("Settings")]
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float musicVolume = 0.6f;
        [SerializeField] private float sfxVolume = 0.8f;
        private bool soundEnabled = true;
        private bool musicEnabled = true;

        public bool IsSoundEnabled => soundEnabled;
        public bool IsMusicEnabled => musicEnabled;

        #endregion

        #region Initialization

        /// <summary>
        /// Cree les AudioSources manquantes des Awake (pas Start, qui ne s'execute qu'a la frame
        /// suivante) - un son d'interface appele tres tot (avant meme la premiere frame reelle) doit
        /// toujours trouver sfxSource pret, jamais null.
        /// </summary>
        private void CreateAudioSourcesIfMissing()
        {
            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFXSource");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (uiSoundLibrary == null) uiSoundLibrary = Resources.Load<UiSoundLibrary>(UiSoundLibraryResourcePath);
            if (uiSoundLibrary == null)
            {
                Debug.LogWarning("AudioManager: aucun UiSoundLibrary trouve sous Resources/" + UiSoundLibraryResourcePath + " - les sons d'interface resteront silencieux.");
            }

            if (resourceGainSound == null) resourceGainSound = Resources.Load<AudioClip>(ResourceGainSoundResourcePath);
            if (resourceGainSound == null)
            {
                Debug.LogWarning("AudioManager: aucun clip de collecte trouve sous Resources/" + ResourceGainSoundResourcePath + " - le son de collecte restera silencieux.");
            }

            // Toujours router sfxSource vers le groupe SFX du MasterMixer (jamais directement vers le
            // master) - couvre a la fois les futurs sons d'interface et les effets sonores existants
            // (voix des Championnes, etc.) qui partagent cette meme source.
            sfxSource.outputAudioMixerGroup = uiSoundLibrary != null ? uiSoundLibrary.SfxMixerGroup : null;
        }

        private void Start()
        {
            // Appliquer les volumes
            UpdateVolumes();

            // Démarrer la musique de fond
            if (backgroundMusic != null)
            {
                PlayMusic(backgroundMusic);
            }
        }

        #endregion

        #region Sound Effects

        /// <summary>
        /// Joue un effet sonore par nom
        /// </summary>
        public void PlaySound(string soundName)
        {
            AudioClip clip = GetClipByName(soundName);
            if (clip != null)
            {
                PlaySound(clip);
            }
            else
            {
                Debug.LogWarning($"Sound '{soundName}' not found!");
            }
        }

        /// <summary>
        /// Joue un effet sonore
        /// </summary>
        public void PlaySound(AudioClip clip)
        {
            PlaySound(clip, sfxVolume);
        }

        /// <summary>
        /// Joue un effet sonore avec un volume de base dedie (ex: voix des Championnes, qui doivent
        /// rester intelligibles par-dessus la musique meme quand le volume SFX general est plus bas).
        /// Toujours multiplie par le volume general et respecte l'interrupteur son actif/inactif.
        /// </summary>
        public void PlaySound(AudioClip clip, float volumeScale)
        {
            if (clip != null && sfxSource != null && soundEnabled)
            {
                sfxSource.PlayOneShot(clip, volumeScale * masterVolume);
            }
        }

        /// <summary>
        /// Joue le son de clic de bouton
        /// </summary>
        public void PlayButtonClick()
        {
            PlaySound(buttonClickSound);
        }

        /// <summary>
        /// Joue un son d'interface catalogue (Sprint Audio Polish, 2026-08-05) - reserve strictement
        /// aux elements purement UI (boutons, menus, navigation, fenetres, confirmations/annulations,
        /// parametres, boutique, inventaire, recherche, alliance, championnes...). Ne fait rien (avec
        /// un avertissement en console) si aucun clip n'est encore associe a cet id dans
        /// UiSoundLibrary, et absorbe silencieusement les declenchements trop rapproches du MEME son
        /// (double-clic, plusieurs boutons dans la meme frame) pour ne jamais produire de doublon
        /// audible.
        /// </summary>
        public void PlayUiSound(UiSoundId id)
        {
            if (id == UiSoundId.None) return;

            float now = Time.unscaledTime;
            if (lastUiSoundTimeById.TryGetValue(id, out float lastTime) && now - lastTime < UiSoundMinRetriggerSeconds) return;

            if (uiSoundLibrary == null || !uiSoundLibrary.TryGetClip(id, out AudioClip clip))
            {
                Debug.LogWarning("AudioManager: son d'interface '" + id + "' pas encore configure - lecture ignoree.");
                return;
            }

            lastUiSoundTimeById[id] = now;
            PlaySound(clip);
        }

        /// <summary>
        /// API dediee pour le premier son d'interface (clic generique) - point d'entree unique pour
        /// tout element purement UI, jamais pour un batiment/ressource/creature/objet du monde.
        /// </summary>
        public void PlayUIClick()
        {
            PlayUiSound(UiSoundId.Click);
        }

        /// <summary>
        /// Son d'interface d'OUVERTURE d'un panneau (Championnes, Sac, Recherche, Alliance,
        /// Parametres, Bestiaire, Defi, Communication...). A appeler au moment exact ou un panneau
        /// passe d'ouvert a ferme. Jamais pour un batiment, la carte du monde, une ressource, une
        /// creature ou un objet interactif - uniquement les panneaux d'interface (Sprint Alpha-001,
        /// 2026-08-05).
        /// </summary>
        public void PlayMenuOpen()
        {
            PlayUiSound(UiSoundId.MenuOpen);
        }

        /// <summary>
        /// Son d'interface de FERMETURE d'un panneau (miroir de <see cref="PlayMenuOpen"/>).
        /// </summary>
        public void PlayMenuClose()
        {
            PlayUiSound(UiSoundId.MenuClose);
        }

        /// <summary>
        /// Joue le son de recrutement d'abeille
        /// </summary>
        public void PlayBeeRecruited()
        {
            PlaySound(beeRecruitedSound);
        }

        /// <summary>
        /// Joue le son d'erreur
        /// </summary>
        public void PlayError()
        {
            PlaySound(errorSound);
        }

        /// <summary>
        /// Joue le son de gain de ressource
        /// </summary>
        public void PlayResourceGain()
        {
            PlaySound(resourceGainSound);
        }

        /// <summary>
        /// Joue le son d'augmentation de production
        /// </summary>
        public void PlayProductionIncrease()
        {
            PlaySound(productionIncreaseSound);
        }

        #endregion

        #region Music

        /// <summary>
        /// Joue une musique de fond
        /// </summary>
        public void PlayMusic(AudioClip clip)
        {
            if (clip != null && musicSource != null)
            {
                musicSource.clip = clip;
                musicSource.volume = musicEnabled ? musicVolume * masterVolume : 0f;
                if (musicEnabled) musicSource.Play();
            }
        }

        /// <summary>
        /// Met la musique en pause
        /// </summary>
        public void PauseMusic()
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Pause();
            }
        }

        /// <summary>
        /// Reprend la musique
        /// </summary>
        public void ResumeMusic()
        {
            if (musicSource != null && !musicSource.isPlaying)
            {
                musicSource.UnPause();
            }
        }

        /// <summary>
        /// Arrête la musique
        /// </summary>
        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        #endregion

        #region Volume Controls

        /// <summary>
        /// Définit le volume principal
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        /// <summary>
        /// Définit le volume de la musique
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        /// <summary>
        /// Définit le volume des effets sonores
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Met à jour tous les volumes
        /// </summary>
        private void UpdateVolumes()
        {
            if (musicSource != null)
            {
                musicSource.volume = musicEnabled ? musicVolume * masterVolume : 0f;
            }
        }

        /// <summary>
        /// Active/desactive les effets sonores (persiste cote appelant).
        /// </summary>
        public void SetSoundEnabled(bool enabled)
        {
            soundEnabled = enabled;
        }

        /// <summary>
        /// Active/desactive la musique. Coupe ou reprend la source en cours sans la reinitialiser.
        /// </summary>
        public void SetMusicEnabled(bool enabled)
        {
            musicEnabled = enabled;
            UpdateVolumes();
            if (musicSource == null) return;
            if (!enabled) musicSource.Pause();
            else if (musicSource.clip != null && !musicSource.isPlaying) musicSource.UnPause();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Obtient un AudioClip par son nom
        /// </summary>
        private AudioClip GetClipByName(string name)
        {
            return name.ToLower() switch
            {
                "button_click" => buttonClickSound,
                "bee_recruited" => beeRecruitedSound,
                "error" => errorSound,
                "resource_gain" => resourceGainSound,
                "production_increase" => productionIncreaseSound,
                _ => null
            };
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        [ContextMenu("Test Button Click")]
        private void TestButtonClick()
        {
            PlayButtonClick();
        }

        [ContextMenu("Test Bee Recruited")]
        private void TestBeeRecruited()
        {
            PlayBeeRecruited();
        }

        [ContextMenu("Test Error")]
        private void TestError()
        {
            PlayError();
        }
#endif

        #endregion
    }
}

/*
═══════════════════════════════════════════════════════════════
GUIDE D'UTILISATION - AUDIO MANAGER
═══════════════════════════════════════════════════════════════

📋 SETUP:
1. Créez un GameObject vide: "AudioManager"
2. Attachez ce script
3. Assignez les AudioClips dans l'Inspector

🔊 DANS VOS SCRIPTS:

// Jouer un son
AudioManager.Instance.PlayButtonClick();
AudioManager.Instance.PlayBeeRecruited();
AudioManager.Instance.PlayError();

// Ou par nom
AudioManager.Instance.PlaySound("bee_recruited");

// Contrôler la musique
AudioManager.Instance.PlayMusic(myMusicClip);
AudioManager.Instance.PauseMusic();
AudioManager.Instance.ResumeMusic();

// Volumes
AudioManager.Instance.SetMasterVolume(0.7f);
AudioManager.Instance.SetMusicVolume(0.5f);
AudioManager.Instance.SetSFXVolume(0.8f);

═══════════════════════════════════════════════════════════════
INTÉGRATION AVEC LES SCRIPTS UX:
═══════════════════════════════════════════════════════════════

📝 RecruitBeeButton_Enhanced.cs:

Dans OnButtonClick(), ajoutez:

// Au début de la fonction
AudioManager.Instance?.PlayButtonClick();

// En cas d'erreur
AudioManager.Instance?.PlayError();

// En cas de succès
AudioManager.Instance?.PlayBeeRecruited();

💚 ResourceUIEffect.cs:

Dans OnBeeRecruited(), ajoutez:

AudioManager.Instance?.PlayProductionIncrease();

═══════════════════════════════════════════════════════════════
SONS SUGGÉRÉS:
═══════════════════════════════════════════════════════════════

🎵 Où trouver des sons gratuits:
- Freesound.org
- OpenGameArt.org
- Itch.io (search "sound effects")
- Mixkit.co

🐝 Sons recommandés pour Bee Kingdom:
- button_click.wav → Click UI léger
- bee_recruited.wav → Bzzzz d'abeille joyeux
- error.wav → Beep court négatif
- resource_gain.wav → Coin pickup
- production_increase.wav → Level up chime
- background_music.ogg → Musique relaxante

📂 Format:
- .wav pour les SFX (pas de compression)
- .ogg pour la musique (compression légère)

═══════════════════════════════════════════════════════════════
🎮 BEE KINGDOM - Session 4 Ready!
═══════════════════════════════════════════════════════════════
*/