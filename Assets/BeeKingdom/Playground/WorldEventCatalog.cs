using System;
using System.Collections.Generic;

namespace BeeKingdom.Playground
{
    public enum WorldEventKind { Weather, ThreatSurge }

    public readonly struct ActiveWorldEvent
    {
        public readonly string Key;
        public readonly WorldEventKind Kind;

        public ActiveWorldEvent(string key, WorldEventKind kind)
        {
            Key = key;
            Kind = kind;
        }
    }

    // Miroir client de Server/src/BeeKingdom.HiveOperations/WorldEventCatalog.cs - meme convention
    // deja etablie pour ChampionBeeCatalog (les deux doivent rester synchronises si un evenement est
    // ajoute cote serveur). Fonction PURE du temps, sans etat ni RNG : le client peut donc calculer
    // la meme meteo/menace active que le serveur sans aucun appel reseau. Utilise uniquement pour une
    // ambiance visuelle cote carte du monde (demande de Jeff, 2026-08-02, "la meteo influence
    // legerement l'ambiance") - jamais pour un calcul de recompense, qui reste entierement
    // serveur-autoritaire.
    public static class WorldEventCatalog
    {
        private const int WindowHours = 4;

        private static readonly (string Key, WorldEventKind Kind)[] Events =
        {
            ("blossom", WorldEventKind.Weather),
            ("rain", WorldEventKind.Weather),
            ("drought", WorldEventKind.Weather),
            ("ant_invasion", WorldEventKind.ThreatSurge),
            ("spider_surge", WorldEventKind.ThreatSurge),
            ("hornet_swarm", WorldEventKind.ThreatSurge)
        };

        public static ActiveWorldEvent Active(DateTimeOffset utcNow)
        {
            (string key, WorldEventKind kind) = Events[(int)(Cycle(utcNow) % Events.Length)];
            return new ActiveWorldEvent(key, kind);
        }

        private static long Cycle(DateTimeOffset utcNow)
        {
            long days = (long)(utcNow.UtcDateTime.Date - DateTime.UnixEpoch.Date).TotalDays;
            int hourBucket = utcNow.UtcDateTime.Hour / WindowHours;
            return days + hourBucket;
        }
    }
}
