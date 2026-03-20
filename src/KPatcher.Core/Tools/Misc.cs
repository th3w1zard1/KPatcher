using System;
using System.IO;
using JetBrains.Annotations;

namespace KPatcher.Core.Tools
{
    public static class FileHelpers
    {
        public static string NormalizeExt(string strRepr)
        {
            if (string.IsNullOrEmpty(strRepr))
            {
                return "";
            }
            if (strRepr[0] == '.')
            {
                return $"stem{strRepr}";
            }
            if (!strRepr.Contains("."))
            {
                return $"stem.{strRepr}";
            }
            return strRepr;
        }

        public static string NormalizeStem(string strRepr)
        {
            if (string.IsNullOrEmpty(strRepr))
            {
                return "";
            }
            if (strRepr.EndsWith("."))
            {
                return $"{strRepr}ext";
            }
            if (!strRepr.Contains("."))
            {
                return $"{strRepr}.ext";
            }
            return strRepr;
        }

        public static bool IsNssFile(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                return false;
            }
            return Path.GetExtension(NormalizeExt(filepath)).Equals(".nss", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsModFile(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                return false;
            }
            return Path.GetExtension(NormalizeExt(filepath)).Equals(".mod", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsErfFile(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                return false;
            }
            return Path.GetExtension(NormalizeExt(filepath)).Equals(".erf", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsSavFile(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                return false;
            }
            return Path.GetExtension(NormalizeExt(filepath)).Equals(".sav", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsAnyErfTypeFile(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                return false;
            }
            string ext = Path.GetExtension(NormalizeExt(filepath)).ToLowerInvariant();
            return ext == ".erf" || ext == ".mod" || ext == ".sav";
        }

        public static bool IsRimFile(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                return false;
            }
            return Path.GetExtension(NormalizeExt(filepath)).Equals(".rim", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsBifFile(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                return false;
            }
            // Fast path: use string operations instead of Path for better performance
            string lowerPath = filepath.ToLowerInvariant();
            return lowerPath.EndsWith(".bif", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsBzfFile(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                return false;
            }
            // Fast path: use string operations instead of Path for better performance
            return filepath.ToLowerInvariant().EndsWith(".bzf", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsCapsuleFile(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                return false;
            }
            // Fast path: use string operations instead of Path for better performance
            // Check common extensions directly without creating path objects
            string lowerPath = filepath.ToLowerInvariant();
            return lowerPath.EndsWith(".erf", StringComparison.OrdinalIgnoreCase) ||
                   lowerPath.EndsWith(".mod", StringComparison.OrdinalIgnoreCase) ||
                   lowerPath.EndsWith(".rim", StringComparison.OrdinalIgnoreCase) ||
                   lowerPath.EndsWith(".sav", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsStorageFile(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                return false;
            }
            string ext = Path.GetExtension(NormalizeExt(filepath)).ToLowerInvariant();
            return ext == ".erf" || ext == ".mod" || ext == ".sav" || ext == ".rim" || ext == ".bif";
        }
    }
}

