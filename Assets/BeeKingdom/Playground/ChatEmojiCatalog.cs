using System;
using System.Collections.Generic;

namespace BeeKingdom.Playground
{
    // Sprint-014 : catalogue des emoticones du Chat Premium. Architecture evolutive :
    // chaque element porte un Kind — aujourd'hui des Emoji Unicode, demain des Stickers
    // (rendus par une texture/asset au lieu d'un glyph), des packs et des emoticones
    // exclusives BeeKingdom — sans refonte de l'interface : ajouter une categorie au
    // catalogue suffit pour faire apparaitre un nouvel onglet.
    public enum ChatEmojiKind
    {
        Emoji,
        Sticker
    }

    public sealed class ChatEmojiItem
    {
        public ChatEmojiItem(string id, string value, ChatEmojiKind kind)
        {
            Id = id;
            Value = value;
            Kind = kind;
        }

        // Identifiant stable : sert de reference pour les packs, la telemetrie et le
        // remplacement futur d'un emoji par un sticker exclusif.
        public string Id { get; }

        // Glyphe Unicode pour ChatEmojiKind.Emoji ; id de pack/asset pour Sticker.
        public string Value { get; }

        public ChatEmojiKind Kind { get; }
    }

    public sealed class ChatEmojiCategory
    {
        public ChatEmojiCategory(string id, string tabIcon, string nameKey, string fallbackName, IList<ChatEmojiItem> items)
        {
            Id = id;
            TabIcon = tabIcon;
            NameKey = nameKey;
            FallbackName = fallbackName;
            Items = new List<ChatEmojiItem>(items);
        }

        public string Id { get; }

        // Emoji affiche sur l'onglet (ex. "😀", "❤️", "👍", "🎉", "🐝").
        public string TabIcon { get; }

        // Cle de localisation + nom de repli francais.
        public string NameKey { get; }

        public string FallbackName { get; }

        public List<ChatEmojiItem> Items { get; }
    }

    public static class ChatEmojiCatalog
    {
        // Categorie dynamique generee par le client (dernieres emoticones utilisees),
        // toujours affichee en premier onglet.
        public const string RecentCategoryId = "recents";

        public const int RecentMax = 12;

        private static ChatEmojiItem Emoji(string id, string value)
        {
            return new ChatEmojiItem(id, value, ChatEmojiKind.Emoji);
        }

        // La categorie BeeKingdom reste volontairement vide ce sprint : l'architecture
        // (items + Kind) est prete a accueillir les emoticones exclusives, stickers et
        // recompenses cosmétiques futures.
        public static readonly ChatEmojiCategory[] Categories =
        {
            new ChatEmojiCategory("smileys", "😀", "chat.emoji.tab.smileys", "Smileys", new ChatEmojiItem[]
            {
                Emoji("smileys-01", "😀"),
                Emoji("smileys-02", "😁"),
                Emoji("smileys-03", "😂"),
                Emoji("smileys-04", "🤣"),
                Emoji("smileys-05", "😊"),
                Emoji("smileys-06", "😍"),
                Emoji("smileys-07", "🥰"),
                Emoji("smileys-08", "😎"),
                Emoji("smileys-09", "🤩"),
                Emoji("smileys-10", "😜"),
                Emoji("smileys-11", "🤗"),
                Emoji("smileys-12", "😇"),
                Emoji("smileys-13", "🐝")
            }),
            new ChatEmojiCategory("emotions", "❤️", "chat.emoji.tab.emotions", "Émotions", new ChatEmojiItem[]
            {
                Emoji("emotions-01", "❤️"),
                Emoji("emotions-02", "💛"),
                Emoji("emotions-03", "💚"),
                Emoji("emotions-04", "💙"),
                Emoji("emotions-05", "💜"),
                Emoji("emotions-06", "😢"),
                Emoji("emotions-07", "😭"),
                Emoji("emotions-08", "😡"),
                Emoji("emotions-09", "🤬"),
                Emoji("emotions-10", "😱"),
                Emoji("emotions-11", "😨"),
                Emoji("emotions-12", "😴"),
                Emoji("emotions-13", "🤔"),
                Emoji("emotions-14", "🥺")
            }),
            new ChatEmojiCategory("gestures", "👍", "chat.emoji.tab.gestures", "Gestes", new ChatEmojiItem[]
            {
                Emoji("gestures-01", "👍"),
                Emoji("gestures-02", "👎"),
                Emoji("gestures-03", "👏"),
                Emoji("gestures-04", "🙏"),
                Emoji("gestures-05", "👌"),
                Emoji("gestures-06", "🤝"),
                Emoji("gestures-07", "✌️"),
                Emoji("gestures-08", "🤞"),
                Emoji("gestures-09", "💪"),
                Emoji("gestures-10", "🙌"),
                Emoji("gestures-11", "👋"),
                Emoji("gestures-12", "🤙")
            }),
            new ChatEmojiCategory("objects", "🎉", "chat.emoji.tab.objects", "Objets", new ChatEmojiItem[]
            {
                Emoji("objects-01", "🎉"),
                Emoji("objects-02", "🎁"),
                Emoji("objects-03", "🏆"),
                Emoji("objects-04", "⭐"),
                Emoji("objects-05", "🎯"),
                Emoji("objects-06", "🔥"),
                Emoji("objects-07", "💎"),
                Emoji("objects-08", "🍯"),
                Emoji("objects-09", "🎂"),
                Emoji("objects-10", "🎈"),
                Emoji("objects-11", "☕"),
                Emoji("objects-12", "🎊")
            }),
            new ChatEmojiCategory("beekingdom", "🐝", "chat.emoji.tab.beekingdom", "BeeKingdom", new ChatEmojiItem[] { })
        };

        public static ChatEmojiCategory CategoryById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < Categories.Length; i++)
                if (string.Equals(Categories[i].Id, id, StringComparison.Ordinal)) return Categories[i];
            return null;
        }
    }
}
