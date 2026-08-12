using System;
using System.Collections.Generic;

namespace BeeKingdom.Networking
{
    public enum AccountSessionReadinessState
    {
        NotConfigured = 0,
        Checking = 1,
        PreparationOnly = 2,
        Unavailable = 3,
        Ready = 4
    }

    public sealed class AccountSessionReadinessSnapshot
    {
        public AccountSessionReadinessSnapshot(
            AccountSessionReadinessState state,
            bool accountCreationAllowed,
            bool sessionCreationAllowed,
            bool tokenIssuanceAllowed,
            bool liveAccounts,
            bool liveSessions,
            string errorCode = "")
        {
            State = state;
            AccountCreationAllowed = accountCreationAllowed;
            SessionCreationAllowed = sessionCreationAllowed;
            TokenIssuanceAllowed = tokenIssuanceAllowed;
            LiveAccounts = liveAccounts;
            LiveSessions = liveSessions;
            ErrorCode = errorCode ?? string.Empty;
        }

        public AccountSessionReadinessState State { get; }
        public bool AccountCreationAllowed { get; }
        public bool SessionCreationAllowed { get; }
        public bool TokenIssuanceAllowed { get; }
        public bool LiveAccounts { get; }
        public bool LiveSessions { get; }
        public string ErrorCode { get; }

        public bool ServerAllowsLogin => State == AccountSessionReadinessState.Ready
            && SessionCreationAllowed
            && TokenIssuanceAllowed
            && LiveAccounts
            && LiveSessions;

        public bool ServerAllowsAccountCreation => State == AccountSessionReadinessState.Ready
            && AccountCreationAllowed
            && TokenIssuanceAllowed
            && LiveAccounts;

        public static AccountSessionReadinessSnapshot NotConfigured()
        {
            return new AccountSessionReadinessSnapshot(AccountSessionReadinessState.NotConfigured, false, false, false, false, false);
        }

        public static AccountSessionReadinessSnapshot Checking()
        {
            return new AccountSessionReadinessSnapshot(AccountSessionReadinessState.Checking, false, false, false, false, false);
        }

        public static AccountSessionReadinessSnapshot FromServer(
            bool accountCreationAllowed,
            bool sessionCreationAllowed,
            bool tokenIssuanceAllowed,
            bool liveAccounts,
            bool liveSessions)
        {
            bool ready = tokenIssuanceAllowed && liveAccounts
                && ((sessionCreationAllowed && liveSessions) || accountCreationAllowed);
            return new AccountSessionReadinessSnapshot(
                ready ? AccountSessionReadinessState.Ready : AccountSessionReadinessState.PreparationOnly,
                accountCreationAllowed,
                sessionCreationAllowed,
                tokenIssuanceAllowed,
                liveAccounts,
                liveSessions);
        }

        public static AccountSessionReadinessSnapshot Unavailable(string errorCode)
        {
            return new AccountSessionReadinessSnapshot(AccountSessionReadinessState.Unavailable, false, false, false, false, false, errorCode);
        }
    }

    public sealed class MobileAccountSessionGate
    {
        private AccountSessionReadinessSnapshot snapshot = AccountSessionReadinessSnapshot.NotConfigured();

        public AccountSessionReadinessSnapshot Snapshot => snapshot;
        public bool TransportConfigured { get; private set; }
        public bool CanCollectCredentials => TransportConfigured && snapshot.ServerAllowsLogin;
        public bool CanSubmitLogin => CanCollectCredentials;
        public bool CanCreateOfficialAccount => TransportConfigured && snapshot.ServerAllowsAccountCreation;

        public void ConfigureTransport(bool configured)
        {
            TransportConfigured = configured;
        }

        public void Apply(AccountSessionReadinessSnapshot value)
        {
            snapshot = value ?? AccountSessionReadinessSnapshot.Unavailable("readiness.empty");
        }

        public void ResetForLogoutOrPlayerChange()
        {
            snapshot = AccountSessionReadinessSnapshot.NotConfigured();
            TransportConfigured = false;
        }

        public IReadOnlyList<string> ProofRows()
        {
            return new[]
            {
                "account_shell_state:" + snapshot.State,
                "account_transport_configured:" + TransportConfigured.ToString().ToLowerInvariant(),
                "account_server_allows_login:" + snapshot.ServerAllowsLogin.ToString().ToLowerInvariant(),
                "account_server_allows_creation:" + snapshot.ServerAllowsAccountCreation.ToString().ToLowerInvariant(),
                "credential_collection_allowed:" + CanCollectCredentials.ToString().ToLowerInvariant(),
                "login_submission_allowed:" + CanSubmitLogin.ToString().ToLowerInvariant(),
                "official_account_creation_allowed:" + CanCreateOfficialAccount.ToString().ToLowerInvariant(),
                "access_token_stored_here:false",
                "refresh_token_stored_here:false",
                "password_stored_here:false"
            };
        }
    }
}
