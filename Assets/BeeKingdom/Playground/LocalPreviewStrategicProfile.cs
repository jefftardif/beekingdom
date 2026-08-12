using System;
using UnityEngine;

namespace BeeKingdom.Playground
{
    [Serializable]
    public sealed class LocalPreviewStrategicProfile
    {
        public int version = 12;
        public string profileId = string.Empty;
        public int revision;
        public string openingCharter = "none";
        public string openingCommissioning = "none";
        public string openingBroodSupply = "none";
        public string openingReward = "none";
        public string broodDevelopment = "none";
        public string broodDoctrine = "none";
        public string broodWorkerHandoff = "none";
        public string broodWaxConsolidation = "none";
        public string workerAssignment = "none";
        public string workerWorkshopHandoff = "none";
        public string workerWorkshopCommission = "none";
        public string workshopSpecialization = "none";
        public string workshopDoctrine = "none";
        public string workshopCertification = "none";
        public string workshopDefenseHandoff = "none";
        public string defenseExpeditionMandate = "none";
        public string defenseWorldBriefing = "none";
        public float operationalHoneyProductionBonus;
        public float operationalWaxProductionBonus;
        public float operationalWaxCapacityBonus;
        public float operationalBroodCareBonus;
        public float operationalBroodStabilityBonus;
    }

    public readonly struct LocalPreviewStrategicEffects
    {
        public readonly float HoneyProductionBonus;
        public readonly float WaxProductionBonus;
        public readonly float WaxCapacityBonus;
        public readonly float BroodCareBonus;
        public readonly float BroodStabilityBonus;
        public readonly float WorkerRationHoneyDiscount;
        public readonly float CapacityFlatBonus;
        public readonly float WorkshopHoneyDiscount;
        public readonly float WorkshopWaxDiscount;
        public readonly float ForagingPollenBonus;
        public readonly float ExpeditionSecurityBonus;
        public readonly float DefenseBarrierWaxDiscount;
        public readonly float DefenseStartingSecurityBonus;
        public readonly float WorkerEmergenceWaxDiscount;
        public readonly float WorkshopCalibrationWaxBonus;
        public readonly float WorkshopApplicationWaxDiscount;
        public readonly float WorldNavigationHintLevel;
        public readonly float WorldBriefingSecurityBonus;

        public LocalPreviewStrategicEffects(float honeyProductionBonus, float waxProductionBonus, float waxCapacityBonus, float broodCareBonus, float broodStabilityBonus, float workerRationHoneyDiscount, float capacityFlatBonus, float workshopHoneyDiscount, float workshopWaxDiscount, float foragingPollenBonus, float expeditionSecurityBonus, float defenseBarrierWaxDiscount, float defenseStartingSecurityBonus, float workerEmergenceWaxDiscount, float workshopCalibrationWaxBonus, float workshopApplicationWaxDiscount, float worldNavigationHintLevel, float worldBriefingSecurityBonus)
        {
            HoneyProductionBonus = honeyProductionBonus;
            WaxProductionBonus = waxProductionBonus;
            WaxCapacityBonus = waxCapacityBonus;
            BroodCareBonus = broodCareBonus;
            BroodStabilityBonus = broodStabilityBonus;
            WorkerRationHoneyDiscount = workerRationHoneyDiscount;
            CapacityFlatBonus = capacityFlatBonus;
            WorkshopHoneyDiscount = workshopHoneyDiscount;
            WorkshopWaxDiscount = workshopWaxDiscount;
            ForagingPollenBonus = foragingPollenBonus;
            ExpeditionSecurityBonus = expeditionSecurityBonus;
            DefenseBarrierWaxDiscount = defenseBarrierWaxDiscount;
            DefenseStartingSecurityBonus = defenseStartingSecurityBonus;
            WorkerEmergenceWaxDiscount = workerEmergenceWaxDiscount;
            WorkshopCalibrationWaxBonus = workshopCalibrationWaxBonus;
            WorkshopApplicationWaxDiscount = workshopApplicationWaxDiscount;
            WorldNavigationHintLevel = worldNavigationHintLevel;
            WorldBriefingSecurityBonus = worldBriefingSecurityBonus;
        }
    }

    public static class LocalPreviewStrategicProfileRules
    {
        public static LocalPreviewStrategicEffects Derive(LocalPreviewStrategicProfile profile)
        {
            if (profile == null) return new LocalPreviewStrategicEffects(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
            float honey = profile.workerAssignment == "honey" ? 0.03f : 0f;
            float care = profile.workerAssignment == "nursery" ? 2f : 0f;
            float stability = profile.broodDevelopment == "resilience" ? 6f : 0f;
            float discount = profile.broodDevelopment == "growth" ? 80f : 0f;
            if (profile.broodWorkerHandoff == "emergence_ration") discount += 60f;
            float capacityFlat = profile.openingCharter == "secure_reserve" ? 5000f : 0f;
            float workshopHoneyDiscount = profile.workerWorkshopHandoff == "honey_logistics" ? 120f : 0f;
            float workshopWaxDiscount = profile.workerWorkshopHandoff == "wax_convoy" ? 80f : 0f;
            float foragingPollenBonus = profile.defenseExpeditionMandate == "scout_corridor" ? 6f : profile.defenseExpeditionMandate == "guardian_escort" ? 4f : 0f;
            float expeditionSecurityBonus = profile.defenseExpeditionMandate == "guardian_escort" ? 2f : 0f;
            if (profile.openingCharter == "brood_bridge") { stability += 4f; care += 1f; }
            if (profile.openingCommissioning == "reserve_seal") honey += 0.01f;
            if (profile.openingCommissioning == "brood_seal") stability += 2f;
            if (profile.openingBroodSupply == "royal_jelly_cache") care += 2f;
            if (profile.openingBroodSupply == "thermal_escort") stability += 3f;
            if (profile.broodDoctrine == "resilience") { stability += 8f; care += 1f; }
            float waxProduction = profile.workshopSpecialization == "production" ? 0.06f : 0f;
            float waxCapacity = profile.workshopSpecialization == "capacity" ? 0.20f : 0f;
            if (profile.workshopDoctrine == "precision") waxProduction += 0.01f;
            if (profile.workshopDoctrine == "cadence") waxCapacity += 0.05f;
            if (profile.workshopCertification == "thermal") waxProduction += 0.01f;
            if (profile.workshopCertification == "load") waxCapacity += 0.05f;
            return new LocalPreviewStrategicEffects(
                honey + profile.operationalHoneyProductionBonus,
                waxProduction + profile.operationalWaxProductionBonus,
                waxCapacity + profile.operationalWaxCapacityBonus,
                care + profile.operationalBroodCareBonus,
                stability + profile.operationalBroodStabilityBonus,
                discount,
                capacityFlat,
                workshopHoneyDiscount,
                workshopWaxDiscount,
                foragingPollenBonus,
                expeditionSecurityBonus,
                profile.workshopDefenseHandoff == "wax_shields" ? 60f : 0f,
                profile.workshopDefenseHandoff == "ventilated_lattice" ? 4f : 0f,
                profile.broodWorkerHandoff == "reinforced_operculum" ? 40f : 0f,
                profile.workerWorkshopCommission == "calibration_template" ? 40f : 0f,
                profile.workerWorkshopCommission == "application_toolkit" ? 40f : 0f,
                profile.defenseWorldBriefing == "sun_beacon" ? 1f : 0f,
                profile.defenseWorldBriefing == "guarded_return" ? 3f : 0f);
        }
    }

    public interface ILocalPreviewStrategicProfileStore
    {
        string Read();
        void Write(string json);
        void Delete();
    }

    public sealed class PlayerPrefsLocalPreviewStrategicProfileStore : ILocalPreviewStrategicProfileStore
    {
        private const string Key = "BeeKingdom_LivingHive_StrategicProfile_v12";
        private const string LegacyKeyV11 = "BeeKingdom_LivingHive_StrategicProfile_v11";
        private const string LegacyKeyV10 = "BeeKingdom_LivingHive_StrategicProfile_v10";
        private const string LegacyKeyV9 = "BeeKingdom_LivingHive_StrategicProfile_v9";
        private const string LegacyKeyV8 = "BeeKingdom_LivingHive_StrategicProfile_v8";
        private const string LegacyKeyV7 = "BeeKingdom_LivingHive_StrategicProfile_v7";
        private const string LegacyKeyV6 = "BeeKingdom_LivingHive_StrategicProfile_v6";
        private const string LegacyKeyV5 = "BeeKingdom_LivingHive_StrategicProfile_v5";
        private const string LegacyKeyV4 = "BeeKingdom_LivingHive_StrategicProfile_v4";
        private const string LegacyKeyV3 = "BeeKingdom_LivingHive_StrategicProfile_v3";
        private const string LegacyKeyV2 = "BeeKingdom_LivingHive_StrategicProfile_v2";
        private const string LegacyKeyV1 = "BeeKingdom_LivingHive_StrategicProfile_v1";
        public string Read() => PlayerPrefs.HasKey(Key) ? PlayerPrefs.GetString(Key, string.Empty) : PlayerPrefs.HasKey(LegacyKeyV11) ? PlayerPrefs.GetString(LegacyKeyV11, string.Empty) : PlayerPrefs.HasKey(LegacyKeyV10) ? PlayerPrefs.GetString(LegacyKeyV10, string.Empty) : PlayerPrefs.HasKey(LegacyKeyV9) ? PlayerPrefs.GetString(LegacyKeyV9, string.Empty) : PlayerPrefs.HasKey(LegacyKeyV8) ? PlayerPrefs.GetString(LegacyKeyV8, string.Empty) : PlayerPrefs.HasKey(LegacyKeyV7) ? PlayerPrefs.GetString(LegacyKeyV7, string.Empty) : PlayerPrefs.HasKey(LegacyKeyV6) ? PlayerPrefs.GetString(LegacyKeyV6, string.Empty) : PlayerPrefs.HasKey(LegacyKeyV5) ? PlayerPrefs.GetString(LegacyKeyV5, string.Empty) : PlayerPrefs.HasKey(LegacyKeyV4) ? PlayerPrefs.GetString(LegacyKeyV4, string.Empty) : PlayerPrefs.HasKey(LegacyKeyV3) ? PlayerPrefs.GetString(LegacyKeyV3, string.Empty) : PlayerPrefs.HasKey(LegacyKeyV2) ? PlayerPrefs.GetString(LegacyKeyV2, string.Empty) : PlayerPrefs.GetString(LegacyKeyV1, string.Empty);
        public void Write(string json) { PlayerPrefs.SetString(Key, json ?? string.Empty); PlayerPrefs.DeleteKey(LegacyKeyV11); PlayerPrefs.DeleteKey(LegacyKeyV10); PlayerPrefs.DeleteKey(LegacyKeyV9); PlayerPrefs.DeleteKey(LegacyKeyV8); PlayerPrefs.DeleteKey(LegacyKeyV7); PlayerPrefs.DeleteKey(LegacyKeyV6); PlayerPrefs.DeleteKey(LegacyKeyV5); PlayerPrefs.DeleteKey(LegacyKeyV4); PlayerPrefs.DeleteKey(LegacyKeyV3); PlayerPrefs.DeleteKey(LegacyKeyV2); PlayerPrefs.DeleteKey(LegacyKeyV1); PlayerPrefs.Save(); }
        public void Delete() { PlayerPrefs.DeleteKey(Key); PlayerPrefs.DeleteKey(LegacyKeyV11); PlayerPrefs.DeleteKey(LegacyKeyV10); PlayerPrefs.DeleteKey(LegacyKeyV9); PlayerPrefs.DeleteKey(LegacyKeyV8); PlayerPrefs.DeleteKey(LegacyKeyV7); PlayerPrefs.DeleteKey(LegacyKeyV6); PlayerPrefs.DeleteKey(LegacyKeyV5); PlayerPrefs.DeleteKey(LegacyKeyV4); PlayerPrefs.DeleteKey(LegacyKeyV3); PlayerPrefs.DeleteKey(LegacyKeyV2); PlayerPrefs.DeleteKey(LegacyKeyV1); PlayerPrefs.Save(); }
    }
}
