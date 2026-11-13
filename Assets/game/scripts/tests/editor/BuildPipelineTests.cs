using System;
using System.IO;
using Game.Editor.Builders;
using NUnit.Framework;
using Paowa.Core.Foundation.Bootstrap;

namespace Game.Tests.Editor
{
    /// <summary>Build stamp text (read back by Core's parser), build number, command line, and stamp write/restore on files.</summary>
    public sealed class BuildPipelineTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "pc-base-build-tests", Guid.NewGuid().ToString("N"));
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
        public void StampText_IsReadBackByCore()
        {
            string text = BuildInfoFile.Compose("128", "3f2a9c1", new DateTime(2026, 10, 20, 8, 15, 0, DateTimeKind.Utc));

            BuildInfo info = BuildInfo.Parse("Game", "1.2.0", text);

            Assert.That(info.BuildNumber, Is.EqualTo("128"));
            Assert.That(info.Commit, Is.EqualTo("3f2a9c1"));
            Assert.That(info.BuildDate, Is.EqualTo("2026-10-20T08:15:00Z"));
            Assert.That(info.LogLine, Is.EqualTo("Game v1.2.0 (build 128, commit 3f2a9c1)"));
        }

        [Test]
        public void StampText_LeavesOutUnknownValues_AndKeepsOneLinePerValue()
        {
            string text = BuildInfoFile.Compose("7\nbuild=99", "", new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));

            BuildInfo info = BuildInfo.Parse("Game", "1.0", text);

            Assert.That(text, Does.Not.Contain("commit="));
            Assert.That(info.BuildNumber, Is.EqualTo("7 build=99"));
            Assert.That(info.DisplayText, Is.EqualTo("v1.0 (build 7 build=99)"));
        }

        [Test]
        public void BuildNumber_FromEnvironment_ElseLocalCounter()
        {
            Assert.That(BuildInfoFile.ResolveBuildNumber(" 42 ", "5", out string unused), Is.EqualTo("42"));
            Assert.That(unused, Is.Null);
            Assert.That(BuildInfoFile.ResolveBuildNumber(null, "5", out string next), Is.EqualTo("6"));
            Assert.That(next, Is.EqualTo("6"));
            Assert.That(BuildInfoFile.ResolveBuildNumber("", null, out _), Is.EqualTo("1"));
            Assert.That(BuildInfoFile.NextCounter("garbage"), Is.EqualTo(1));
            Assert.That(BuildInfoFile.NextCounter("-3"), Is.EqualTo(1));
            Assert.That(BuildInfoFile.NextCounter(int.MaxValue.ToString()), Is.EqualTo(1));
        }

        [Test]
        public void CommandLine_NeedsExactlyOneConfiguration()
        {
            BuildCommandLine release = BuildCommandLine.Parse(new[] { "Unity.exe", "-buildTarget", "Win64", "-release", "-output", "out/win" });
            Assert.That(release.Error, Is.Null);
            Assert.That(release.Development, Is.False);
            Assert.That(release.OutputDirectory, Is.EqualTo("out/win"));

            BuildCommandLine development = BuildCommandLine.Parse(new[] { "-DEVELOPMENT" });
            Assert.That(development.Error, Is.Null);
            Assert.That(development.Development, Is.True);
            Assert.That(development.OutputDirectory, Is.Null);

            Assert.That(BuildCommandLine.Parse(new[] { "-release", "-development" }).Error, Is.Not.Null);
            Assert.That(BuildCommandLine.Parse(new string[0]).Error, Is.Not.Null);
            Assert.That(BuildCommandLine.Parse(new[] { "-release", "-output" }).Error, Is.Not.Null);
            Assert.That(BuildCommandLine.Parse(new[] { "-release", "-output", "-quit" }).Error, Is.Not.Null);
            Assert.That(BuildCommandLine.Parse(null).Error, Is.Not.Null);
        }

        [Test]
        public void Stamp_NewFileIsDeletedWithItsMeta()
        {
            string path = Path.Combine(_root, "Resources", "Paowa", "BuildInfo.txt");

            BuildInfoStamp stamp = BuildInfoStamp.Write(path, "build=1\n");
            Assert.That(File.ReadAllText(path), Is.EqualTo("build=1\n"));
            File.WriteAllText(path + ".meta", "guid");

            stamp.Restore();
            stamp.Restore();

            Assert.That(File.Exists(path), Is.False);
            Assert.That(File.Exists(path + ".meta"), Is.False);
        }

        [Test]
        public void Stamp_ExistingFileGetsItsTextBack()
        {
            string path = Path.Combine(_root, "BuildInfo.txt");
            Directory.CreateDirectory(_root);
            File.WriteAllText(path, "build=hand\n");
            File.WriteAllText(path + ".meta", "guid");

            BuildInfoStamp stamp = BuildInfoStamp.Write(path, "build=2\n");
            Assert.That(File.ReadAllText(path), Is.EqualTo("build=2\n"));
            stamp.Restore();

            Assert.That(File.ReadAllText(path), Is.EqualTo("build=hand\n"));
            Assert.That(File.Exists(path + ".meta"), Is.True);
        }
    }
}
