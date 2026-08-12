using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    // Carnet du Bestiaire (demande de Jeff, 2026-08-01) : lecture seule, sous-produit du flux
    // Combat Patrol existant - aucune mutation, aucune idempotence necessaire ici.
    public enum BestiaryCodexClientError
    {
        NotConfigured = 0,
        AuthenticationRequired = 1,
        InvalidRequest = 2,
        InvalidResponse = 3,
        TransportFailure = 4
    }

    public sealed class BestiaryCodexClientException : Exception
    {
        public BestiaryCodexClientException(BestiaryCodexClientError error, string message) : base(message) { Error = error; }
        public BestiaryCodexClientError Error { get; }
    }

    public sealed class RemoteBestiaryCodexEntry
    {
        public int Tier { get; set; }
        public string EnemyName { get; set; }
        public string HazardFamily { get; set; }
        public long EncounterCount { get; set; }
        public string BestBand { get; set; }
        public bool Mastered { get; set; }
        public bool Legendary { get; set; }
        public DateTimeOffset? FirstEncounteredAtUtc { get; set; }
        public DateTimeOffset? LastEncounteredAtUtc { get; set; }
        public long TotalHoneyCredited { get; set; }
        public long TotalPollenCredited { get; set; }
        public long DailyFocusEncounterCount { get; set; }
        public List<string> LastContributingChampionBeeIds { get; set; }
        public string LastStrategicPathId { get; set; }
        public long LastHoneyCredited { get; set; }
        public long LastPollenCredited { get; set; }
        public DateTimeOffset? BestBandAchievedAtUtc { get; set; }
        public string LastBand { get; set; }
    }

    public sealed class RemoteBestiaryCodexSnapshot
    {
        public DateTimeOffset ServerTimeUtc { get; set; }
        public List<RemoteBestiaryCodexEntry> Tiers { get; set; }
        public int MasteredTierCount { get; set; }
        public int TotalTierCount { get; set; }
        public long MasteryEncounterThreshold { get; set; }
    }

    public interface IBestiaryCodexClient
    {
        Task<RemoteBestiaryCodexSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default);
    }

    public sealed class BestiaryCodexClient : IBestiaryCodexClient
    {
        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;

        public BestiaryCodexClient(MobileAccountSessionGate sessionGate, IGameAccountSessionSource sessionSource, IAuthenticatedGameRestTransport transport)
        {
            this.sessionGate = sessionGate ?? throw new ArgumentNullException(nameof(sessionGate));
            this.sessionSource = sessionSource ?? throw new ArgumentNullException(nameof(sessionSource));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public async Task<RemoteBestiaryCodexSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default)
        {
            if (hiveId == Guid.Empty) throw new BestiaryCodexClientException(BestiaryCodexClientError.InvalidRequest, "A hive identifier is required.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!sessionGate.CanSubmitLogin)
                throw new BestiaryCodexClientException(BestiaryCodexClientError.NotConfigured, "Official account session transport is not ready.");
            if (!sessionSource.TryGetSession(out GameAccountSession session) || session == null || session.PlayerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(session.AccessToken) || session.AccessToken.Length > 8192)
                throw new BestiaryCodexClientException(BestiaryCodexClientError.AuthenticationRequired, "An official account session is required.");

            RemoteBestiaryCodexSnapshot response;
            try
            {
                response = await transport.SendAsync<RemoteBestiaryCodexSnapshot>(
                    new AuthenticatedGameRestRequest("GET", BoardPath(hiveId)), session.AccessToken, cancellationToken).ConfigureAwait(false);
            }
            catch (AuthenticatedGameRestException exception)
            {
                throw MapTransportFailure(exception);
            }
            if (response == null) throw new BestiaryCodexClientException(BestiaryCodexClientError.InvalidResponse, "The bestiary codex response is empty.");
            response.Tiers ??= new List<RemoteBestiaryCodexEntry>();
            return response;
        }

        private static BestiaryCodexClientException MapTransportFailure(AuthenticatedGameRestException exception)
        {
            if (exception.Error == AuthenticatedGameRestError.NetworkFailure) return new BestiaryCodexClientException(BestiaryCodexClientError.TransportFailure, exception.SafeCode);
            if (exception.Error == AuthenticatedGameRestError.Unauthorized) return new BestiaryCodexClientException(BestiaryCodexClientError.AuthenticationRequired, exception.SafeCode);
            return new BestiaryCodexClientException(BestiaryCodexClientError.InvalidResponse, exception.SafeCode);
        }

        public static string BoardPath(Guid hiveId) => "/game/v1/hives/" + hiveId.ToString("D") + "/bestiary-codex";
    }
}
