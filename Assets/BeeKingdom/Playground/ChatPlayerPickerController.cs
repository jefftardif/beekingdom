using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    // RAP-OPTIONNEL-COMMUNICATIONS_01 - "who do I want to talk to / add to my group" picker.
    //
    // Deliberately NOT reusing AllianceCenterPanelController's invite search: that list carries
    // alliance invitation row state (Sending/Sent/AlreadyPending) which means nothing here, and
    // sharing it would make a chat search overwrite what the alliance screen is showing. Both simply
    // sit on the same generic PlayerDirectoryClient, which is exactly what that client was built for.
    public enum ChatPlayerPickerStatus { Idle, Searching, Empty, Results, Error }

    public sealed class ChatPlayerPickerEntry
    {
        public ChatPlayerPickerEntry(Guid playerId, string displayName)
        {
            PlayerId = playerId;
            DisplayName = displayName ?? string.Empty;
        }

        public Guid PlayerId { get; }
        public string DisplayName { get; }
    }

    public interface IChatPlayerPickerController
    {
        IReadOnlyList<ChatPlayerPickerEntry> Results { get; }
        ChatPlayerPickerStatus Status { get; }
        bool IsConfigured { get; }
        void Search(string query);
        void Clear();
    }

    public sealed class UnavailableChatPlayerPickerController : IChatPlayerPickerController
    {
        public IReadOnlyList<ChatPlayerPickerEntry> Results => Array.Empty<ChatPlayerPickerEntry>();
        public ChatPlayerPickerStatus Status => ChatPlayerPickerStatus.Idle;
        public bool IsConfigured => false;
        public void Search(string query) { }
        public void Clear() { }
    }

    public sealed class ChatPlayerPickerController : IChatPlayerPickerController, IDisposable
    {
        private readonly IPlayerDirectoryClient directory;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private List<ChatPlayerPickerEntry> results = new List<ChatPlayerPickerEntry>();
        private readonly Dictionary<Guid, string> displayNames = new Dictionary<Guid, string>();
        private ChatPlayerPickerStatus status = ChatPlayerPickerStatus.Idle;
        private bool disposed;

        public ChatPlayerPickerController(IPlayerDirectoryClient directory)
        {
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        public IReadOnlyList<ChatPlayerPickerEntry> Results => results;
        public ChatPlayerPickerStatus Status => status;
        public bool IsConfigured => !disposed;

        public string ResolveDisplayName(Guid playerId) =>
            !disposed && displayNames.TryGetValue(playerId, out string name) ? name : null;

        public void Search(string query)
        {
            Task ignored = SearchCoreAsync(query);
            // Fire and forget: IMGUI polls Results/Status every frame, exactly like the alliance
            // search above it. Faults are already folded into Status by SearchCoreAsync.
            GC.KeepAlive(ignored);
        }

        public void Clear()
        {
            results = new List<ChatPlayerPickerEntry>();
            status = ChatPlayerPickerStatus.Idle;
        }

        public Task SearchForProofAsync(string query) => SearchCoreAsync(query);

        private async Task SearchCoreAsync(string query)
        {
            if (disposed) return;
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2) { Clear(); return; }

            status = ChatPlayerPickerStatus.Searching;
            try
            {
                List<RemotePlayerPublicIdentity> found = await directory.SearchAsync(query, 0, 20, lifetime.Token);
                if (disposed) return;
                results = (found ?? new List<RemotePlayerPublicIdentity>())
                    .Where(item => item != null && item.PlayerId != Guid.Empty)
                    .Select(item => new ChatPlayerPickerEntry(item.PlayerId, item.DisplayName))
                    .ToList();
                foreach (ChatPlayerPickerEntry entry in results)
                    if (!string.IsNullOrWhiteSpace(entry.DisplayName)) displayNames[entry.PlayerId] = entry.DisplayName;
                status = results.Count == 0 ? ChatPlayerPickerStatus.Empty : ChatPlayerPickerStatus.Results;
            }
            catch (OperationCanceledException) { }
            catch (Exception)
            {
                if (disposed) return;
                results = new List<ChatPlayerPickerEntry>();
                status = ChatPlayerPickerStatus.Error;
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            displayNames.Clear();
            try { lifetime.Cancel(); } catch (Exception) { }
            lifetime.Dispose();
        }
    }
}
