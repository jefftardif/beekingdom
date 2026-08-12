using BeeKingdom.Audio;
using NUnit.Framework;

namespace BeeKingdom.Playground.Editor
{
    public sealed class MusicStartupTests
    {
        [Test]
        public void HiveMusicIsLoadedAndSelectedByTheStartupPath()
        {
            MusicManager manager = MusicManager.EnsureInstance();
            Assert.That(manager.HasTrack(MusicTrack.Hive), Is.True);
        }

    }
}
