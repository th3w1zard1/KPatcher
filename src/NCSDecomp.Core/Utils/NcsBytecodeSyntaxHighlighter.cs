// Copyright (c) 2021-2025 DeNCS contributors / KPatcher
// Ported from DeNCS BytecodeSyntaxHighlighter.java (DeNCS: MIT; KPatcher distribution LGPL).

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// Highlight kinds for NCS decoder token streams (DeNCS <c>BytecodeSyntaxHighlighter</c>).
    /// </summary>
    public enum NcsBytecodeHighlightKind
    {
        Default = 0,
        Instruction,
        Address,
        HexValue,
        Function,
        TypeIndicator
    }

    /// <summary>
    /// A contiguous slice of decoder output with one highlight kind.
    /// </summary>
    public readonly struct NcsBytecodeHighlightedSegment
    {
        public NcsBytecodeHighlightedSegment(string text, NcsBytecodeHighlightKind kind)
        {
            Text = text ?? string.Empty;
            Kind = kind;
        }

        public string Text { get; }
        public NcsBytecodeHighlightKind Kind { get; }
    }

    /// <summary>
    /// Segments decoder text (e.g. <see cref="NcsParsePipeline.DecodeToTokenStream"/>) for UI coloring.
    /// </summary>
    public static class NcsBytecodeSyntaxHighlighter
    {
        public const int MaxHighlightSize = 500000;

        private static readonly string[] Instructions =
        {
            "CPDOWNSP", "RSADD", "RSADDI", "CPTOPSP", "CONST", "CONSTI", "CONSTF", "CONSTS", "ACTION",
            "LOGANDII", "LOGORII", "INCORII", "EXCORII", "BOOLANDII",
            "EQUAL", "NEQUAL", "GEQ", "GT", "LT", "LEQ",
            "SHLEFTII", "SHRIGHTII", "USHRIGHTII",
            "ADD", "SUB", "MUL", "DIV", "MOD", "NEG", "COMP",
            "MOVSP", "STATEALL", "JMP", "JSR", "JZ", "JNZ", "RETN", "DESTRUCT",
            "NOT", "DECISP", "INCISP", "CPDOWNBP", "CPTOPBP", "DECIBP", "INCIBP",
            "SAVEBP", "RESTOREBP", "STORE_STATE", "NOP", "T"
        };

        private static readonly string InstructionPattern = "\\b(" + string.Join("|", Array.ConvertAll(Instructions, Regex.Escape)) + ")\\b";

        private static readonly Regex PatternFunction = new Regex(@"\bfn_[0-9A-Fa-f]+\b", RegexOptions.Compiled);
        private static readonly Regex PatternType = new Regex(@"\bT\s+[0-9A-Fa-f]+\b", RegexOptions.Compiled);
        private static readonly Regex PatternInstruction = new Regex(InstructionPattern, RegexOptions.Compiled);
        private static readonly Regex PatternAddress = new Regex(@"\b[0-9A-Fa-f]{8}\b", RegexOptions.Compiled);
        private static readonly Regex PatternHexValue = new Regex(@"\b[0-9A-Fa-f]{4,}\b", RegexOptions.Compiled);

        /// <summary>
        /// Split <paramref name="text"/> into segments. Returns a single Default segment if too large or empty.
        /// </summary>
        public static IReadOnlyList<NcsBytecodeHighlightedSegment> Segment(string text, Action<string> logDebug = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new NcsBytecodeHighlightedSegment[0];
            }

            if (text.Length > MaxHighlightSize)
            {
                logDebug?.Invoke("NcsBytecodeSyntaxHighlighter: file too large for highlighting (" + text.Length + " chars), skipping.");
                return new[] { new NcsBytecodeHighlightedSegment(text, NcsBytecodeHighlightKind.Default) };
            }

            try
            {
                return SegmentInternal(text, logDebug);
            }
            catch (Exception ex)
            {
                logDebug?.Invoke("NcsBytecodeSyntaxHighlighter: error during highlighting: " + ex.GetType().Name + " — " + ex.Message);
                return new[] { new NcsBytecodeHighlightedSegment(text, NcsBytecodeHighlightKind.Default) };
            }
        }

        private static IReadOnlyList<NcsBytecodeHighlightedSegment> SegmentInternal(string text, Action<string> logDebug)
        {
            var slots = new NcsBytecodeHighlightKind[text.Length];

            TryApply(text, slots, PatternFunction, NcsBytecodeHighlightKind.Function, "functions", logDebug);
            TryApply(text, slots, PatternType, NcsBytecodeHighlightKind.TypeIndicator, "type indicators", logDebug);
            TryApply(text, slots, PatternInstruction, NcsBytecodeHighlightKind.Instruction, "instructions", logDebug);
            TryApply(text, slots, PatternAddress, NcsBytecodeHighlightKind.Address, "addresses", logDebug);
            TryApply(text, slots, PatternHexValue, NcsBytecodeHighlightKind.HexValue, "hex values", logDebug);

            return MergeSegments(text, slots);
        }

        private static void TryApply(string text, NcsBytecodeHighlightKind[] slots, Regex pattern, NcsBytecodeHighlightKind kind, string label, Action<string> logDebug)
        {
            try
            {
                ApplyPattern(text, slots, pattern, kind);
            }
            catch (Exception ex)
            {
                logDebug?.Invoke("NcsBytecodeSyntaxHighlighter: failed " + label + ": " + ex.Message);
            }
        }

        private static void ApplyPattern(string text, NcsBytecodeHighlightKind[] slots, Regex pattern, NcsBytecodeHighlightKind kind)
        {
            foreach (Match m in pattern.Matches(text))
            {
                int start = m.Index;
                int end = m.Index + m.Length;
                if (start < 0 || end > text.Length)
                {
                    continue;
                }

                bool overlap = false;
                for (int i = start; i < end; i++)
                {
                    if (slots[i] != NcsBytecodeHighlightKind.Default)
                    {
                        overlap = true;
                        break;
                    }
                }

                if (!overlap)
                {
                    for (int i = start; i < end; i++)
                    {
                        slots[i] = kind;
                    }
                }
            }
        }

        private static IReadOnlyList<NcsBytecodeHighlightedSegment> MergeSegments(string text, NcsBytecodeHighlightKind[] slots)
        {
            var list = new List<NcsBytecodeHighlightedSegment>(Math.Min(256, text.Length / 8 + 1));
            int i = 0;
            while (i < text.Length)
            {
                NcsBytecodeHighlightKind k = slots[i];
                int j = i + 1;
                while (j < text.Length && slots[j] == k)
                {
                    j++;
                }

                list.Add(new NcsBytecodeHighlightedSegment(text.Substring(i, j - i), k));
                i = j;
            }

            return list;
        }
    }
}
