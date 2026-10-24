using System;
using UnityEngine;
using BeeKingdom.Networking;
#if UNITY_EDITOR
using System.IO;
#endif

namespace BeeKingdom.Playground
{
    public static partial class MobileAccountSessionRuntimeBootstrap
    {
#if UNITY_EDITOR
        private const string StagingLoopbackEndpoint = "http://127.0.0.1:6269";
        private const string StagingGoogleClientId = "209838375708-j85ov0lc01ub2710o96ok4akmdagrrjq.apps.googleusercontent.com";

        // Visual-only affordance: the Google action remains gated by its
        // actual OAuth client ID and a ready official account session.
        internal static bool AcademyStagingSelectedForEditor =>
            activeConfiguration != null &&
            AcademyStagingEndpointAllowedForProof(activeConfiguration.BaseUrl);

        // A temporary Cloudflare HTTPS tunnel is acceptable ONLY with the
        // 256-bit unguessable route prefix created by the staging gateway.
        // This logic is Editor-only; no remote staging URL is embedded in builds.
        internal static bool AcademyStagingEndpointAllowedForProof(string value)
        {
            if (string.Equals(value, StagingLoopbackEndpoint,
                    StringComparison.OrdinalIgnoreCase))
                return true;
            Uri uri;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase) ||
                !uri.Host.EndsWith(".trycloudflare.com",
                    StringComparison.OrdinalIgnoreCase) ||
                !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.UserInfo) ||
                !string.IsNullOrEmpty(uri.Query) ||
                !string.IsNullOrEmpty(uri.Fragment))
                return false;
            const string route = "/stage-";
            string path = uri.AbsolutePath;
            if (!path.StartsWith(route, StringComparison.Ordinal) ||
                path.Length != route.Length + 64)
                return false;
            for (int i = route.Length; i < path.Length; i++)
                if (!((path[i] >= '0' && path[i] <= '9') ||
                      (path[i] >= 'a' && path[i] <= 'f')))
                    return false;
            return true;
        }

        // Putting this control in Temp rather than Resources keeps production
        // configuration and committed assets byte-for-byte intact. It is
        // deliberately absent from iOS/Android/player builds.
        private static MobileAccountSessionRuntimeConfiguration
            ResolveLocalAcademyStagingConfig(
                MobileAccountSessionRuntimeConfiguration standard)
        {
            if (standard == null) return null;
            string project = Path.GetDirectoryName(Application.dataPath);
            string file = Path.Combine(project,"Temp",
                "BKAcademyStagingUnity.json");
            if (!File.Exists(file)) return standard;
            MobileAccountSessionRuntimeConfiguration copy = null;
            try
            {
                copy = UnityEngine.Object.Instantiate(standard);
                copy.hideFlags = HideFlags.DontSave;
                JsonUtility.FromJsonOverwrite(File.ReadAllText(file),copy);
                Guid stagingHive;
                if (!AcademyStagingEndpointAllowedForProof(copy.BaseUrl) ||
                    !copy.OfficialAccountsEnabled || !copy.OfficialGameplayEnabled ||
                    !copy.AllowInsecureLoopbackForDevelopment ||
                    !Guid.TryParseExact(copy.OfficialHiveId,"D",out stagingHive) ||
                    stagingHive == Guid.Empty ||
                    string.Equals(copy.OfficialHiveId,standard.OfficialHiveId,
                        StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(copy.GoogleOAuthClientId) &&
                     !string.Equals(copy.GoogleOAuthClientId,
                         StagingGoogleClientId, StringComparison.Ordinal)))
                    throw new InvalidDataException(
                        "Staging endpoint must be approved loopback or secret HTTPS staging route with a new hive and no OAuth client.");
                // Never log the secret route token for HTTPS staging.
                Uri stageUri = new Uri(copy.BaseUrl);
                Debug.Log("[BK-Academy-Staging] Unity Editor staging selected: " +
                    stageUri.Scheme + "://" + stageUri.Host +
                    (stageUri.IsDefaultPort ? "" : ":" + stageUri.Port.ToString()) +
                    " (production configuration untouched).");
                return copy;
            }
            catch (Exception error)
            {
                if (copy != null) UnityEngine.Object.DestroyImmediate(copy);
                // Fail closed: if the staging marker exists but is invalid,
                // NEVER silently fall back to the production endpoint.
                Debug.LogError("[BK-Academy-Staging] Invalid local staging configuration: " +
                    error.GetType().Name + ". Account startup blocked.");
                return null;
            }
        }
#endif
    }
}
