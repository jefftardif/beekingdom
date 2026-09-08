-- M065-CL - preuve runtime (session CEO du 2026-09-07, logs de production) : la colonne
-- InviterAcknowledged existe deja dans 094_chat_groups.sql, mais ce script est garde par
-- "IF OBJECT_ID(...) IS NULL" - la table dbo.ChatGroupInvites existait deja en production
-- (creee par un deploiement anterieur a l'ajout de cette colonne au script), donc la colonne
-- n'a jamais ete appliquee. Chaque appel a GET /chat/v1/invitations plantait avec
-- "Invalid column name 'InviterAcknowledged'" (SqlChatRepository.ListPendingInvitesForPlayer).
-- Additif et idempotent : sans effet si la colonne/l'index existe deja.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.ChatGroupInvites') AND name = 'InviterAcknowledged'
)
BEGIN
    ALTER TABLE dbo.ChatGroupInvites ADD InviterAcknowledged bit NOT NULL CONSTRAINT DF_ChatGroupInvites_InviterAcknowledged DEFAULT 0;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ChatGroupInvites') AND name = 'IX_ChatGroupInvites_Inviter_Ack'
)
BEGIN
    CREATE INDEX IX_ChatGroupInvites_Inviter_Ack ON dbo.ChatGroupInvites (InviterPlayerId, InviterAcknowledged);
END
