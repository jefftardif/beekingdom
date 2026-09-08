-- Clear-AllChatData.sql
-- M065-CL - efface TOUTES les donnees chat (conversations, messages, groupes,
-- invitations) pour repartir sur des bases saines. Demande explicitement par
-- le CEO le 2026-09-07 pour la base de PRODUCTION (Chat non encore utilise en
-- production a ce moment).
--
-- Ordre de suppression respectant les contraintes de cle etrangere existantes
-- (voir Server/src/BeeKingdom.Database/Scripts/060-063_chat_*.sql et
-- 094_chat_groups.sql) : les tables enfants d'abord, les tables referencees
-- (ChatConversations, ChatModerationReports) en dernier.
--
-- ChatPreferences (reglages par joueur, pas une "discussion") n'est PAS
-- touchee ici - demande separee si voulu.
--
-- A executer manuellement contre la base de production, avec un acces SQL
-- deja configure (ce depot ne contient aucun secret de connexion).

SET NOCOUNT ON;
BEGIN TRANSACTION;

DELETE FROM dbo.ChatModerationReportReceipts;
DELETE FROM dbo.ChatModerationReports;
DELETE FROM dbo.ChatMessageTranslations;   -- redondant (ON DELETE CASCADE depuis ChatMessages), garde par clarte
DELETE FROM dbo.ChatMessages;
DELETE FROM dbo.ChatConversationCreationReceipts;
DELETE FROM dbo.ChatGroupInvites;
DELETE FROM dbo.ChatConversationParticipants;
DELETE FROM dbo.ChatConversationSequences;
DELETE FROM dbo.ChatInbox;
DELETE FROM dbo.ChatOutboxReceipts;
DELETE FROM dbo.ChatConversations;

COMMIT TRANSACTION;

SELECT 'ChatConversations' AS TableName, COUNT(*) AS RemainingRows FROM dbo.ChatConversations
UNION ALL SELECT 'ChatMessages', COUNT(*) FROM dbo.ChatMessages
UNION ALL SELECT 'ChatConversationParticipants', COUNT(*) FROM dbo.ChatConversationParticipants
UNION ALL SELECT 'ChatInbox', COUNT(*) FROM dbo.ChatInbox
UNION ALL SELECT 'ChatGroupInvites', COUNT(*) FROM dbo.ChatGroupInvites
UNION ALL SELECT 'ChatModerationReports', COUNT(*) FROM dbo.ChatModerationReports;
