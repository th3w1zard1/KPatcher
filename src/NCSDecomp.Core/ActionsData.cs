// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using KPatcher.Core.Formats.NCS.Decompiler;
using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core
{
    /// <summary>
    /// Parses engine action table from nwscript NSS (DeNCS ActionsData.java).
    /// Implements <see cref="IActionsData"/> for <see cref="Decoder"/> ACTION opcode expansion.
    /// </summary>
    public sealed class ActionsData : IActionsData
    {
        private readonly List<ActionEntry> actions;

        public ActionsData(TextReader actionsReader)
        {
            actions = new List<ActionEntry>(877);
            ReadActions(actionsReader);
        }

        /// <summary>
        /// Load nwscript action table from embedded resources (k1_nwscript.nss / tsl_nwscript.nss).
        /// </summary>
        /// <exception cref="FileNotFoundException">Embedded resource or fallback missing.</exception>
        public static ActionsData LoadFromEmbedded(bool tsl)
        {
            Stream stream = tsl ? ResourceLoader.OpenTslNwscript() : ResourceLoader.OpenK1Nwscript();
            using (stream)
            using (var reader = new StreamReader(stream))
            {
                return new ActionsData(reader);
            }
        }

        /// <summary>
        /// Uses <paramref name="k1Path"/> / <paramref name="k2Path"/> when the file exists; otherwise embedded resources (DeNCS Settings paths).
        /// </summary>
        public static ActionsData LoadForGame(bool tsl, string k1Path, string k2Path)
        {
            string path = tsl ? k2Path : k1Path;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                using (var reader = new StreamReader(path))
                {
                    return new ActionsData(reader);
                }
            }

            return LoadFromEmbedded(tsl);
        }

        public string GetAction(int index)
        {
            try
            {
                return actions[index].ToString();
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new InvalidOperationException("Invalid action call: action " + index);
            }
        }

        private void ReadActions(TextReader reader)
        {
            var header = new Regex(@"^\s*//\s*(\d+)\s*(?:[\.:]\s*.*)?$");
            var sig = new Regex(@"^\s*(\w+)\s+(\w+)\s*\((.*)\)\s*;?.*");
            string line;
            bool started = false;
            int pendingIndex = -1;
            int maxIndex = -1;
            while ((line = reader.ReadLine()) != null)
            {
                Match h = header.Match(line);
                if (h.Success)
                {
                    int idx = int.Parse(h.Groups[1].Value);
                    if (idx == 0)
                    {
                        started = true;
                    }

                    if (started)
                    {
                        pendingIndex = idx;
                        if (idx > maxIndex)
                        {
                            maxIndex = idx;
                        }
                    }

                    continue;
                }

                if (!started)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("//"))
                {
                    continue;
                }

                if (pendingIndex >= 0)
                {
                    Match m = sig.Match(line);
                    if (m.Success)
                    {
                        while (actions.Count <= pendingIndex)
                        {
                            actions.Add(null);
                        }

                        actions[pendingIndex] = new ActionEntry(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value);
                    }

                    pendingIndex = -1;
                }
            }

            while (actions.Count <= maxIndex)
            {
                actions.Add(null);
            }
        }

        public DecompType GetReturnType(int index)
        {
            if (index < 0 || index >= actions.Count)
            {
                throw new InvalidOperationException("Invalid action index: " + index);
            }

            if (actions[index] == null)
            {
                throw new InvalidOperationException("Missing action metadata for index: " + index);
            }

            return actions[index].ReturnType();
        }

        public string GetName(int index)
        {
            if (index < 0 || index >= actions.Count)
            {
                throw new InvalidOperationException("Invalid action index: " + index);
            }

            if (actions[index] == null)
            {
                throw new InvalidOperationException("Missing action metadata for index: " + index);
            }

            return actions[index].Name();
        }

        public List<DecompType> GetParamTypes(int index)
        {
            if (index < 0 || index >= actions.Count)
            {
                throw new InvalidOperationException("Invalid action index: " + index);
            }

            if (actions[index] == null)
            {
                throw new InvalidOperationException("Missing action metadata for index: " + index);
            }

            return actions[index].Params();
        }

        public List<string> GetDefaultValues(int index)
        {
            if (index < 0 || index >= actions.Count)
            {
                throw new InvalidOperationException("Invalid action index: " + index);
            }

            if (actions[index] == null)
            {
                throw new InvalidOperationException("Missing action metadata for index: " + index);
            }

            return actions[index].DefaultValues();
        }

        public int GetRequiredParamCount(int index)
        {
            if (index < 0 || index >= actions.Count)
            {
                throw new InvalidOperationException("Invalid action index: " + index);
            }

            if (actions[index] == null)
            {
                throw new InvalidOperationException("Missing action metadata for index: " + index);
            }

            return actions[index].RequiredParamCount();
        }

        public sealed class ActionEntry
        {
            private readonly string name;
            private readonly DecompType _returnType;
            private readonly int paramsize;
            private readonly List<DecompType> paramlist;
            private readonly List<string> defaultValues;

            public ActionEntry(string type, string name, string parameters)
            {
                this.name = name;
                _returnType = DecompType.ParseType(type);
                paramlist = new List<DecompType>();
                defaultValues = new List<string>();
                paramsize = 0;
                var p = new Regex(@"\s*(\w+)\s+\w+(\s*=\s*(\S+))?\s*");
                foreach (string tok in parameters.Split(','))
                {
                    Match m = p.Match(tok);
                    if (m.Success)
                    {
                        paramlist.Add(new DecompType(m.Groups[1].Value));
                        string dv = m.Groups[3].Success ? m.Groups[3].Value.Trim() : null;
                        defaultValues.Add(dv);
                        paramsize += DecompType.TypeSize(m.Groups[1].Value);
                    }
                }
            }

            public override string ToString()
            {
                return "\"" + name + "\" " + _returnType.ToValueString() + " " + paramsize;
            }

            public List<DecompType> Params()
            {
                return paramlist;
            }

            public DecompType ReturnType()
            {
                return _returnType;
            }

            public int Paramsize()
            {
                return paramsize;
            }

            public string Name()
            {
                return name;
            }

            public List<string> DefaultValues()
            {
                return defaultValues;
            }

            public int RequiredParamCount()
            {
                int count = 0;
                for (int i = 0; i < defaultValues.Count; i++)
                {
                    if (defaultValues[i] == null)
                    {
                        count = i + 1;
                    }
                }

                return count;
            }
        }
    }
}
