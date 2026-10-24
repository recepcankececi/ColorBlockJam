using CBJ.Core;
using CBJ.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace CBJ.Tests.EditMode.Gameplay
{
    public class DragDirectionTests
    {
        private const float Threshold = 0.5f;

        [Test]
        public void Resolve_LeadBelowThreshold_ReturnsNoAxis()
        {
            int axes = DragDirection.Resolve(new Vector2(0.4f, -0.3f), Threshold, out _, out _);

            Assert.That(axes, Is.Zero);
        }

        [Test]
        public void Resolve_LeadRight_StepsRight()
        {
            int axes = DragDirection.Resolve(new Vector2(0.6f, 0.1f), Threshold, out Direction first, out _);

            Assert.That((axes, first), Is.EqualTo((1, Direction.Right)));
        }

        [Test]
        public void Resolve_LeadDown_StepsDown()
        {
            int axes = DragDirection.Resolve(new Vector2(-0.2f, -1.5f), Threshold, out Direction first, out _);

            Assert.That((axes, first), Is.EqualTo((1, Direction.Down)));
        }

        [Test]
        public void Resolve_SmallerAxisAloneReachesThreshold_StepsAlongIt()
        {
            int axes = DragDirection.Resolve(new Vector2(0.45f, 0.5f), Threshold, out Direction first, out _);

            Assert.That((axes, first), Is.EqualTo((1, Direction.Up)));
        }

        [Test]
        public void Resolve_BothAxesReachThreshold_DominantAxisFirst()
        {
            int axes = DragDirection.Resolve(new Vector2(-0.8f, 1.2f), Threshold, out Direction first, out Direction second);

            Assert.That((axes, first, second), Is.EqualTo((2, Direction.Up, Direction.Left)));
        }
    }
}
