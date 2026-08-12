namespace BeeKingdom.Core.Save
{
    public abstract class SaveRepository
    {
        public abstract bool Exists(string slot);
        public abstract void Write(string slot, string data);
        public abstract bool TryRead(string slot, out string data);
        public abstract void Delete(string slot);
    }
}
