using System;
using System.Collections.Generic;
using Game.Contracts.Presentation;
using Game.Contracts.Save;
using Game.Systems.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor
{
    /// <summary>
    /// Leaving gameplay from the pause menu: ask, stop playtime, save, and only then switch scene or quit; declining changes
    /// nothing; a failed save stays with a notice and playtime counting again; without a slot "Leave without saving?" is
    /// asked (declining stays); a switch that does not start stays; without a dialog the quit question counts as yes and the
    /// unsaved question as no; one action at a time; Save reports saved, failed or no slot.
    /// </summary>
    public sealed class GameplayExitFlowTests
    {
        private readonly List<string> _log = new List<string>();
        private readonly Queue<bool> _answers = new Queue<bool>();
        private readonly List<ConfirmRequest> _asked = new List<ConfirmRequest>();
        private FakeCheckpoint _checkpoint;
        private FakePlaytime _playtime;
        private string _slot;
        private bool _switchStarts;
        private AwaitableCompletionSource<bool> _pendingAnswer;

        [SetUp]
        public void SetUp()
        {
            _log.Clear();
            _answers.Clear();
            _asked.Clear();
            _checkpoint = new FakeCheckpoint(_log) { Result = true };
            _playtime = new FakePlaytime(_log);
            _playtime.StartCounting();
            _log.Clear();
            _slot = "slot-1";
            _switchStarts = true;
            _pendingAnswer = null;
        }

        [Test]
        public void QuitToMenuAsksThenSavesThenLeaves()
        {
            _answers.Enqueue(true);

            Assert.That(Run(Flow().QuitToMenuAsync(Leave)), Is.True);

            Assert.That(_asked, Has.Count.EqualTo(1));
            Assert.That(_asked[0].TitleKey, Is.EqualTo("menu.title"));
            Assert.That(_asked[0].HasCancel, Is.True);
            Assert.That(_log, Is.EqualTo(new[] { "stop", "save", "leave" }));
            Assert.That(_playtime.IsCounting, Is.False);
        }

        [Test]
        public void QuitToDesktopAsksThenSavesThenQuits()
        {
            _answers.Enqueue(true);

            Assert.That(Run(Flow().QuitToDesktopAsync(() => _log.Add("quit"))), Is.True);

            Assert.That(_asked[0].TitleKey, Is.EqualTo("desktop.title"));
            Assert.That(_log, Is.EqualTo(new[] { "stop", "save", "quit" }));
        }

        [Test]
        public void DecliningChangesNothing()
        {
            _answers.Enqueue(false);

            Assert.That(Run(Flow().QuitToMenuAsync(Leave)), Is.False);

            Assert.That(_log, Is.Empty, "No save, no leaving, playtime untouched.");
            Assert.That(_playtime.IsCounting, Is.True);
        }

        [Test]
        public void AFailedSaveStaysWithANoticeAndPlaytimeCountsAgain()
        {
            _answers.Enqueue(true);
            _checkpoint.Result = false;
            var notices = new List<string>();

            Assert.That(Run(Flow(notices.Add).QuitToDesktopAsync(() => _log.Add("quit"))), Is.False);

            Assert.That(_log, Is.EqualTo(new[] { "stop", "save", "start" }));
            Assert.That(notices, Is.EqualTo(new[] { "failed" }));
            Assert.That(_playtime.IsCounting, Is.True);
        }

        [Test]
        public void WithoutASlotItAsksBeforeLeavingUnsaved()
        {
            _slot = null;
            _answers.Enqueue(true);
            _answers.Enqueue(true);

            Assert.That(Run(Flow().QuitToMenuAsync(Leave)), Is.True);

            Assert.That(_asked, Has.Count.EqualTo(2));
            Assert.That(_asked[1].TitleKey, Is.EqualTo("unsaved.title"));
            Assert.That(_log, Is.EqualTo(new[] { "stop", "leave" }), "Nothing to save into.");
        }

        [Test]
        public void DecliningToLeaveUnsavedStays()
        {
            _slot = null;
            _answers.Enqueue(true);
            _answers.Enqueue(false);

            Assert.That(Run(Flow().QuitToMenuAsync(Leave)), Is.False);

            Assert.That(_log, Is.EqualTo(new[] { "stop", "start" }));
        }

        [Test]
        public void ASwitchThatDoesNotStartStaysAndPlaytimeCountsAgain()
        {
            _answers.Enqueue(true);
            _switchStarts = false;

            Assert.That(Run(Flow().QuitToMenuAsync(Leave)), Is.False);

            Assert.That(_log, Is.EqualTo(new[] { "stop", "save", "leave", "start" }));
        }

        [Test]
        public void WithoutADialogQuittingProceedsButNeverLeavesUnsaved()
        {
            var notices = new List<string>();
            var flow = new GameplayExitFlow(_checkpoint, _playtime, () => _slot, null, notices.Add, Keys());
            Assert.That(Run(flow.QuitToMenuAsync(Leave)), Is.True);
            Assert.That(_log, Is.EqualTo(new[] { "stop", "save", "leave" }));

            _log.Clear();
            _slot = null;
            Assert.That(Run(flow.QuitToMenuAsync(Leave)), Is.False);
            Assert.That(_log, Is.EqualTo(new[] { "stop", "start" }));
            Assert.That(notices, Is.EqualTo(new[] { "no-slot" }), "The player is told why the game stays.");
        }

        [Test]
        public void OneActionRunsAtATime()
        {
            _pendingAnswer = new AwaitableCompletionSource<bool>();
            GameplayExitFlow flow = Flow();
            Awaitable<bool> first = flow.QuitToMenuAsync(Leave);

            Assert.That(flow.IsBusy, Is.True);
            Assert.That(Run(flow.QuitToDesktopAsync(() => _log.Add("quit"))), Is.False);
            Assert.That(flow.SaveNow(), Is.False);

            _pendingAnswer.SetResult(false);
            Assert.That(Run(first), Is.False);
            Assert.That(flow.IsBusy, Is.False);
            Assert.That(_log, Is.Empty);
        }

        [Test]
        public void SaveReportsSavedFailedOrNoSlot()
        {
            var notices = new List<string>();
            GameplayExitFlow flow = Flow(notices.Add);

            Assert.That(flow.SaveNow(), Is.True);
            _checkpoint.Result = false;
            Assert.That(flow.SaveNow(), Is.False);
            _slot = null;
            Assert.That(flow.SaveNow(), Is.False);

            Assert.That(notices, Is.EqualTo(new[] { "saved", "failed", "no-slot" }));
            Assert.That(_checkpoint.Saves, Is.EqualTo(2), "No save is attempted without a slot.");
        }

        private GameplayExitFlow Flow(Action<string> notify = null)
        {
            return new GameplayExitFlow(_checkpoint, _playtime, () => _slot, Confirm, notify, Keys());
        }

        private Awaitable<bool> Leave()
        {
            _log.Add("leave");
            return Completed(_switchStarts);
        }

        private Awaitable<bool> Confirm(ConfirmRequest request)
        {
            _asked.Add(request);
            if (_pendingAnswer != null)
            {
                return _pendingAnswer.Awaitable;
            }

            return Completed(_answers.Count > 0 && _answers.Dequeue());
        }

        private static GameplayExitKeys Keys()
        {
            return new GameplayExitKeys
            {
                QuitToMenuTitle = "menu.title", QuitToMenuMessage = "menu.message", QuitToMenuConfirm = "menu.confirm",
                QuitToDesktopTitle = "desktop.title", QuitToDesktopMessage = "desktop.message", QuitToDesktopConfirm = "desktop.confirm",
                UnsavedTitle = "unsaved.title", UnsavedMessage = "unsaved.message", UnsavedConfirm = "unsaved.confirm",
                Cancel = "cancel", Saved = "saved", SaveFailed = "failed", NoSlot = "no-slot",
            };
        }

        private static Awaitable<T> Completed<T>(T value)
        {
            var source = new AwaitableCompletionSource<T>();
            source.SetResult(value);
            return source.Awaitable;
        }

        private static bool Run(Awaitable<bool> action)
        {
            Awaitable<bool>.Awaiter awaiter = action.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True, "The action did not finish.");
            return awaiter.GetResult();
        }

        private sealed class FakeCheckpoint : ICheckpoint
        {
            private readonly List<string> _log;

            internal FakeCheckpoint(List<string> log)
            {
                _log = log;
            }

            internal bool Result { get; set; }

            internal int Saves { get; private set; }

            public Awaitable<bool> SaveCheckpointAsync(string labelKey)
            {
                _log.Add("checkpoint");
                return Completed(Result);
            }

            public bool Save()
            {
                Saves++;
                _log.Add("save");
                return Result;
            }
        }

        private sealed class FakePlaytime : IPlaytime
        {
            private readonly List<string> _log;

            internal FakePlaytime(List<string> log)
            {
                _log = log;
            }

            public double Seconds => 0d;

            public bool IsCounting { get; private set; }

            public void StartCounting()
            {
                IsCounting = true;
                _log.Add("start");
            }

            public void StopCounting()
            {
                IsCounting = false;
                _log.Add("stop");
            }
        }
    }
}
