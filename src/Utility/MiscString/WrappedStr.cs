using System;
using System.Reflection;
using JetBrains.Annotations;

namespace KPatcher.Core.Utility.MiscString
{
    public static class StringUtil
    {
        public static bool IsStringLike(object obj)
        {
            try
            {
                var _ = obj + "";
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class WrappedStr
    {
        protected string _content;

        public WrappedStr(string content = "")
        {
            if (content == null)
            {
                throw new Exception($"Cannot initialize {GetType().Name}(None), expected a str-like argument");
            }
            _content = content;
        }

        // Additional constructor to handle WrappedStr instances
        public WrappedStr(WrappedStr content)
        {
            if (content == null)
            {
                throw new Exception($"Cannot initialize {GetType().Name}(None), expected a str-like argument");
            }
            _content = content._content;
        }

        public override string ToString()
        {
            return $"WrappedStr({_content})";
        }

        public object GetReduce()
        {
            return new object[] { GetType(), new object[] { _content } };
        }

        protected static string AssertStrOrNoneType(Type cls, object var)
        {
            if (var == null)
            {
                return null;
            }
            if (!(var is string) && !(var != null && var.GetType() == cls))
            {
                throw new Exception($"Expected str-like, got '{var}' of type {var?.GetType()}");
            }
            return var?.ToString();
        }

        public WrappedStr RemovePrefix(WrappedStr prefix)
        {
            return RemovePrefix(prefix?._content ?? prefix?.ToString() ?? "");
        }

        public WrappedStr RemovePrefix(string prefix)
        {
            string parsedPrefix = AssertStrOrNoneType(GetType(), prefix);
            if (_content.StartsWith(parsedPrefix))
            {
                return (WrappedStr)Activator.CreateInstance(GetType(), _content.Substring(parsedPrefix.Length));
            }
            return (WrappedStr)Activator.CreateInstance(GetType(), _content);
        }

        public WrappedStr RemoveSuffix(WrappedStr suffix)
        {
            return RemoveSuffix(suffix?._content ?? suffix?.ToString() ?? "");
        }

        public WrappedStr RemoveSuffix(string suffix)
        {
            string parsedSuffix = AssertStrOrNoneType(GetType(), suffix);
            if (_content.EndsWith(parsedSuffix))
            {
                return (WrappedStr)Activator.CreateInstance(GetType(), _content.Substring(0, _content.Length - parsedSuffix.Length));
            }
            return (WrappedStr)Activator.CreateInstance(GetType(), _content);
        }

        public string GetState()
        {
            return _content;
        }

        // Forward all string operations to _content
        public static implicit operator string(WrappedStr wrapped)
        {
            return wrapped?._content ?? "";
        }

        public static implicit operator WrappedStr(string str)
        {
            return new WrappedStr(str);
        }

        public int Length => _content.Length;
        public char this[int index] => _content[index];
    }
}

