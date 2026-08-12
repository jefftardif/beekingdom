using System.IO;

namespace BeeKingdom.Core.Save
{
    public sealed class FileSaveRepository : SaveRepository
    {
        private readonly string rootPath;

        public FileSaveRepository(string rootPath)
        {
            this.rootPath = rootPath;
        }

        public override bool Exists(string slot)
        {
            return File.Exists(GetPath(slot));
        }

        public override void Write(string slot, string data)
        {
            Directory.CreateDirectory(rootPath);
            File.WriteAllText(GetPath(slot), data);
        }

        public override bool TryRead(string slot, out string data)
        {
            string path = GetPath(slot);
            if (!File.Exists(path))
            {
                data = null;
                return false;
            }

            data = File.ReadAllText(path);
            return true;
        }

        public override void Delete(string slot)
        {
            string path = GetPath(slot);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private string GetPath(string slot)
        {
            string safeSlot = string.IsNullOrWhiteSpace(slot) ? "default" : slot;
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                safeSlot = safeSlot.Replace(invalid, '_');
            }

            return Path.Combine(rootPath, safeSlot + ".bksave");
        }
    }
}
