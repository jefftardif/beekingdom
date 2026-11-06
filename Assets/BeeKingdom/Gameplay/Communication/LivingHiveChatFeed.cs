using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Gameplay.Communication
{
    [Flags]
    public enum LivingHiveChatFeedChannels { None=0, World=1, Alliance=2, Private=4, Group=8 }

    // Projection only: never selects a conversation, joins a channel or marks a message read.
    public static class LivingHiveChatFeed
    {
        public static LivingHiveChatFeedChannels Channel(string type)
        {
            if(string.Equals(type,"Server",StringComparison.OrdinalIgnoreCase))return LivingHiveChatFeedChannels.World;
            if(string.Equals(type,"Alliance",StringComparison.OrdinalIgnoreCase))return LivingHiveChatFeedChannels.Alliance;
            if(string.Equals(type,"Private",StringComparison.OrdinalIgnoreCase))return LivingHiveChatFeedChannels.Private;
            if(string.Equals(type,"Group",StringComparison.OrdinalIgnoreCase))return LivingHiveChatFeedChannels.Group;
            return LivingHiveChatFeedChannels.None;
        }

        public static bool IsLive(LivingHiveChatSnapshot snapshot) => snapshot!=null &&
            (snapshot.Status==LivingHiveChatStatus.Online || snapshot.Status==LivingHiveChatStatus.Polling);

        public static LivingHiveChatFeedChannels Available(LivingHiveChatSnapshot snapshot)
        {
            if(!IsLive(snapshot))return LivingHiveChatFeedChannels.None;
            var result=LivingHiveChatFeedChannels.None;
            foreach(var conversation in snapshot.Conversations)
                if(conversation!=null)result|=Channel(conversation.ChannelType);
            return result;
        }

        public static IReadOnlyList<LivingHiveChatMessage> Select(LivingHiveChatSnapshot snapshot,LivingHiveChatFeedChannels filter)
        {
            if(!IsLive(snapshot) || filter==LivingHiveChatFeedChannels.None)return Array.Empty<LivingHiveChatMessage>();
            var allowed=new HashSet<string>(snapshot.Conversations.Where(c=>c!=null &&
                (Channel(c.ChannelType)&filter)!=0).Select(c=>c.ConversationId),StringComparer.Ordinal);
            return snapshot.FeedMessages.Concat(snapshot.Messages)
                .Where(m=>m!=null && m.Delivery==LivingHiveChatDelivery.Confirmed && allowed.Contains(m.ConversationId))
                .GroupBy(m=>m.ConversationId+"/"+(string.IsNullOrEmpty(m.MessageId)?m.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture):m.MessageId),StringComparer.Ordinal)
                .Select(group=>group.Last()).OrderByDescending(m=>m.CreatedAt).ThenByDescending(m=>m.Sequence)
                .ThenBy(m=>m.ConversationId,StringComparer.Ordinal).Take(3).Reverse().ToArray();
        }

        public static LivingHiveChatConversation Resolve(LivingHiveChatSnapshot snapshot,string conversationId)
        {
            if(!IsLive(snapshot) || string.IsNullOrEmpty(conversationId))return null;
            return snapshot.Conversations.FirstOrDefault(c=>c!=null && string.Equals(c.ConversationId,conversationId,StringComparison.Ordinal));
        }
    }
}
