using System.Data;
using BeeKingdom.Persistence.Sql;

namespace BeeKingdom.Chat.Translations;

public sealed class SqlChatTranslationRepository(SqlConnectionFactory connectionFactory) : IChatTranslationRepository
{
    public ChatTranslationCacheEntry? Get(Guid messageId, string targetLocale, string modelVersion)
    {
        using IDbConnection connection = connectionFactory.CreateConnection(); connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT SourceLocale,TranslatedText,CreatedAtUtc FROM dbo.ChatMessageTranslations WHERE MessageId=@MessageId AND TargetLocale=@TargetLocale AND ModelVersion=@ModelVersion";
        Add(command,"@MessageId",messageId); Add(command,"@TargetLocale",targetLocale); Add(command,"@ModelVersion",modelVersion);
        using IDataReader reader = command.ExecuteReader();
        return reader.Read() ? new(messageId,targetLocale,modelVersion,reader.GetString(0),reader.GetString(1),new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(2),DateTimeKind.Utc))) : null;
    }

    public ChatTranslationCacheEntry SaveIfAbsent(ChatTranslationCacheEntry entry)
    {
        using IDbConnection connection = connectionFactory.CreateConnection(); connection.Open();
        using IDbTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        using IDbCommand command = connection.CreateCommand(); command.Transaction=transaction;
        command.CommandText = "IF NOT EXISTS(SELECT 1 FROM dbo.ChatMessageTranslations WITH(UPDLOCK,HOLDLOCK) WHERE MessageId=@MessageId AND TargetLocale=@TargetLocale AND ModelVersion=@ModelVersion) INSERT dbo.ChatMessageTranslations(MessageId,TargetLocale,ModelVersion,SourceLocale,TranslatedText,CreatedAtUtc) VALUES(@MessageId,@TargetLocale,@ModelVersion,@SourceLocale,@TranslatedText,@CreatedAtUtc);";
        Add(command,"@MessageId",entry.MessageId); Add(command,"@TargetLocale",entry.TargetLocale); Add(command,"@ModelVersion",entry.ModelVersion); Add(command,"@SourceLocale",entry.SourceLocale); Add(command,"@TranslatedText",entry.TranslatedText); Add(command,"@CreatedAtUtc",entry.CreatedAtUtc.UtcDateTime); command.ExecuteNonQuery(); transaction.Commit();
        return Get(entry.MessageId,entry.TargetLocale,entry.ModelVersion)!;
    }
    private static void Add(IDbCommand command,string name,object value){IDbDataParameter p=command.CreateParameter();p.ParameterName=name;p.Value=value;command.Parameters.Add(p);}
}
