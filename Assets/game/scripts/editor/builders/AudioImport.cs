using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Imports every WAV under <c>audio</c> from <c>audio/audio-spec.json</c> (written by <c>Tools/AudioGen</c> with the
    /// placeholders), with the settings of its category: music streams (Vorbis), ambience and voice stay compressed in
    /// memory (Vorbis), effects and UI sounds are decompressed on load (ADPCM, cheap to start many times); voice, effects
    /// and UI are forced to mono. A sound missing from the spec, or a spec entry without its file, stops the build.
    /// </summary>
    internal static class AudioImport
    {
        [Serializable]
        private sealed class AudioSpec
        {
            public SoundSpec[] sounds = Array.Empty<SoundSpec>();
        }

        [Serializable]
        private sealed class SoundSpec
        {
            public string path;
            public string category;
            public float seconds;
            public bool loop;
        }

        private readonly struct CategorySettings
        {
            internal CategorySettings(AudioClipLoadType loadType, AudioCompressionFormat format, float quality, bool mono)
            {
                LoadType = loadType;
                Format = format;
                Quality = quality;
                Mono = mono;
            }

            internal AudioClipLoadType LoadType { get; }
            internal AudioCompressionFormat Format { get; }
            internal float Quality { get; }
            internal bool Mono { get; }
        }

        private static CategorySettings SettingsOf(string category)
        {
            switch (category)
            {
                case "music":
                    return new CategorySettings(AudioClipLoadType.Streaming, AudioCompressionFormat.Vorbis, 0.6f, false);
                case "ambience":
                    return new CategorySettings(AudioClipLoadType.CompressedInMemory, AudioCompressionFormat.Vorbis, 0.5f, false);
                case "voice":
                    return new CategorySettings(AudioClipLoadType.CompressedInMemory, AudioCompressionFormat.Vorbis, 0.5f, true);
                case "sfx":
                case "ui":
                    return new CategorySettings(AudioClipLoadType.DecompressOnLoad, AudioCompressionFormat.ADPCM, 1f, true);
                default:
                    throw new InvalidOperationException($"Unknown sound category '{category}' in {BuildPaths.AudioSpec}.");
            }
        }

        internal static void Import()
        {
            if (!File.Exists(BuildPaths.AudioSpec))
            {
                throw new InvalidOperationException($"{BuildPaths.AudioSpec} is missing. Run python Tools/AudioGen/generate_sounds.py.");
            }

            AudioSpec spec = JsonUtility.FromJson<AudioSpec>(File.ReadAllText(BuildPaths.AudioSpec));
            string prefix = BuildPaths.Audio + "/";
            string[] files = AssetDatabase.FindAssets("t:AudioClip", new[] { BuildPaths.Audio })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => path.Substring(prefix.Length, path.LastIndexOf('.') - prefix.Length))
                .ToArray();
            string[] listed = spec.sounds.Select(sound => sound.path).ToArray();
            string[] unlisted = files.Except(listed).ToArray();
            string[] missing = listed.Except(files).ToArray();
            if (unlisted.Length > 0 || missing.Length > 0)
            {
                throw new InvalidOperationException(
                    "audio and audio/audio-spec.json differ. Add the sound to Tools/AudioGen/generate_sounds.py and run it. "
                    + $"Not in the spec: {string.Join(", ", unlisted)}. Missing files: {string.Join(", ", missing)}.");
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (SoundSpec sound in spec.sounds)
                {
                    Configure($"{prefix}{sound.path}.wav", SettingsOf(sound.category));
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        private static void Configure(string path, CategorySettings wanted)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"{path} is not an audio file (sounds are delivered as .wav).");
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            bool unchanged = settings.loadType == wanted.LoadType && settings.compressionFormat == wanted.Format
                             && Mathf.Approximately(settings.quality, wanted.Quality)
                             && settings.sampleRateSetting == AudioSampleRateSetting.PreserveSampleRate
                             && importer.forceToMono == wanted.Mono;
            if (unchanged)
            {
                return;
            }

            settings.loadType = wanted.LoadType;
            settings.compressionFormat = wanted.Format;
            settings.quality = wanted.Quality;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = wanted.Mono;
            importer.SaveAndReimport();
        }
    }
}
