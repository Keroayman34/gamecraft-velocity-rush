using NUnit.Framework;
using VelocityRush.Core;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Guards the T-8.11 session selection container: one stable vehicle id, fail-closed on empty
    /// input, and an explicit clear used by test isolation and the debug flow.
    /// </summary>
    public sealed class SelectionContextTests
    {
        [SetUp]
        public void SetUp()
        {
            SelectionContext.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            SelectionContext.Clear();
        }

        [Test]
        public void EmptyContext_HasNoSelection()
        {
            Assert.That(SelectionContext.HasSelection, Is.False);
            Assert.That(SelectionContext.SelectedVehicleId, Is.Null);
        }

        [Test]
        public void SetSelection_StoresTheTrimmedStableId()
        {
            Assert.That(SelectionContext.SetSelection("  starter-apex-v  "), Is.True);
            Assert.That(SelectionContext.HasSelection, Is.True);
            Assert.That(SelectionContext.SelectedVehicleId, Is.EqualTo("starter-apex-v"));
        }

        [Test]
        public void SetSelection_RejectsEmptyIds()
        {
            Assert.That(SelectionContext.SetSelection(null), Is.False);
            Assert.That(SelectionContext.SetSelection(string.Empty), Is.False);
            Assert.That(SelectionContext.SetSelection("   "), Is.False);
            Assert.That(SelectionContext.HasSelection, Is.False);
        }

        [Test]
        public void Clear_RemovesTheSelection()
        {
            SelectionContext.SetSelection("sport-pulse-r");
            SelectionContext.Clear();

            Assert.That(SelectionContext.HasSelection, Is.False);
            Assert.That(SelectionContext.SelectedVehicleId, Is.Null);
        }
    }
}
