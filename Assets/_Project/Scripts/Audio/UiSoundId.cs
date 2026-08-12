namespace BeeKingdom.Audio
{
    /// <summary>
    /// Catalogue des sons d'interface connus de <see cref="AudioManager"/>. Ajouter un futur son
    /// d'interface (survol, erreur, succes...) se fait en ajoutant une entree ici puis en lui
    /// associant un clip dans <see cref="UiSoundLibrary"/> - aucune autre modification requise dans
    /// AudioManager lui-meme.
    /// </summary>
    public enum UiSoundId
    {
        None = 0,
        Click,
        MenuOpen,
        MenuClose
    }
}
