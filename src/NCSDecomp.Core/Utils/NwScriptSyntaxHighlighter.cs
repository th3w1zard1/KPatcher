// Copyright (c) 2021-2025 DeNCS contributors / KPatcher
// Ported from DeNCS NWScriptSyntaxHighlighter.java (DeNCS: MIT; KPatcher distribution LGPL).

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using KCompiler.Diagnostics;
using Microsoft.Extensions.Logging;

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// NWScript syntax segmentation for UI highlighting (keywords, types, strings, comments, numbers, calls).
    /// Mirrors DeNCS <c>NWScriptSyntaxHighlighter</c> ordering and regex behavior.
    /// </summary>
    public enum NwScriptHighlightKind
    {
        Default = 0,
        Comment,
        String,
        Number,
        Keyword,
        Type,
        Function
    }

    /// <summary>
    /// A contiguous slice of source text with one highlight kind.
    /// </summary>
    public readonly struct NwScriptHighlightedSegment
    {
        public NwScriptHighlightedSegment(string text, NwScriptHighlightKind kind)
        {
            Text = text ?? string.Empty;
            Kind = kind;
        }

        public string Text { get; }
        public NwScriptHighlightKind Kind { get; }
    }

    /// <summary>
    /// Builds highlight segments from NSS/NWScript source without UI dependencies.
    /// </summary>
    public static class NwScriptSyntaxHighlighter
    {
        /// <summary>Same cap as Java to avoid regex cost on huge buffers.</summary>
        public const int MaxHighlightSize = 500000;

        private static readonly string[] Keywords =
        {
            "if", "else", "while", "for", "do", "switch", "case", "default", "break", "continue",
            "return", "void", "int", "float", "string", "object", "vector", "location", "effect",
            "event", "talent", "action", "const", "struct"
        };

        private static readonly string[] Types =
        {
            "int", "float", "string", "object", "vector", "location", "effect", "event", "talent",
            "action", "void"
        };

        private static readonly HashSet<string> KeywordSet = BuildKeywordSet();

        private static readonly string KeywordPattern = "\\b(" + string.Join("|", Array.ConvertAll(Keywords, Regex.Escape)) + ")\\b";
        private static readonly string TypePattern = "\\b(" + string.Join("|", Array.ConvertAll(Types, Regex.Escape)) + ")\\b";

        // Java DeNCS used possessive *+ / \s*+; .NET rejects nested quantifiers like *+ — use non-backtracking * or atomic groups.
        private static readonly Regex PatternCommentMulti = new Regex(@"/\*(?:[^*]|\*(?!/))*\*/", RegexOptions.Compiled);
        private static readonly Regex PatternCommentSingle = new Regex("//.*$", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex PatternStringDouble = new Regex(@"""(?:[^""\\]|\\.)*""", RegexOptions.Compiled);
        private static readonly Regex PatternStringSingle = new Regex(@"'(?:[^'\\]|\\.)*'", RegexOptions.Compiled);
        private static readonly Regex PatternNumber = new Regex(@"\b\d+\.?\d*[fF]?\b", RegexOptions.Compiled);
        private static readonly Regex PatternKeyword = new Regex(KeywordPattern, RegexOptions.Compiled);
        private static readonly Regex PatternType = new Regex(TypePattern, RegexOptions.Compiled);
        private static readonly Regex PatternFunction = new Regex(@"\b([a-zA-Z_][a-zA-Z0-9_]*)(?>\s*)\(", RegexOptions.Compiled);

        private static HashSet<string> BuildKeywordSet()
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (string k in Keywords)
            {
                set.Add(k);
            }

            return set;
        }

        /// <summary>
        /// Split <paramref name="text"/> into segments with highlight kinds. Returns a single Default segment if too large or empty.
        /// </summary>
        /// <param name="text">Source NSS.</param>
        /// <param name="logDebug">Optional debug sink (e.g. stderr).</param>
        /// <param name="structuredLog">Optional MEL sink (<c>Phase=syntax_highlight.nss</c>).</param>
        public static IReadOnlyList<NwScriptHighlightedSegment> Segment(
            string text,
            Action<string> logDebug = null,
            ILogger structuredLog = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new NwScriptHighlightedSegment[0];
            }

            if (text.Length > MaxHighlightSize)
            {
                logDebug?.Invoke("NWScriptSyntaxHighlighter: file too large for highlighting (" + text.Length + " chars), skipping.");
                if (structuredLog != null && structuredLog.IsEnabled(LogLevel.Debug))
                {
                    structuredLog.LogDebug(
                        "Tool=NCSDecomp Phase=syntax_highlight.nss Result=skipped_too_large CharCount={Count} CorrelationId={CorrelationId}",
                        text.Length,
                        ToolCorrelation.ReadOptional() ?? "");
                }

                return new[] { new NwScriptHighlightedSegment(text, NwScriptHighlightKind.Default) };
            }

            try
            {
                return SegmentInternal(text, logDebug);
            }
            catch (Exception ex)
            {
                logDebug?.Invoke("NWScriptSyntaxHighlighter: error during highlighting: " + ex.GetType().Name + " — " + ex.Message);
                if (structuredLog != null && structuredLog.IsEnabled(LogLevel.Debug))
                {
                    structuredLog.LogDebug(
                        ex,
                        "Tool=NCSDecomp Phase=syntax_highlight.nss Result=error CharCount={Count} CorrelationId={CorrelationId}",
                        text.Length,
                        ToolCorrelation.ReadOptional() ?? "");
                }

                return new[] { new NwScriptHighlightedSegment(text, NwScriptHighlightKind.Default) };
            }
        }

        private static IReadOnlyList<NwScriptHighlightedSegment> SegmentInternal(string text, Action<string> logDebug)
        {
            var slots = new NwScriptHighlightKind[text.Length];
            // slots default Default

            TryApply(text, slots, PatternCommentMulti, NwScriptHighlightKind.Comment, "multi-line comments", logDebug);
            TryApply(text, slots, PatternCommentSingle, NwScriptHighlightKind.Comment, "single-line comments", logDebug);
            TryApply(text, slots, PatternStringDouble, NwScriptHighlightKind.String, "double-quoted strings", logDebug);
            TryApply(text, slots, PatternStringSingle, NwScriptHighlightKind.String, "single-quoted strings", logDebug);
            TryApply(text, slots, PatternNumber, NwScriptHighlightKind.Number, "numbers", logDebug);
            TryApply(text, slots, PatternKeyword, NwScriptHighlightKind.Keyword, "keywords", logDebug);
            TryApply(text, slots, PatternType, NwScriptHighlightKind.Type, "types", logDebug);
            TryApplyFunctions(text, slots, logDebug);

            return MergeSegments(text, slots);
        }

        private static void TryApply(string text, NwScriptHighlightKind[] slots, Regex pattern, NwScriptHighlightKind kind, string label, Action<string> logDebug)
        {
            try
            {
                ApplyPattern(text, slots, pattern, kind);
            }
            catch (Exception ex)
            {
                logDebug?.Invoke("NWScriptSyntaxHighlighter: failed " + label + ": " + ex.Message);
            }
        }

        private static void ApplyPattern(string text, NwScriptHighlightKind[] slots, Regex pattern, NwScriptHighlightKind kind)
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
                    if (slots[i] != NwScriptHighlightKind.Default)
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

        private static void TryApplyFunctions(string text, NwScriptHighlightKind[] slots, Action<string> logDebug)
        {
            try
            {
                foreach (Match m in PatternFunction.Matches(text))
                {
                    Group g = m.Groups[1];
                    if (!g.Success)
                    {
                        continue;
                    }

                    int start = g.Index;
                    int end = start + g.Length;
                    if (start < 0 || end > text.Length)
                    {
                        continue;
                    }

                    bool overlap = false;
                    for (int i = start; i < end; i++)
                    {
                        if (slots[i] != NwScriptHighlightKind.Default)
                        {
                            overlap = true;
                            break;
                        }
                    }

                    if (overlap)
                    {
                        continue;
                    }

                    string funcName = text.Substring(start, end - start);
                    if (KeywordSet.Contains(funcName))
                    {
                        continue;
                    }

                    for (int i = start; i < end; i++)
                    {
                        slots[i] = NwScriptHighlightKind.Function;
                    }
                }
            }
            catch (Exception ex)
            {
                logDebug?.Invoke("NWScriptSyntaxHighlighter: failed function calls: " + ex.Message);
            }
        }

        private static IReadOnlyList<NwScriptHighlightedSegment> MergeSegments(string text, NwScriptHighlightKind[] slots)
        {
            var list = new List<NwScriptHighlightedSegment>(Math.Min(256, text.Length / 8 + 1));
            int i = 0;
            while (i < text.Length)
            {
                NwScriptHighlightKind k = slots[i];
                int j = i + 1;
                while (j < text.Length && slots[j] == k)
                {
                    j++;
                }

                list.Add(new NwScriptHighlightedSegment(text.Substring(i, j - i), k));
                i = j;
            }

            return list;
        }
    }
}
