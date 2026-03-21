// Copyright 2021-2025 NCSDecomp / KPatcher

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NCSDecomp.Core.Node;
using AstNode = global::NCSDecomp.Core.Node.Node;

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// One node in a parse-tree outline built for display (e.g. Avalonia <c>TreeView</c>).
    /// Children are discovered via public <c>Get*</c> accessors on SableCC AST <see cref="AstNode"/> types.
    /// </summary>
    public sealed class AstOutlineNode
    {
        public AstOutlineNode(string label, IReadOnlyList<AstOutlineNode> children)
        {
            Label = label ?? string.Empty;
            Children = children ?? Array.Empty<AstOutlineNode>();
        }

        public string Label { get; }
        public IReadOnlyList<AstOutlineNode> Children { get; }
    }

    /// <summary>
    /// Builds a compact outline of the lexer/parser AST (pre-analysis). Safe limits avoid huge UI trees.
    /// </summary>
    public static class NcsAstOutline
    {
        public const int DefaultMaxNodes = 40000;
        public const int DefaultMaxDepth = 128;

        /// <summary>
        /// Build an outline from the root <see cref="Start"/> node.
        /// </summary>
        public static AstOutlineNode Build(Start root, int maxNodes = DefaultMaxNodes, int maxDepth = DefaultMaxDepth)
        {
            if (root == null)
            {
                return new AstOutlineNode("(null)", Array.Empty<AstOutlineNode>());
            }

            int count = 0;
            return BuildNode(root, 0, maxNodes, maxDepth, ref count);
        }

        private static AstOutlineNode BuildNode(AstNode node, int depth, int maxNodes, int maxDepth, ref int count)
        {
            if (node == null)
            {
                return new AstOutlineNode("(null)", Array.Empty<AstOutlineNode>());
            }

            if (count >= maxNodes)
            {
                return new AstOutlineNode("… (node limit)", Array.Empty<AstOutlineNode>());
            }

            count++;

            string label = FormatLabel(node);
            if (depth >= maxDepth)
            {
                return new AstOutlineNode(label + " [max depth]", Array.Empty<AstOutlineNode>());
            }

            var children = new List<AstOutlineNode>();
            foreach (AstNode child in EnumerateChildNodes(node))
            {
                if (count >= maxNodes)
                {
                    children.Add(new AstOutlineNode("… (node limit " + maxNodes + ")", Array.Empty<AstOutlineNode>()));
                    break;
                }

                children.Add(BuildNode(child, depth + 1, maxNodes, maxDepth, ref count));
            }

            return new AstOutlineNode(label, children);
        }

        private static string FormatLabel(AstNode node)
        {
            string typeName = node.GetType().Name;
            if (node is Token tok)
            {
                string text = tok.GetText() ?? string.Empty;
                if (text.Length > 0)
                {
                    var sb = new StringBuilder(text.Length);
                    foreach (char c in text)
                    {
                        if (c == '\r')
                        {
                            sb.Append("\\r");
                        }
                        else if (c == '\n')
                        {
                            sb.Append("\\n");
                        }
                        else if (c == '\t')
                        {
                            sb.Append("\\t");
                        }
                        else
                        {
                            sb.Append(c);
                        }
                    }

                    string shown = sb.ToString();
                    if (shown.Length > 48)
                    {
                        shown = shown.Substring(0, 45) + "…";
                    }

                    return typeName + " \"" + shown + "\"";
                }
            }

            return typeName;
        }

        private static IEnumerable<AstNode> EnumerateChildNodes(AstNode node)
        {
            foreach (MethodInfo m in CollectGetters(node))
            {
                object val;
                try
                {
                    val = m.Invoke(node, null);
                }
                catch
                {
                    continue;
                }

                if (val == null)
                {
                    continue;
                }

                if (val is AstNode n)
                {
                    yield return n;
                    continue;
                }

                if (val is IEnumerable<AstNode> en)
                {
                    foreach (AstNode x in en)
                    {
                        if (x != null)
                        {
                            yield return x;
                        }
                    }

                    continue;
                }

                if (val is IEnumerable seq && !(val is string))
                {
                    foreach (object o in seq)
                    {
                        if (o is AstNode nn)
                        {
                            yield return nn;
                        }
                    }
                }
            }
        }

        private static List<MethodInfo> CollectGetters(AstNode node)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var list = new List<MethodInfo>();

            for (Type cur = node.GetType();
                 cur != null && typeof(AstNode).IsAssignableFrom(cur);
                 cur = cur.BaseType)
            {
                foreach (MethodInfo m in cur.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (m.Name.Length < 4 || !m.Name.StartsWith("Get", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (m.GetParameters().Length != 0)
                    {
                        continue;
                    }

                    if (m.Name == "GetType" || m.Name == "GetHashCode")
                    {
                        continue;
                    }

                    if (!seen.Add(m.Name))
                    {
                        continue;
                    }

                    Type rt = m.ReturnType;
                    if (typeof(AstNode).IsAssignableFrom(rt))
                    {
                        list.Add(m);
                        continue;
                    }

                    if (rt == typeof(string))
                    {
                        continue;
                    }

                    Type elem = GetEnumerableNodeElementType(rt);
                    if (elem != null)
                    {
                        list.Add(m);
                    }
                }
            }

            return list.OrderBy(x => x.Name, StringComparer.Ordinal).ToList();
        }

        private static Type GetEnumerableNodeElementType(Type rt)
        {
            if (rt == null)
            {
                return null;
            }

            if (rt.IsGenericType && rt.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                Type arg = rt.GetGenericArguments()[0];
                return typeof(AstNode).IsAssignableFrom(arg) ? arg : null;
            }

            foreach (Type iface in rt.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    Type arg = iface.GetGenericArguments()[0];
                    if (typeof(AstNode).IsAssignableFrom(arg))
                    {
                        return arg;
                    }
                }
            }

            if (typeof(IEnumerable).IsAssignableFrom(rt) && rt != typeof(string))
            {
                return typeof(AstNode);
            }

            return null;
        }
    }
}
