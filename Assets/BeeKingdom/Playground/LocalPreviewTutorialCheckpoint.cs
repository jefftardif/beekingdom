using System;
using UnityEngine;

namespace BeeKingdom.Playground
{
    [Serializable]
    public sealed class LocalPreviewTutorialCheckpoint
    {
        public int version = 1;
        public string checkpointId = string.Empty;
        public int chapter;
        public string interruptedObjective = string.Empty;
        public int interruptedObjectiveIndex;
        public int completedChapters;
        public float chapterStartHoney;
        public float chapterStartWax;
        public float chapterStartPollen;
        public float chapterStartCapacityUsed;
        public float chapterStartCapacityMax;
        public int chapterStartWorkers;
        public float chapterStartBroodNutrition;
        public float chapterStartBroodStability;
        public int chapterStartSecurity;
    }

    public interface ILocalPreviewTutorialCheckpointStore
    {
        string Read();
        void Write(string json);
        void Delete();
    }

    public sealed class PlayerPrefsLocalPreviewTutorialCheckpointStore : ILocalPreviewTutorialCheckpointStore
    {
        private const string Key = "BeeKingdom_LivingHive_TutorialCheckpoint_v1";
        public string Read() => PlayerPrefs.GetString(Key, string.Empty);
        public void Write(string json) { PlayerPrefs.SetString(Key, json ?? string.Empty); PlayerPrefs.Save(); }
        public void Delete() { PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); }
    }
}
