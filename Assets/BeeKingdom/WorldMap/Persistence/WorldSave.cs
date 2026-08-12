using System;
using UnityEngine;

namespace BeeKingdom.WorldMap
{
    [Serializable]
    public sealed class WorldMapSaveData
    {
        public double CameraPositionX;
        public double CameraPositionY;
        public float CameraZoom;
        public bool GridVisible;
        public bool DebugOverlayVisible;

        public WorldVector2 CameraPosition => new WorldVector2(CameraPositionX, CameraPositionY);

        public void SetCamera(WorldVector2 position, float zoom)
        {
            CameraPositionX = position.X;
            CameraPositionY = position.Y;
            CameraZoom = zoom;
        }
    }

    // Stockage brut de la sauvegarde (cle -> json). PlayerPrefs en production,
    // memoire dans les tests.
    public interface IWorldMapSaveStore
    {
        string Read(string key);
        void Write(string key, string json);
        void Delete(string key);
    }

    public sealed class PlayerPrefsWorldMapSaveStore : IWorldMapSaveStore
    {
        public string Read(string key)
        {
            return PlayerPrefs.GetString(key, null);
        }

        public void Write(string key, string json)
        {
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }

    // Persistance des parametres de la carte : position camera, zoom et preferences
    // utilisateur. Cle versionnee pour migrer proprement plus tard.
    public sealed class WorldSave
    {
        public const string DefaultKey = "bee-kingdom.worldmap.v1";

        private readonly IWorldMapSaveStore store;
        private readonly string key;

        public WorldSave(IWorldMapSaveStore store, string key = null)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.key = string.IsNullOrWhiteSpace(key) ? DefaultKey : key;
        }

        public void Save(WorldMapSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            store.Write(key, JsonUtility.ToJson(data));
        }

        public bool TryLoad(out WorldMapSaveData data)
        {
            data = null;
            string json = store.Read(key);
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                WorldMapSaveData parsed = JsonUtility.FromJson<WorldMapSaveData>(json);
                if (parsed == null || !IsFinite(parsed.CameraPositionX) || !IsFinite(parsed.CameraPositionY) || !IsFinite(parsed.CameraZoom))
                {
                    return false;
                }

                data = parsed;
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public void Reset()
        {
            store.Delete(key);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
