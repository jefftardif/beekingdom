using System;
using UnityEngine;

namespace BeeKingdom.Playground
{
    [Serializable]
    public sealed class LocalPreviewDailyRound
    {
        public int version = LocalPreviewDailyRoundCodec.CurrentVersion;
        public string utcDay = string.Empty;
        public int tasksMask;
        public bool rewardClaimed;
        public string claimOperationId = string.Empty;
        public float rewardHoney = 120f;
        public float rewardPollen = 60f;
    }

    public interface ILocalPreviewDailyRoundStore
    {
        string Read();
        void Write(string json);
        void Delete();
    }

    public sealed class PlayerPrefsLocalPreviewDailyRoundStore : ILocalPreviewDailyRoundStore
    {
        private const string Key = "BeeKingdom_LivingHive_LocalPreviewDailyRound_v1";

        public string Read() => PlayerPrefs.GetString(Key, string.Empty);

        public void Write(string json)
        {
            PlayerPrefs.SetString(Key, json ?? string.Empty);
            PlayerPrefs.Save();
        }

        public void Delete()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }

    public static class LocalPreviewDailyRoundCodec
    {
        public const int CurrentVersion = 1;

        public static LocalPreviewDailyRound Read(ILocalPreviewDailyRoundStore store)
        {
            if (store == null) return new LocalPreviewDailyRound();
            string json = store.Read();
            if (string.IsNullOrWhiteSpace(json)) return new LocalPreviewDailyRound();
            try
            {
                LocalPreviewDailyRound round = JsonUtility.FromJson<LocalPreviewDailyRound>(json);
                if (round == null || round.version != CurrentVersion) return new LocalPreviewDailyRound();
                round.utcDay ??= string.Empty;
                round.claimOperationId ??= string.Empty;
                round.rewardHoney = Mathf.Max(0f, round.rewardHoney);
                round.rewardPollen = Mathf.Max(0f, round.rewardPollen);
                round.tasksMask &= 7;
                return round;
            }
            catch
            {
                return new LocalPreviewDailyRound();
            }
        }

        public static void Write(ILocalPreviewDailyRoundStore store, LocalPreviewDailyRound round)
        {
            if (store == null || round == null) return;
            store.Write(JsonUtility.ToJson(round));
        }
    }
}
