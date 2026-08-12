using System;
using UnityEngine;
using UnityEngine.Audio;

namespace BeeKingdom.Audio
{
    /// <summary>
    /// Table de correspondance MusicTrack -> AudioClip, plus la reference au groupe "Music" du
    /// MasterMixer. Vit sous un dossier Resources (voir MusicManager.LibraryResourcePath) afin que
    /// MusicManager puisse la charger sans reference de scene, meme si les clips et le mixer eux-
    /// memes restent a leur emplacement d'origine (Assets/Audio/...) - aucun fichier de Jeff n'a
    /// besoin d'etre deplace pour que ce chargement fonctionne.
    ///
    /// Pour ajouter une future piste (Combat, Boss...) : ajouter une entree dans le tableau via
    /// l'Inspecteur une fois le clip disponible. Aucun code a modifier.
    /// </summary>
    [CreateAssetMenu(fileName = "MusicLibrary", menuName = "BeeKingdom/Audio/Music Library")]
    public sealed class MusicLibrary : ScriptableObject
    {
        [Serializable]
        private struct TrackEntry
        {
            public MusicTrack track;
            public AudioClip clip;
        }

        [SerializeField]
        private AudioMixerGroup musicMixerGroup;

        [SerializeField]
        private TrackEntry[] entries = Array.Empty<TrackEntry>();

        public AudioMixerGroup MusicMixerGroup => musicMixerGroup;

        public bool TryGetClip(MusicTrack track, out AudioClip clip)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].track != track) continue;
                clip = entries[i].clip;
                return clip != null;
            }

            clip = null;
            return false;
        }
    }
}
