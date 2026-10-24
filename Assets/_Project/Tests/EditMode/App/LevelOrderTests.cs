using CBJ.App;
using NUnit.Framework;

namespace CBJ.Tests.EditMode.App
{
    public class LevelOrderTests
    {
        [Test]
        public void Resume_SavedIndexInCatalog_ReturnsIt()
        {
            Assert.That(LevelOrder.Resume(3, 5), Is.EqualTo(3));
        }

        [Test]
        public void Resume_SavedIndexPastShrunkCatalog_ReturnsFirstLevel()
        {
            Assert.That(LevelOrder.Resume(5, 3), Is.EqualTo(0));
        }

        [Test]
        public void Resume_EmptyCatalog_ReturnsFirstLevel()
        {
            Assert.That(LevelOrder.Resume(2, 0), Is.EqualTo(0));
        }

        [Test]
        public void Next_MiddleLevel_ReturnsFollowingLevel()
        {
            Assert.That(LevelOrder.Next(1, 5), Is.EqualTo(2));
        }

        [Test]
        public void Next_LastLevel_WrapsToFirstLevel()
        {
            Assert.That(LevelOrder.Next(4, 5), Is.EqualTo(0));
        }
    }
}
