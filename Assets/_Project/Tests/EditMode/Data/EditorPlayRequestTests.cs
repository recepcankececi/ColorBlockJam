using CBJ.Data;
using NUnit.Framework;

namespace CBJ.Tests.EditMode.Data
{
    public class EditorPlayRequestTests
    {
        [TearDown]
        public void ClearRequest()
        {
            EditorPlayRequest.TryConsume(out _);
        }

        [Test]
        public void TryConsume_AfterSet_ReturnsRequestedIndex()
        {
            EditorPlayRequest.Set(3);

            EditorPlayRequest.TryConsume(out int index);

            Assert.That(index, Is.EqualTo(3));
        }

        [Test]
        public void TryConsume_Twice_SecondCallFindsNoRequest()
        {
            EditorPlayRequest.Set(3);
            EditorPlayRequest.TryConsume(out _);

            Assert.That(EditorPlayRequest.TryConsume(out _), Is.False);
        }
    }
}
