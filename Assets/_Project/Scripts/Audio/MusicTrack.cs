namespace BeeKingdom.Audio
{
    /// <summary>
    /// Catalogue des pistes musicales connues de <see cref="MusicManager"/>. Ajouter une future
    /// musique (Combat, Boss, Victoire...) se fait en ajoutant une entree ici puis en lui associant
    /// un clip dans l'asset <see cref="MusicLibrary"/> - aucune autre modification requise dans
    /// MusicManager lui-meme.
    ///
    /// Sprint Audio Foundation (2026-08-04) : seules Hive et World ont un clip assigne pour
    /// l'instant. Les entrees suivantes existent deja pour que l'architecture soit prete, mais ne
    /// sont volontairement associees a aucun fichier audio - MusicManager.Play() les reconnait et
    /// se contente d'un avertissement silencieux tant qu'aucun clip ne leur est assigne.
    /// </summary>
    public enum MusicTrack
    {
        None = 0,
        Hive,
        World,

        // Prets pour un futur sprint (architecture uniquement, aucun clip assigne aujourd'hui) :
        Combat,
        Boss,
        Victory,
        Defeat,
        MainMenu,
        LoginScreen,
        WorldEvent,
        Seasonal
    }
}
