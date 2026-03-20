using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace KPatcher.Core.Utility.MiscString
{
    public class CaseInsensImmutableStr : WrappedStr
    {
        private readonly string _casefoldContent;

        private static string CoerceStr(object item)
        {
            if (item is WrappedStr wrapped)
            {
                return ((string)wrapped).ToLowerInvariant();
            }
            if (item is string str)
            {
                return str.ToLowerInvariant();
            }
            return item?.ToString();
        }

        public CaseInsensImmutableStr(string content) : base(content)
        {
            _casefoldContent = content?.ToLowerInvariant() ?? "";
        }

        public CaseInsensImmutableStr(WrappedStr content) : base(content)
        {
            _casefoldContent = content != null ? ((string)content).ToLowerInvariant() : "";
        }

        public bool Contains(object key)
        {
            return _casefoldContent.Contains(CoerceStr(key));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            return _casefoldContent.Equals(CoerceStr(obj));
        }

        public override int GetHashCode()
        {
            return _casefoldContent.GetHashCode();
        }

        public static bool operator !=(CaseInsensImmutableStr left, object right)
        {
            return !(left == right);
        }

        public static bool operator ==(CaseInsensImmutableStr left, object right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }
            return left.Equals(right);
        }

        // Already implemented in GetHashCode()

        public int Find(string sub, int start = 0, int? end = null)
        {
            string target = CoerceStr(sub);
            int endIndex = end ?? _casefoldContent.Length;
            return _casefoldContent.IndexOf(target, start, endIndex - start);
        }

        public Tuple<string, string, string> Partition(string sep)
        {
            string sepStr = CoerceStr(sep);
            Regex pattern = new Regex(Regex.Escape(sepStr), RegexOptions.IgnoreCase);
            Match match = pattern.Match(_content);

            if (!match.Success)
            {
                return Tuple.Create(_content, "", "");
            }

            int idx = match.Index;
            int sepLength = sep.Length;
            return Tuple.Create(
                _content.Substring(0, idx),
                _content.Substring(idx, sepLength),
                _content.Substring(idx + sepLength)
            );
        }

        public Tuple<string, string, string> RPartition(string sep)
        {
            string sepStr = CoerceStr(sep);
            Regex pattern = new Regex(Regex.Escape(sepStr), RegexOptions.IgnoreCase);
            MatchCollection matches = pattern.Matches(_content);

            if (matches.Count == 0)
            {
                return Tuple.Create("", "", _content);
            }

            Match match = matches[matches.Count - 1];
            int idx = match.Index;
            return Tuple.Create(
                _content.Substring(0, idx),
                _content.Substring(idx, sep.Length),
                _content.Substring(idx + sep.Length)
            );
        }

        public bool Endswith(object suffix, int? start = null, int? end = null)
        {
            if (suffix is IEnumerable<object> tuple)
            {
                var parsedSuffixArray = tuple.Select(CoerceStr).ToArray();
                int startIdx = start ?? 0;
                int endIdx = end ?? _casefoldContent.Length;
                return parsedSuffixArray.Any(s => _casefoldContent.Substring(startIdx, endIdx - startIdx).EndsWith(s));
            }
            string parsedSuffix = CoerceStr(suffix);
            int startIndex = start ?? 0;
            int endIndex = end ?? _casefoldContent.Length;
            return _casefoldContent.Substring(startIndex, endIndex - startIndex).EndsWith(parsedSuffix);
        }

        public int RFind(string sub, int? start = null, int? end = null)
        {
            int startIdx = start ?? 0;
            int endIdx = end ?? _casefoldContent.Length;
            string sliceText = _casefoldContent.Substring(startIdx, endIdx - startIdx);
            string target = CoerceStr(sub);
            int result = sliceText.LastIndexOf(target);
            return result == -1 ? -1 : (start ?? 0) + result;
        }

        public List<string> Split(string sep = null, int maxsplit = -1)
        {
            if (sep == null)
            {
                return _content.Split((char[])null, maxsplit == -1 ? int.MaxValue : maxsplit + 1, StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            string sepStr = sep;
            if (sepStr == "")
            {
                throw new ArgumentException("empty separator");
            }

            Regex pattern = new Regex(Regex.Escape(sepStr), RegexOptions.IgnoreCase);
            int maxsplitArg = maxsplit >= 0 ? maxsplit : 0;
            string[] parts = pattern.Split(_content, maxsplitArg + 1);
            return parts.ToList();
        }

        public int Count(object x, int? start = null, int? end = null)
        {
            string target = CoerceStr(x);
            int startIdx = start ?? 0;
            int endIdx = end ?? _casefoldContent.Length;
            string slice = _casefoldContent.Substring(startIdx, endIdx - startIdx);
            int count = 0;
            int index = 0;
            while ((index = slice.IndexOf(target, index)) != -1)
            {
                count++;
                index += target.Length;
            }
            return count;
        }

        public int Index(string sub, int? start = null, int? end = null)
        {
            int result = Find(sub, start ?? 0, end);
            if (result == -1)
            {
                throw new ArgumentException("substring not found");
            }
            return result;
        }

        public static bool operator <(CaseInsensImmutableStr left, object right)
        {
            return string.Compare(left?._casefoldContent, CoerceStr(right), StringComparison.OrdinalIgnoreCase) < 0;
        }

        public static bool operator >(CaseInsensImmutableStr left, object right)
        {
            return string.Compare(left?._casefoldContent, CoerceStr(right), StringComparison.OrdinalIgnoreCase) > 0;
        }

        public static bool operator <=(CaseInsensImmutableStr left, object right)
        {
            return string.Compare(left?._casefoldContent, CoerceStr(right), StringComparison.OrdinalIgnoreCase) <= 0;
        }

        public static bool operator >=(CaseInsensImmutableStr left, object right)
        {
            return string.Compare(left?._casefoldContent, CoerceStr(right), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public new CaseInsensImmutableStr RemovePrefix(string prefix)
        {
            string prefixStr = prefix;
            if (_content.StartsWith(prefixStr, StringComparison.OrdinalIgnoreCase))
            {
                return new CaseInsensImmutableStr(_content.Substring(prefixStr.Length));
            }
            return new CaseInsensImmutableStr(_content);
        }

        public new CaseInsensImmutableStr RemoveSuffix(string suffix)
        {
            string suffixStr = suffix;
            if (_content.EndsWith(suffixStr, StringComparison.OrdinalIgnoreCase))
            {
                return new CaseInsensImmutableStr(_content.Substring(0, _content.Length - suffixStr.Length));
            }
            return new CaseInsensImmutableStr(_content);
        }

        public CaseInsensImmutableStr Replace(string old, string new_, int count = -1)
        {
            if (old == "")
            {
                // C# String.Replace doesn't support count parameter, so we need to implement it manually
                if (count == -1 || count == int.MaxValue)
                {
                    return new CaseInsensImmutableStr(_content.Replace(old, new_));
                }
                else
                {
                    // Replace only the first 'count' occurrences
                    string result = _content;
                    int remaining = count;
                    int index = 0;
                    while (remaining > 0 && (index = result.IndexOf(old, index, StringComparison.Ordinal)) >= 0)
                    {
                        result = result.Substring(0, index) + new_ + result.Substring(index + old.Length);
                        index += new_.Length;
                        remaining--;
                    }
                    return new CaseInsensImmutableStr(result);
                }
            }

            string pattern = Regex.Escape(old);
            string repl = new_;

            MatchCollection matches = Regex.Matches(_content, pattern, RegexOptions.IgnoreCase);
            List<Match> matchList = matches.Cast<Match>().ToList();

            if (count >= 0)
            {
                matchList = matchList.Take(count).ToList();
            }

            string newContent = _content;
            foreach (Match match in matchList.OrderByDescending(m => m.Index))
            {
                string matched = match.Value;
                string replacement = repl;
                if (matched == matched.ToUpperInvariant())
                {
                    replacement = repl.ToUpperInvariant();
                }
                else if (matched == matched.ToLowerInvariant())
                {
                    replacement = repl.ToLowerInvariant();
                }
                else if (matched.Length > 0 && char.IsUpper(matched[0]))
                {
                    replacement = repl.Length > 0 ? char.ToUpperInvariant(repl[0]) + (repl.Length > 1 ? repl.Substring(1).ToLowerInvariant() : "") : repl;
                }

                int start = match.Index;
                int length = match.Length;
                newContent = newContent.Substring(0, start) + replacement + newContent.Substring(start + length);
            }

            return new CaseInsensImmutableStr(newContent);
        }

        public List<string> RSplit(string sep = null, int maxsplit = -1)
        {
            if (sep == null)
            {
                return _content.Split((char[])null, maxsplit == -1 ? int.MaxValue : maxsplit + 1, StringSplitOptions.RemoveEmptyEntries).Reverse().ToList();
            }

            string sepStr = sep;
            if (sepStr == "")
            {
                throw new ArgumentException("empty separator");
            }

            Regex pattern = new Regex(Regex.Escape(sepStr), RegexOptions.IgnoreCase);
            MatchCollection matches = pattern.Matches(_content);
            if (matches.Count == 0)
            {
                return new List<string> { _content };
            }

            List<Match> matchList = matches.Cast<Match>().ToList();
            if (maxsplit > 0)
            {
                matchList = matchList.Skip(Math.Max(0, matchList.Count - maxsplit)).ToList();
            }

            List<string> parts = new List<string>();
            int lastIndex = _content.Length;
            foreach (Match match in matchList.OrderByDescending(m => m.Index))
            {
                int start = match.Index;
                int end = start + match.Length;
                parts.Add(_content.Substring(end, lastIndex - end));
                lastIndex = start;
            }
            parts.Add(_content.Substring(0, lastIndex));
            parts.Reverse();
            return parts;
        }

        public int RIndex(string sub, int? start = null, int? end = null)
        {
            int value = RFind(sub, start, end);
            if (value == -1)
            {
                throw new ArgumentException("substring not found");
            }
            return value;
        }

        public bool Startswith(object prefix, int? start = null, int? end = null)
        {
            if (prefix is IEnumerable<object> tuple)
            {
                var parsedPrefixArray = tuple.Select(CoerceStr).ToArray();
                int startIdx = start ?? 0;
                int endIdx = end ?? _casefoldContent.Length;
                return parsedPrefixArray.Any(s => _casefoldContent.Substring(startIdx, endIdx - startIdx).StartsWith(s));
            }
            string parsedPrefix = CoerceStr(prefix);
            int startIndex = start ?? 0;
            int endIndex = end ?? _casefoldContent.Length;
            return _casefoldContent.Substring(startIndex, endIndex - startIndex).StartsWith(parsedPrefix);
        }
    }
}

