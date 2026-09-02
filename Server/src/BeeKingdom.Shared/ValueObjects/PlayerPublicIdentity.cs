namespace BeeKingdom.Shared.ValueObjects;

// M043B-CL: the minimal, privacy-safe public identity of a player - PlayerId + DisplayName only.
// Deliberately lives in BeeKingdom.Shared (not BeeKingdom.Accounts or BeeKingdom.Alliance) so any
// domain (Alliance, Communication, Friends, mail recipient selection, ...) can depend on it without
// creating a cross-domain reference to Accounts. Never carries email, auth provider id, token, or
// any other private account metadata - see PlayerDirectoryService (BeeKingdom.Accounts) for the
// server-side resolver that builds this from AccountProfile.
public sealed record PlayerPublicIdentity(PlayerId PlayerId, string DisplayName);
