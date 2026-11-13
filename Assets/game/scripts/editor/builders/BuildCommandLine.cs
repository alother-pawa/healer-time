using System;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Options of <c>BuildPipelineMenu.BuildFromCommandLine</c> read from the editor's command line: <c>-release</c> or
    /// <c>-development</c> (exactly one), optional <c>-output &lt;dir&gt;</c>. Unity's own arguments (<c>-buildTarget</c>,
    /// <c>-projectPath</c>…) are ignored here. Pure, tested without Unity.
    /// </summary>
    internal sealed class BuildCommandLine
    {
        private BuildCommandLine(bool development, string outputDirectory, string error)
        {
            Development = development;
            OutputDirectory = outputDirectory;
            Error = error;
        }

        /// <summary>True for a development build.</summary>
        internal bool Development { get; }

        /// <summary>The <c>-output</c> folder, or null for the default <c>Builds/&lt;platform&gt;/&lt;config&gt;</c>.</summary>
        internal string OutputDirectory { get; }

        /// <summary>Why the arguments are invalid; null when they are valid.</summary>
        internal string Error { get; }

        internal static BuildCommandLine Parse(string[] args)
        {
            bool release = false;
            bool development = false;
            string output = null;
            args = args ?? Array.Empty<string>();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (string.Equals(arg, "-release", StringComparison.OrdinalIgnoreCase))
                {
                    release = true;
                }
                else if (string.Equals(arg, "-development", StringComparison.OrdinalIgnoreCase))
                {
                    development = true;
                }
                else if (string.Equals(arg, "-output", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("-", StringComparison.Ordinal))
                    {
                        return new BuildCommandLine(false, null, "-output needs a folder.");
                    }

                    output = args[++i];
                }
            }

            if (release == development)
            {
                return new BuildCommandLine(false, null, "Pass exactly one of -release or -development.");
            }

            return new BuildCommandLine(development, output, null);
        }
    }
}
