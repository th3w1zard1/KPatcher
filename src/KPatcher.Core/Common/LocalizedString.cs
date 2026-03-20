using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace KPatcher.Core.Common
{
    /// <summary>
    /// Localized strings are a way of the game handling strings that need to be catered to a specific language or gender.
    /// This is achieved through either referencing a entry in the 'dialog.tlk' or by directly providing strings for each language.
    /// </summary>
    public class LocalizedString : IEquatable<LocalizedString>, IEnumerable<(Language, Gender, string)>
    {
        /// <summary>
        /// An index into the 'dialog.tlk' file. If this value is -1 the game will use the stored substrings.
        /// </summary>
        public int StringRef { get; set; }

        private Dictionary<int, string> _substringsInternal;

        public LocalizedString(int stringRef, [CanBeNull] IDictionary<int, string> substrings = null)
        {
            StringRef = stringRef;
            _substringsInternal = new Dictionary<int, string>();
            if (substrings != null)
            {
                SetSubstrings(substrings);
            }
        }

        private Dictionary<int, string> Substrings
        {
            get { return _substringsInternal; }
            set { _substringsInternal = value ?? new Dictionary<int, string>(); }
        }

        public IEnumerator<(Language, Gender, string)> GetEnumerator()
        {
            foreach (KeyValuePair<int, string> kvp in _substringsInternal)
            {
                Language language;
                Gender gender;
                SubstringPair(kvp.Key, out language, out gender);
                yield return (language, gender, kvp.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get { return _substringsInternal.Count; }
        }

        public override int GetHashCode()
        {
            return StringRef;
        }

        public override string ToString()
        {
            if (StringRef >= 0)
            {
                return StringRef.ToString();
            }

            if (Exists(Language.English, Gender.Male))
            {
                string english = Get(Language.English, Gender.Male, false);
                if (english != null)
                {
                    return english;
                }
            }

            foreach ((Language _, Gender _, string text) in this)
            {
                return text;
            }

            return "-1";
        }

        public override bool Equals([CanBeNull] object obj)
        {
            LocalizedString other = obj as LocalizedString;
            return Equals(other);
        }

        public bool Equals([CanBeNull] LocalizedString other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (StringRef != other.StringRef)
            {
                return false;
            }

            if (_substringsInternal.Count != other._substringsInternal.Count)
            {
                return false;
            }

            return _substringsInternal.All(kvp =>
                other._substringsInternal.TryGetValue(kvp.Key, out string value) && value == kvp.Value);
        }

        public static bool operator ==([CanBeNull] LocalizedString left, [CanBeNull] LocalizedString right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left is null || right is null)
            {
                return false;
            }

            return left.Equals(right);
        }

        public static bool operator !=([CanBeNull] LocalizedString left, [CanBeNull] LocalizedString right)
        {
            return !(left == right);
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "stringref", StringRef },
                { "substrings", new Dictionary<int, string>(_substringsInternal) }
            };
        }

        public static LocalizedString FromDictionary(Dictionary<string, object> data)
        {
            int stringRef = Convert.ToInt32(data["stringref"]);
            Dictionary<int, string> substrings = null;
            object subsObj;
            if (data.TryGetValue("substrings", out subsObj) && subsObj is Dictionary<int, string>)
            {
                substrings = (Dictionary<int, string>)subsObj;
            }

            return new LocalizedString(stringRef, substrings);
        }

        public static LocalizedString FromInvalid()
        {
            return new LocalizedString(-1);
        }

        public static LocalizedString FromEnglish(string text)
        {
            LocalizedString locstring = new LocalizedString(-1);
            locstring.SetData(Language.English, Gender.Male, text);
            return locstring;
        }

        public static int SubstringId(Language language, Gender gender)
        {
            return (int)language * 2 + (int)gender;
        }

        public static int SubstringId(int language, int gender)
        {
            Language languageEnum = LanguageExtensions.FromValue(language);
            Gender genderEnum = ToGenderEnum(gender);
            return SubstringId(languageEnum, genderEnum);
        }

        public static void SubstringPair(int substringId, out Language language, out Gender gender)
        {
            language = LanguageExtensions.FromValue(substringId / 2);
            gender = (Gender)(substringId % 2);
        }

        public void SetData(Language language, Gender gender, string text)
        {
            int substringId = SubstringId(language, gender);
            _substringsInternal[substringId] = text;
        }

        public void SetData(int language, int gender, string text)
        {
            SetData(LanguageExtensions.FromValue(language), ToGenderEnum(gender), text);
        }

        public void SetString(int substringId, string text)
        {
            Language language;
            Gender gender;
            SubstringPair(substringId, out language, out gender);
            SetData(language, gender, text);
        }

        public string Get(Language language, Gender gender, bool useFallback = false)
        {
            int substringId = SubstringId(language, gender);
            string value;
            if (_substringsInternal.TryGetValue(substringId, out value))
            {
                return value;
            }

            if (useFallback)
            {
                return _substringsInternal.Values.FirstOrDefault();
            }

            return null;
        }

        public string Get(int language, int gender, bool useFallback = false)
        {
            return Get(LanguageExtensions.FromValue(language), ToGenderEnum(gender), useFallback);
        }

        public string Get(int language, bool useFallback = false)
        {
            return Get(LanguageExtensions.FromValue(language), Gender.Male, useFallback);
        }

        public void Remove(Language language, Gender gender)
        {
            int substringId = SubstringId(language, gender);
            _substringsInternal.Remove(substringId);
        }

        public void Remove(int language, int gender)
        {
            Remove(LanguageExtensions.FromValue(language), ToGenderEnum(gender));
        }

        public bool Exists(Language language, Gender gender)
        {
            int substringId = SubstringId(language, gender);
            return _substringsInternal.ContainsKey(substringId);
        }

        public bool Exists(int language, int gender)
        {
            return Exists(LanguageExtensions.FromValue(language), ToGenderEnum(gender));
        }

        private void SetSubstrings(IEnumerable<KeyValuePair<int, string>> substrings)
        {
            _substringsInternal.Clear();
            foreach (KeyValuePair<int, string> pair in substrings)
            {
                _substringsInternal[Convert.ToInt32(pair.Key)] = pair.Value;
            }
        }

        private static Gender ToGenderEnum(int gender)
        {
            if (!Enum.IsDefined(typeof(Gender), gender))
            {
                throw new ArgumentException("Invalid gender value: " + gender);
            }

            return (Gender)gender;
        }
    }
}
