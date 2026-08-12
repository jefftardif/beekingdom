namespace BeeKingdom.Chat.Realtime;

public static class ChatRealtimeGroups
{
    public static string Conversation(Guid conversationId)
    {
        return $"conversation:{conversationId:N}";
    }
}
