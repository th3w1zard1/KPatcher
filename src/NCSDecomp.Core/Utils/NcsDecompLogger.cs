// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.IO;

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// Structured console logging with optional ANSI colors (DeNCS Logger.java).
    /// Writes to <see cref="TextWriter"/> (default: <see cref="Console.Error"/>).
    /// </summary>
    public sealed class NcsDecompLogger
    {
        private readonly TextWriter _writer;
        private readonly bool _colorsEnabled;

        private const string Reset = "\u001b[0m";
        private const string AnsiBold = "\u001b[1m";
        private const string AnsiDim = "\u001b[2m";
        private const string Red = "\u001b[31m";
        private const string Green = "\u001b[32m";
        private const string Yellow = "\u001b[33m";
        private const string Blue = "\u001b[34m";
        private const string Cyan = "\u001b[36m";
        private const string White = "\u001b[37m";
        private const string BgBlue = "\u001b[44m";
        private const string BgCyan = "\u001b[46m";
        private const string BgYellow = "\u001b[43m";
        private const string BgRed = "\u001b[41m";

        private static readonly string DividerLine =
            "═══════════════════════════════════════════════════════════════════════════════";

        public NcsDecompLogger(TextWriter writer = null)
        {
            _writer = writer ?? Console.Error;
            _colorsEnabled = IsColorSupported();
        }

        /// <summary>Default stderr logger (shared instance).</summary>
        public static NcsDecompLogger Default { get; } = new NcsDecompLogger();

        public void Trace(string message)
        {
            WriteLine(Colorize("TRACE", AnsiDim + Cyan) + " " + DimText(message));
        }

        public void Debug(string message)
        {
            WriteLine(Colorize("DEBUG", Cyan) + " " + DimText(message));
        }

        public void Info(string message)
        {
            WriteLine(Colorize("INFO", Blue) + "  " + message);
        }

        public void Warn(string message)
        {
            WriteLine(Colorize("WARN", Yellow) + " " + message);
        }

        public void Error(string message)
        {
            WriteLine(Colorize("ERROR", Red) + " " + BoldText(message));
        }

        public void Success(string message)
        {
            WriteLine(Colorize("SUCCESS", Green) + " " + message);
        }

        public void NcsDecomp(string message)
        {
            WriteLine(Colorize("\u25b6", Green) + " " + Colorize("[NCSDecomp]", AnsiBold + Cyan) + " " + message);
        }

        public void CompilerLine(string line)
        {
            WriteLine("  " + Colorize("|", AnsiDim + Cyan) + " " + DimText(line));
        }

        public void StartControlFlowSection()
        {
            PrintDivider(Cyan);
            PrintSectionHeader("CONTROL FLOW DEBUG", BgCyan, White);
            PrintDivider(Cyan);
        }

        public void StartCompilerSection()
        {
            PrintDivider(Yellow);
            PrintSectionHeader("COMPILER OUTPUT", BgYellow, White);
            PrintDivider(Yellow);
        }

        public void StartNcsDecompSection()
        {
            PrintDivider(Green);
            PrintSectionHeader("NCSDECOMP OPERATIONS", BgBlue, White);
            PrintDivider(Green);
        }

        public void StartErrorSection()
        {
            PrintDivider(Red);
            PrintSectionHeader("ERRORS", BgRed, White);
            PrintDivider(Red);
        }

        public void EndSection()
        {
            WriteLine(string.Empty);
        }

        public void CompilerOutput(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return;
            }

            string[] lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length > 0 || i < lines.Length - 1)
                {
                    CompilerLine(lines[i]);
                }
            }
        }

        private void WriteLine(string line)
        {
            _writer.WriteLine(line);
        }

        private string Colorize(string text, string color)
        {
            if (_colorsEnabled)
            {
                return color + text + Reset;
            }

            return text;
        }

        private string BoldText(string text)
        {
            if (_colorsEnabled)
            {
                return AnsiBold + text + Reset;
            }

            return text;
        }

        private string DimText(string text)
        {
            if (_colorsEnabled)
            {
                return AnsiDim + text + Reset;
            }

            return text;
        }

        private void PrintDivider(string color)
        {
            WriteLine(Colorize(DividerLine, color));
        }

        private void PrintSectionHeader(string title, string bgColor, string fgColor)
        {
            int titleLen = title.Length;
            int paddingLen = Math.Max(0, (DividerLine.Length - titleLen - 4) / 2);
            string leftPad = DividerLine.Substring(0, Math.Min(paddingLen, DividerLine.Length));
            int rightLen = Math.Max(0, DividerLine.Length - leftPad.Length - titleLen - 4);
            string rightPad = rightLen > 0 ? DividerLine.Substring(0, Math.Min(rightLen, DividerLine.Length)) : string.Empty;

            if (_colorsEnabled)
            {
                WriteLine(bgColor + fgColor + " " + leftPad + " " + BoldText(title) + " " + rightPad + " " + Reset);
            }
            else
            {
                WriteLine("=== " + title + " ===");
            }
        }

        private static bool IsColorSupported()
        {
            string term = Environment.GetEnvironmentVariable("TERM");
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                try
                {
                    if (Environment.OSVersion.Version.Major >= 10)
                    {
                        return true;
                    }
                }
                catch
                {
                    // fall through
                }
            }

            return !string.IsNullOrEmpty(term) &&
                   !string.Equals(term, "dumb", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(term, "unknown", StringComparison.OrdinalIgnoreCase);
        }
    }
}
