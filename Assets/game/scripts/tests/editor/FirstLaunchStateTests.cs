using System;
using System.IO;
using Game.Systems.FirstLaunch;
using NUnit.Framework;
using Paowa.Save;

namespace Game.Tests.Editor
{
    /// <summary>First-launch flag on Core's real global store (temporary folder): pending once, remembered after restart, reset.</summary>
    public sealed class FirstLaunchStateTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "pc-base-first-launch-tests", Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [Test]
        public void PendingUntilCompleted_ThenRememberedAcrossRestarts()
        {
            ISaveSlotService save = SaveServiceFactory.CreateForTests(_root);
            var state = new FirstLaunchState(save.Global, save.FlushGlobal);
            Assert.That(state.IsPending, Is.True);

            state.Complete();
            Assert.That(state.IsPending, Is.False);
            Assert.That(save.Global.Get(FirstLaunchState.DoneKey, false), Is.True);

            // A new service on the same folder reads what Complete flushed.
            ISaveSlotService restarted = SaveServiceFactory.CreateForTests(_root);
            Assert.That(new FirstLaunchState(restarted.Global, restarted.FlushGlobal).IsPending, Is.False);
        }

        [Test]
        public void ResetMakesItDueAgain()
        {
            ISaveSlotService save = SaveServiceFactory.CreateForTests(_root);
            var state = new FirstLaunchState(save.Global, save.FlushGlobal);
            state.Complete();

            state.Reset();

            Assert.That(state.IsPending, Is.True);
            ISaveSlotService restarted = SaveServiceFactory.CreateForTests(_root);
            Assert.That(new FirstLaunchState(restarted.Global, restarted.FlushGlobal).IsPending, Is.True);
        }

        [Test]
        public void WithoutAStoreItIsNeverDue()
        {
            var state = new FirstLaunchState(null, null);

            Assert.That(state.IsPending, Is.False);
            Assert.DoesNotThrow(state.Complete);
            Assert.DoesNotThrow(state.Reset);
        }
    }
}
