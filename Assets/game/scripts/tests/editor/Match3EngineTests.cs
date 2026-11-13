using System.Collections.Generic;
using System.Linq;
using Game.Systems.Gameplay;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    internal sealed class Match3EngineTests
    {
        private static readonly float[] FourKinds = { 1f, 1f, 1f, 1f };

        [Test]
        public void FindsHorizontalAndVerticalRuns()
        {
            Board board = Board.Parse(
                "0123",
                "0231",
                "0312",
                "1112");
            List<GemGroup> matches = MatchFinder.Find(board);
            Assert.That(matches.Count, Is.EqualTo(2));
            Assert.That(matches.Select(m => m.Kind), Is.EquivalentTo(new[] { 1, 0 }));
            Assert.That(matches.All(m => m.Count == 3));
        }

        [Test]
        public void CrossingRunsOfOneKindAreOneGroup()
        {
            Board board = Board.Parse(
                "1023",
                "1203",
                "0003",
                "1230");
            List<GemGroup> matches = MatchFinder.Find(board);
            GemGroup zeros = matches.Single(m => m.Kind == 0);
            Assert.That(zeros.Count, Is.EqualTo(3));
            GemGroup threes = matches.Single(m => m.Kind == 3);
            Assert.That(threes.Count, Is.EqualTo(3));

            Board ell = Board.Parse(
                "2013",
                "2023",
                "0002",
                "1231");
            GemGroup l = MatchFinder.Find(ell).Single(m => m.Kind == 0);
            Assert.That(l.Count, Is.EqualTo(5), "an L of five is one group");
            Assert.That(l.LongestRun, Is.EqualTo(3));
        }

        [Test]
        public void ASwapWithoutAMatchChangesNothing()
        {
            Board board = Board.Parse(
                "0123",
                "1230",
                "2301",
                "3012");
            string before = board.ToString();
            var engine = new Match3Engine(board, new GemSpawner(FourKinds, 1));
            BoardResolution result = engine.TrySwap(new Cell(0, 0), new Cell(0, 1));
            Assert.That(result.Valid, Is.False);
            Assert.That(engine.Board.ToString(), Is.EqualTo(before));
        }

        [Test]
        public void SwapsThatAreNotNeighboursAreRefused()
        {
            var engine = new Match3Engine(Board.Parse("0123", "1230", "2301", "3012"), new GemSpawner(FourKinds, 1));
            Assert.That(engine.TrySwap(new Cell(0, 0), new Cell(1, 1)).Valid, Is.False);
            Assert.That(engine.TrySwap(new Cell(0, 0), new Cell(0, 9)).Valid, Is.False);
        }

        [Test]
        public void AValidSwapClearsFallsAndRefillsWithoutEmptyCells()
        {
            // Swapping the last two gems of the bottom row makes 0 0 0.
            Board board = Board.Parse(
                "1231",
                "2312",
                "3103",
                "0010");
            var engine = new Match3Engine(board, new GemSpawner(FourKinds, 7));
            BoardResolution result = engine.TrySwap(new Cell(0, 2), new Cell(0, 3));
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Steps.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(result.Steps[0].Cleared.Any(m => m.Kind == 0 && m.Count >= 3));
            for (int row = 0; row < engine.Board.Rows; row++)
            {
                for (int column = 0; column < engine.Board.Columns; column++)
                {
                    Assert.That(engine.Board[row, column], Is.Not.EqualTo(Board.Empty));
                }
            }

            Assert.That(MatchFinder.Find(engine.Board), Is.Empty, "the board settles with no match left");
            Assert.That(MoveFinder.HasMove(engine.Board), Is.True, "the board always ends playable");
        }

        [Test]
        public void NewGemsDropFromAboveTheBoardIntoTheHoles()
        {
            Board board = Board.Parse(
                "1231",
                "2312",
                "3103",
                "0010");
            var engine = new Match3Engine(board, new GemSpawner(FourKinds, 3));
            BoardResolution result = engine.TrySwap(new Cell(0, 2), new Cell(0, 3));
            CascadeStep first = result.Steps[0];
            int cleared = first.Cleared.Sum(m => m.Count);
            List<GemMove> drops = first.Moves.Where(m => m.IsNew).ToList();
            Assert.That(drops.Count, Is.EqualTo(cleared));
            Assert.That(drops.All(m => m.From.Row >= board.Rows && m.From.Column == m.To.Column));
            Assert.That(first.Moves.Where(m => !m.IsNew).All(m => m.From.Row > m.To.Row && m.From.Column == m.To.Column));
        }

        [Test]
        public void TheSameSeedGivesTheSameBoardAndCascade()
        {
            var a = new Match3Engine(8, 8, new GemSpawner(FourKinds, 42));
            var b = new Match3Engine(8, 8, new GemSpawner(FourKinds, 42));
            Assert.That(a.Board.ToString(), Is.EqualTo(b.Board.ToString()));
            Assert.That(MoveFinder.TryFind(a.Board, out Cell x, out Cell y), Is.True);
            a.TrySwap(x, y);
            b.TrySwap(x, y);
            Assert.That(a.Board.ToString(), Is.EqualTo(b.Board.ToString()));
        }

        [Test]
        public void ANewBoardHasNoMatchAndAMove()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                var engine = new Match3Engine(8, 8, new GemSpawner(FourKinds, seed));
                Assert.That(MatchFinder.Find(engine.Board), Is.Empty, $"seed {seed}");
                Assert.That(MoveFinder.HasMove(engine.Board), Is.True, $"seed {seed}");
            }
        }

        [Test]
        public void ABoardWithoutMovesIsShuffledAfterTheMove()
        {
            // Three kinds in a pattern with one forced match; after it settles the spawner decides, so only check the outcome.
            for (int seed = 0; seed < 20; seed++)
            {
                var engine = new Match3Engine(6, 6, new GemSpawner(new[] { 1f, 1f, 1f }, seed));
                for (int move = 0; move < 15; move++)
                {
                    Assert.That(MoveFinder.TryFind(engine.Board, out Cell a, out Cell b), Is.True);
                    BoardResolution result = engine.TrySwap(a, b);
                    Assert.That(result.Valid, Is.True);
                    Assert.That(MatchFinder.Find(engine.Board), Is.Empty);
                    Assert.That(MoveFinder.HasMove(engine.Board), Is.True);
                    if (result.Reshuffled && !result.Regenerated)
                    {
                        Assert.That(result.Shuffle.All(m => engine.Board[m.To] == m.Kind));
                    }
                }
            }
        }

        [Test]
        public void ABlastClearsTheSquareGroupedByKind()
        {
            var engine = new Match3Engine(8, 8, new GemSpawner(FourKinds, 5));
            BoardResolution result = engine.Blast(new Cell(0, 0), 1);
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Steps[0].Cleared.Sum(m => m.Count), Is.EqualTo(4), "a corner blast of radius 1 holds 2×2 gems");
            Assert.That(result.Steps[0].Cleared.Select(m => m.Kind).Distinct().Count(), Is.EqualTo(result.Steps[0].Cleared.Count));

            BoardResolution middle = engine.Blast(new Cell(4, 4), 1);
            Assert.That(middle.Steps[0].Cleared.Sum(m => m.Count), Is.EqualTo(9));
            Assert.That(MatchFinder.Find(engine.Board), Is.Empty);
        }

        [Test]
        public void ShufflingKeepsTheSameGems()
        {
            var engine = new Match3Engine(8, 8, new GemSpawner(FourKinds, 11));
            string before = new string(engine.Board.ToString().Where(char.IsDigit).OrderBy(c => c).ToArray());
            BoardResolution result = engine.ShuffleNow();
            Assert.That(result.Reshuffled, Is.True);
            if (!result.Regenerated)
            {
                string after = new string(engine.Board.ToString().Where(char.IsDigit).OrderBy(c => c).ToArray());
                Assert.That(after, Is.EqualTo(before));
            }

            Assert.That(MatchFinder.Find(engine.Board), Is.Empty);
            Assert.That(MoveFinder.HasMove(engine.Board), Is.True);
        }

        [Test]
        public void SpawnerNeedsThreeKinds()
        {
            Assert.That(() => new GemSpawner(new[] { 1f, 1f, 0f }, 0), Throws.ArgumentException);
        }
    }
}
