using System.IO;
using System.Text;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Writes the build stamp file for one player build and puts things back afterwards: a file that existed before (a
    /// stamp left by hand for testing) gets its old text back; a file that did not exist is deleted with its <c>.meta</c>.
    /// File system only (the caller tells the AssetDatabase), so it is tested on a temporary folder.
    /// </summary>
    internal sealed class BuildInfoStamp
    {
        private readonly string _path;
        private readonly string _previousText;
        private bool _restored;

        private BuildInfoStamp(string path, string previousText)
        {
            _path = path;
            _previousText = previousText;
        }

        /// <summary>Writes <paramref name="text"/> (UTF-8 without BOM) at <paramref name="path"/>, remembering what was there.</summary>
        internal static BuildInfoStamp Write(string path, string text)
        {
            string previous = File.Exists(path) ? File.ReadAllText(path) : null;
            string folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var stamp = new BuildInfoStamp(path, previous);
            File.WriteAllText(path, text, new UTF8Encoding(false));
            return stamp;
        }

        /// <summary>Restores the earlier text, or deletes the file and its meta; later calls do nothing.</summary>
        internal void Restore()
        {
            if (_restored)
            {
                return;
            }

            _restored = true;
            if (_previousText != null)
            {
                File.WriteAllText(_path, _previousText, new UTF8Encoding(false));
                return;
            }

            if (File.Exists(_path))
            {
                File.Delete(_path);
            }

            string meta = _path + ".meta";
            if (File.Exists(meta))
            {
                File.Delete(meta);
            }
        }
    }
}
