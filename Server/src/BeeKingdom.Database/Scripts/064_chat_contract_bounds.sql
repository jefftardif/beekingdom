IF COL_LENGTH(N'dbo.ChatMessages', N'Body') IS NOT NULL ALTER TABLE dbo.ChatMessages ALTER COLUMN Body nvarchar(4000) NOT NULL;
IF COL_LENGTH(N'dbo.ChatMessages', N'ClientRequestId') IS NOT NULL ALTER TABLE dbo.ChatMessages ALTER COLUMN ClientRequestId nvarchar(256) NOT NULL;
IF COL_LENGTH(N'dbo.ChatOutboxReceipts', N'ClientRequestId') IS NOT NULL ALTER TABLE dbo.ChatOutboxReceipts ALTER COLUMN ClientRequestId nvarchar(256) NOT NULL;
IF COL_LENGTH(N'dbo.ChatConversationCreationReceipts', N'ClientRequestId') IS NOT NULL ALTER TABLE dbo.ChatConversationCreationReceipts ALTER COLUMN ClientRequestId nvarchar(256) NOT NULL;
IF COL_LENGTH(N'dbo.ChatModerationReportReceipts', N'ClientRequestId') IS NOT NULL ALTER TABLE dbo.ChatModerationReportReceipts ALTER COLUMN ClientRequestId nvarchar(256) NOT NULL;
IF COL_LENGTH(N'dbo.ChatMessageTranslations', N'TargetLocale') IS NOT NULL ALTER TABLE dbo.ChatMessageTranslations ALTER COLUMN TargetLocale nvarchar(35) NOT NULL;
IF COL_LENGTH(N'dbo.ChatMessageTranslations', N'ModelVersion') IS NOT NULL ALTER TABLE dbo.ChatMessageTranslations ALTER COLUMN ModelVersion nvarchar(128) NOT NULL;
IF COL_LENGTH(N'dbo.ChatMessageTranslations', N'TranslatedText') IS NOT NULL ALTER TABLE dbo.ChatMessageTranslations ALTER COLUMN TranslatedText nvarchar(max) NOT NULL;
