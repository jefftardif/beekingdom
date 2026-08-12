using System;

namespace BeeKingdom.Networking
{
    // Miroir client du premier evenement mondial dynamique (demande de Jeff, 2026-08-01) : la
    // meteo/menace active change plusieurs fois par jour (contrairement a la Cible du jour, fixe
    // pour toute la journee) - voir WorldEventCatalog cote serveur, seule autorite sur le calcul.
    // Ce DTO n'est qu'un affichage ; la validation du bonus reste toujours cote serveur.
    public sealed class RemoteActiveWorldEvent
    {
        public string Key { get; set; }
        public string Kind { get; set; }
        public string TargetKey { get; set; }
        public long BonusBp { get; set; }
        public DateTimeOffset EndsAtUtc { get; set; }
    }
}
