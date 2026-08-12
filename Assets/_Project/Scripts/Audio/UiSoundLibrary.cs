using System;
using UnityEngine;
using UnityEngine.Audio;

namespace BeeKingdom.Audio
{
    /// <summary>
    /// Table de correspondance UiSoundId -> AudioClip, plus la reference au groupe "SFX" du
    /// MasterMixer. Vit sous un dossier Resources (meme convention que <see cref="MusicLibrary"/>)
    /// afin que <see cref="AudioManager"/> puisse la charger sans reference de scene.
    ///
    /// Pour ajouter un futur son d'interface : ajouter une entree dans le tableau via l'Inspecteur
    /// une fois le clip disponible. Aucun code a modifier.
    /// </summary>
    [CreateAssetMenu(fileName = "UiSoundLibrary", menuName = "BeeKingdom/Audio/UI Sound Library")]
    public sealed class UiSoundLibrary : ScriptableObject
    {
        [Serializable]
        private struct SoundEntry
        {
            public UiSoundId id;
            public AudioClip clip;
        }

        [SerializeField]
        private AudioMixerGroup sfxMixerGroup;

        [SerializeField]
        private SoundEntry[] entries = Array.Empty<SoundEntry>();

        public AudioMixerGroup SfxMixerGroup => sfxMixerGroup;

        public bool TryGetClip(UiSoundId id, out AudioClip clip)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].id != id) continue;
                clip = entries[i].clip;
                return clip != null;
            }

            clip = null;
            return false;
        }
    }
}
