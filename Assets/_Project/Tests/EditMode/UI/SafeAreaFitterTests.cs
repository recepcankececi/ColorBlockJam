using CBJ.UI;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.UI
{
    public class SafeAreaFitterTests
    {
        [Test]
        public void Anchors_FullScreen_CoverZeroToOne()
        {
            SafeAreaFitter.Anchors(new Rect(0f, 0f, 1080f, 1920f), new Vector2(1080f, 1920f), out Vector2 min, out Vector2 max);

            Assert.That((min, max), Is.EqualTo((Vector2.zero, Vector2.one)));
        }

        [Test]
        public void Anchors_NotchAtTop_LowersTheTopAnchor()
        {
            SafeAreaFitter.Anchors(new Rect(0f, 0f, 1080f, 2160f), new Vector2(1080f, 2400f), out Vector2 min, out Vector2 max);

            Assert.That(min, Is.EqualTo(Vector2.zero));
            Assert.That(max.y, Is.EqualTo(0.9f).Within(1e-5f));
        }
    }
}
