using UnityEngine;

namespace BeeKingdom.Networking
{
    [CreateAssetMenu(
        fileName = "MobileAccountSessionRuntime",
        menuName = "Bee Kingdom/Networking/Mobile Account Session Runtime")]
    public sealed class MobileAccountSessionRuntimeConfiguration : ScriptableObject
    {
        [SerializeField] private bool officialAccountsEnabled;
        [SerializeField] private bool officialGameplayEnabled;
        [SerializeField] private string baseUrl = string.Empty;
        [SerializeField] private string officialHiveId = string.Empty;
        [SerializeField] private string region = "ca-east";
        [SerializeField, Range(5, 120)] private int timeoutSeconds = 20;
        [SerializeField] private bool allowInsecureLoopbackForDevelopment;
        [SerializeField] private string googleOAuthClientId = string.Empty;

        public bool OfficialAccountsEnabled => officialAccountsEnabled;
        public bool OfficialGameplayEnabled => officialGameplayEnabled;
        public string BaseUrl => baseUrl == null ? string.Empty : baseUrl.Trim();
        public string OfficialHiveId => officialHiveId == null ? string.Empty : officialHiveId.Trim();
        public string Region => string.IsNullOrWhiteSpace(region) ? "unknown" : region.Trim();
        public int TimeoutSeconds => timeoutSeconds;
        public bool AllowInsecureLoopbackForDevelopment => allowInsecureLoopbackForDevelopment;
        public string GoogleOAuthClientId => googleOAuthClientId == null ? string.Empty : googleOAuthClientId.Trim();

        public string[] ProofRows()
        {
            return new[]
            {
                "mobile_auth_configuration_present:true",
                "mobile_auth_feature_enabled:" + officialAccountsEnabled.ToString().ToLowerInvariant(),
                "mobile_gameplay_feature_enabled:" + officialGameplayEnabled.ToString().ToLowerInvariant(),
                "mobile_auth_base_url_configured:" + (!string.IsNullOrEmpty(BaseUrl)).ToString().ToLowerInvariant(),
                "mobile_gameplay_hive_id_configured:" + (!string.IsNullOrEmpty(OfficialHiveId)).ToString().ToLowerInvariant(),
                "mobile_auth_region_configured:" + (!string.IsNullOrEmpty(Region)).ToString().ToLowerInvariant(),
                "mobile_auth_insecure_loopback:" + allowInsecureLoopbackForDevelopment.ToString().ToLowerInvariant(),
                "mobile_auth_embedded_secret:false"
            };
        }
    }
}
