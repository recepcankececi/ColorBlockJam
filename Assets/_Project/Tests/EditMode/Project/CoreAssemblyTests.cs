using System;
using System.Linq;
using CBJ.Core;
using NUnit.Framework;

namespace CBJ.Tests.EditMode.Project
{
    /// <summary>
    /// ADR-001: Core may use Unity math types but holds no engine objects, so it stays scene-free and testable.
    /// </summary>
    public class CoreAssemblyTests
    {
        [Test]
        public void CoreTypes_NoneDeriveFromUnityObject()
        {
            Type[] engineTypes = typeof(BoardModel).Assembly.GetTypes()
                .Where(t => typeof(UnityEngine.Object).IsAssignableFrom(t))
                .ToArray();

            Assert.That(engineTypes, Is.Empty);
        }
    }
}
