using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KPatcher.Core.Common;

namespace KPatcher.Core.Utility.MiscString
{
    public static class StringUtilFunctions
    {
        public static string InsertNewlines(string text, int length = 100)
        {
            string[] words = text.Split(' ');
            string newString = "";
            string currentLine = "";

            foreach (string word in words)
            {
                if (currentLine.Length + word.Length + 1 <= length)
                {
                    currentLine += word + " ";
                }
                else
                {
                    newString += currentLine.TrimEnd() + "\n";
                    currentLine = word + " ";
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                newString += currentLine.TrimEnd();
            }

            return newString;
        }

        public static string IReplace(string original, string target, string replacement)
        {
            if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(target))
            {
                return original;
            }

            string result = "";
            int i = 0;
            int targetLength = target.Length;
            string targetLower = target.ToLower();
            string originalLower = original.ToLower();

            while (i < original.Length)
            {
                if (i + targetLength <= originalLower.Length && originalLower.Substring(i, targetLength) == targetLower)
                {
                    result += replacement;
                    i += targetLength;
                }
                else
                {
                    result += original[i];
                    i += 1;
                }
            }
            return result;
        }

        public static string FormatText(object text, int maxCharsBeforeNewline = 20)
        {
            string textStr = text?.ToString() ?? "";
            if (textStr.Contains("\n") || textStr.Length > maxCharsBeforeNewline)
            {
                return $"\"\"\"{Environment.NewLine}{textStr}{Environment.NewLine}\"\"\"";
            }
            return $"'{textStr}'";
        }

        public static int FirstCharDiffIndex(string str1, string str2)
        {
            int minLength = Math.Min(str1.Length, str2.Length);
            for (int i = 0; i < minLength; i++)
            {
                if (str1[i] != str2[i])
                {
                    return i;
                }
            }
            return minLength != str1.Length || minLength != str2.Length ? minLength : -1;
        }

        public static string GenerateDiffMarkerLine(int index, int length)
        {
            if (index == -1)
            {
                return "";
            }
            return new string(' ', index) + "^" + new string(' ', length - index - 1);
        }

        public static Tuple<string, string> CompareAndFormat(object oldValue, object newValue)
        {
            string oldText = oldValue?.ToString() ?? "";
            string newText = newValue?.ToString() ?? "";
            string[] oldLines = oldText.Split('\n');
            string[] newLines = newText.Split('\n');
            List<string> formattedOld = new List<string>();
            List<string> formattedNew = new List<string>();

            int maxLines = Math.Max(oldLines.Length, newLines.Length);
            for (int i = 0; i < maxLines; i++)
            {
                string oldLine = i < oldLines.Length ? oldLines[i] : "";
                string newLine = i < newLines.Length ? newLines[i] : "";
                int diffIndex = FirstCharDiffIndex(oldLine, newLine);
                string markerLine = GenerateDiffMarkerLine(diffIndex, Math.Max(oldLine.Length, newLine.Length));

                formattedOld.Add(oldLine);
                formattedNew.Add(newLine);
                if (!string.IsNullOrEmpty(markerLine))
                {
                    formattedOld.Add(markerLine);
                    formattedNew.Add(markerLine);
                }
            }

            return Tuple.Create(string.Join(Environment.NewLine, formattedOld), string.Join(Environment.NewLine, formattedNew));
        }

        /// <summary>
        /// Strips RTF formatting from text. Delegates to KPatcher.Core.Common.RtfStripper (single source of truth).
        /// </summary>
        public static string StripRtf(string text)
        {
            return RtfStripper.StripRtf(text);
        }
    }
}
