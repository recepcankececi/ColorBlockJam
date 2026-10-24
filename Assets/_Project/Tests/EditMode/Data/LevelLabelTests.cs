using CBJ.Data;
using NUnit.Framework;

namespace CBJ.Tests.EditMode.Data
{
    public class LevelLabelTests
    {
        [Test]
        public void For_FirstCatalogPosition_IsLevelOne()
        {
            Assert.That(LevelLabel.For(0), Is.EqualTo("Level 1"));
        }

        [Test]
        public void For_FifthCatalogPosition_IsLevelFive()
        {
            Assert.That(LevelLabel.For(4), Is.EqualTo("Level 5"));
        }
    }
}
