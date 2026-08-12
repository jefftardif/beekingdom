using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace BeeKingdom.Audio
{
    /// <summary>
    /// Point d'entree centralise et unique pour toute musique du jeu (Sprint Audio Foundation,
    /// 2026-08-04). Plus aucune scene ne doit demarrer/arreter une musique directement - tout passe
    /// par MusicManager.Instance.Play(MusicTrack.X). Le reste (AudioSource, routage vers le groupe
    /// Music du MasterMixer, fondu croise, boucle, volume) reste entierement interne a cette classe.
    ///
    /// Deux AudioSource persistantes sont creees une seule fois et reutilisees pour toute la duree
    /// de vie du jeu (jamais recreees a chaque transition) - aucun doublon, aucune fuite. Chaque
    /// appel a Play() sur une piste differente de la cible actuelle demarre un fondu croise entre la
    /// source active et l'autre ; un appel pendant un fondu deja en cours reprend proprement a partir
    /// du volume reel de chaque source a cet instant (jamais de saut ni de redemarrage audible), ce
    /// qui couvre le cas d'un joueur qui fait plusieurs allers-retours rapides Ruche/Carte.
    /// </summary>
    public sealed class MusicManager : MonoBehaviour
    {
        private const string LibraryResourcePath = "MusicLibrary";
        private const float CrossFadeSeconds = 1.2f;

        // Volume relatif de la musique pendant qu'une voix (Championne...) doit rester intelligible
        // par-dessus elle - retour direct de Jeff, 2026-08-05 : les voix etaient couvertes par la
        // musique. S'applique en plus du fondu croise normal, jamais a la place.
        private const float VoiceDuckMultiplier = 0.32f;
        private const float DuckGlideSpeedPerSecond = 3f;

        public static MusicManager Instance { get; private set; }

        public MusicTrack CurrentTrack => targetTrack;

        public bool HasTrack(MusicTrack track)
        {
            EnsureLibraryLoaded();
            return library != null && library.TryGetClip(track, out AudioClip clip) && clip != null;
        }

        [SerializeField]
        private MusicLibrary library;

        private AudioSource sourceA;
        private AudioSource sourceB;
        private MusicTrack sourceATrack = MusicTrack.None;
        private MusicTrack sourceBTrack = MusicTrack.None;
        private MusicTrack targetTrack = MusicTrack.None;
        private Coroutine crossFadeRoutine;
        private Coroutine duckRestoreRoutine;
        private float duckMultiplier = 1f;
        private float duckTargetMultiplier = 1f;
        private bool libraryWarningLogged;

        /// <summary>
        /// Garantit qu'un MusicManager existe, meme si aucune scene n'en contient un explicitement.
        /// Egalement appele automatiquement avant le chargement de la toute premiere scene (voir
        /// Bootstrap ci-dessous), donc MusicManager.Instance ne devrait jamais etre null en pratique.
        /// </summary>
        public static MusicManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("MusicManager");
            return go.AddComponent<MusicManager>();
        }

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

            EnsureLibraryLoaded();

            AudioMixerGroup musicGroup = library != null ? library.MusicMixerGroup : null;
            sourceA = CreateLoopingSource("MusicSourceA", musicGroup);
            sourceB = CreateLoopingSource("MusicSourceB", musicGroup);

            EnsureAudioListener();
        }

        /// <summary>
        /// Aucune scene du jeu (LivingHive, Carte du monde...) ne place actuellement d'AudioListener
        /// sur sa camera - leurs cameras sont creees/configurees pour le rendu uniquement (voir
        /// SandboxPlaygroundBootstrap.EnsureRenderableCamera, partagee avec des outils de capture
        /// d'ecran internes qui ne doivent justement jamais produire de son). Sans AudioListener actif
        /// nulle part, aucun AudioSource du jeu - musique ou effets - n'est jamais audible, quelle que
        /// soit la justesse de son cablage. MusicManager etant le seul objet garanti present avant
        /// meme le chargement de la premiere scene et survivant a chaque changement de scene, c'est le
        /// point centralise le plus fiable pour porter cet unique AudioListener. Garde explicite pour
        /// ne jamais en ajouter un second si une scene venait un jour a en definir un elle-meme.
        /// </summary>
        private void EnsureAudioListener()
        {
            if (FindObjectsOfType<AudioListener>().Length > 0) return;
            gameObject.AddComponent<AudioListener>();
        }

        private AudioSource CreateLoopingSource(string name, AudioMixerGroup group)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            AudioSource source = go.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
            source.outputAudioMixerGroup = group;
            return source;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Mathf.Approximately(duckMultiplier, duckTargetMultiplier)) return;
            duckMultiplier = Mathf.MoveTowards(duckMultiplier, duckTargetMultiplier, DuckGlideSpeedPerSecond * Time.unscaledDeltaTime);

            // Pendant un fondu croise, CrossFadeRoutine lit et applique deja duckMultiplier a chaque
            // reprise (apres ce Update, dans la meme frame) - ne pas y toucher deux fois ici. Hors
            // fondu, la source active reste sinon figee a son dernier volume fixe et ne suivrait
            // jamais un duck declenche apres coup.
            if (crossFadeRoutine != null) return;

            AudioSource activeSource = ActiveSourceForCurrentTarget();
            if (activeSource != null) activeSource.volume = duckMultiplier;
        }

        private AudioSource ActiveSourceForCurrentTarget()
        {
            if (targetTrack == MusicTrack.None) return null;
            if (sourceATrack == targetTrack) return sourceA;
            if (sourceBTrack == targetTrack) return sourceB;
            return null;
        }

        /// <summary>
        /// Baisse temporairement le volume de la musique pendant qu'une voix (Championne...) doit
        /// rester intelligible, puis le restaure progressivement une fois la duree ecoulee. Plusieurs
        /// appels qui se chevauchent prolongent simplement le duck jusqu'a la fin du plus recent -
        /// jamais de restauration prematuree pendant qu'une autre voix parle encore.
        /// </summary>
        public void DuckForVoice(float durationSeconds)
        {
            duckTargetMultiplier = VoiceDuckMultiplier;
            if (duckRestoreRoutine != null) StopCoroutine(duckRestoreRoutine);
            duckRestoreRoutine = StartCoroutine(RestoreDuckAfterDelay(Mathf.Max(0.01f, durationSeconds)));
        }

        private IEnumerator RestoreDuckAfterDelay(float durationSeconds)
        {
            yield return new WaitForSecondsRealtime(durationSeconds);
            duckTargetMultiplier = 1f;
            duckRestoreRoutine = null;
        }

        /// <summary>
        /// Joue la piste demandee, avec un fondu croise fluide si une autre piste est deja en train
        /// de jouer. Ne fait rien (avec un avertissement en console) si aucun clip n'est encore
        /// associe a cette piste dans MusicLibrary - c'est le cas normal pour toutes les pistes
        /// prevues pour de futurs sprints (Combat, Boss, Victoire...).
        /// </summary>
        public void Play(MusicTrack track)
        {
            if (track == MusicTrack.None || track == targetTrack) return;

            EnsureLibraryLoaded();

            if (library == null || !library.TryGetClip(track, out AudioClip clip))
            {
                Debug.LogWarning("MusicManager: piste '" + track + "' pas encore configuree (architecture prete, aucun clip assigne pour l'instant) - lecture ignoree.");
                return;
            }

            MusicTrack previousTarget = targetTrack;
            targetTrack = track;

            AudioSource fadeIn;
            AudioSource fadeOut;

            if (sourceATrack == track)
            {
                fadeIn = sourceA;
                fadeOut = sourceB;
            }
            else if (sourceBTrack == track)
            {
                fadeIn = sourceB;
                fadeOut = sourceA;
            }
            else
            {
                // Piste absente des deux sources : elle prend la place de celle qui NE porte PAS la
                // piste qu'on est en train de quitter (jamais celle qui joue reellement en ce
                // moment, sinon on ecraserait son clip en plein milieu de la lecture - c'est
                // exactement le clic/coupure que ce systeme doit eviter). Repart toujours du debut
                // du nouveau clip.
                bool sourceAIsLeaving = sourceATrack == previousTarget;
                fadeIn = sourceAIsLeaving ? sourceB : sourceA;
                fadeOut = sourceAIsLeaving ? sourceA : sourceB;

                fadeIn.clip = clip;
                fadeIn.time = 0f;
                fadeIn.Play();
                if (fadeIn == sourceA) sourceATrack = track; else sourceBTrack = track;
            }

            if (crossFadeRoutine != null) StopCoroutine(crossFadeRoutine);
            crossFadeRoutine = StartCoroutine(CrossFadeRoutine(fadeIn, fadeOut));
        }

        private void EnsureLibraryLoaded()
        {
            if (library != null) return;
            library = Resources.Load<MusicLibrary>(LibraryResourcePath);
            if (library == null && !libraryWarningLogged)
            {
                libraryWarningLogged = true;
                Debug.LogWarning("MusicManager: aucun MusicLibrary trouve sous Resources/" + LibraryResourcePath + " - la musique restera silencieuse.");
            }
        }

        private IEnumerator CrossFadeRoutine(AudioSource fadeIn, AudioSource fadeOut)
        {
            float duration = Mathf.Max(0.01f, CrossFadeSeconds);
            float startIn = fadeIn.volume;
            float startOut = fadeOut.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // duckMultiplier est avance par Update() (execute avant la reprise de cette coroutine
                // dans l'ordre de frame de Unity) - simple lecture ici, jamais recalcule deux fois.
                fadeIn.volume = Mathf.Lerp(startIn, 1f, t) * duckMultiplier;
                fadeOut.volume = Mathf.Lerp(startOut, 0f, t) * duckMultiplier;
                yield return null;
            }

            fadeIn.volume = duckMultiplier;
            fadeOut.volume = 0f;
            fadeOut.Stop();
            fadeOut.clip = null;
            if (fadeOut == sourceA) sourceATrack = MusicTrack.None; else sourceBTrack = MusicTrack.None;

            crossFadeRoutine = null;
        }
    }
}
