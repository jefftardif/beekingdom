namespace BeeKingdom.Core.Services
{
    using BeeKingdom.Core.Save;

    public interface ISaveService : IGameService
    {
        SaveDiagnostics Diagnostics { get; }
        SaveSnapshot CreateSnapshot(string payload = "");
        void Save(string slot, SaveSnapshot snapshot);
        bool TryLoad(string slot, out SaveSnapshot snapshot);
        bool Validate(SaveSnapshot snapshot);
        SaveSnapshot Migrate(SaveSnapshot snapshot);
        void AutoSave(string slot, SaveSnapshot snapshot);
        bool HasSave(string key);
        void Save(string key, string payload);
        bool TryLoad(string key, out string payload);
        void Delete(string key);
    }
}
