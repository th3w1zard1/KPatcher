// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace NCSDecomp.Core.ScriptUtils
{
    /// <summary>
    /// SubScriptState trace channel: forwards to <see cref="ILogger"/> at Trace when a sink is set (see <see cref="FileDecompiler.DecompileToNss"/>).
    /// Set <c>KPATCHER_TOOL_LOG_LEVEL=Trace</c> to emit from CLI hosts.
    /// </summary>
    internal static class SubScriptLogger
    {
        private static ILogger _sink = NullLogger.Instance;

        public static void SetDiagnosticSink(ILogger log)
        {
            _sink = log ?? NullLogger.Instance;
        }

        public static void ClearDiagnosticSink()
        {
            _sink = NullLogger.Instance;
        }

        public static void Trace(string message)
        {
            if (_sink.IsEnabled(LogLevel.Trace))
            {
                _sink.Log(LogLevel.Trace, default(EventId), message, null, (s, _) => "[SubScriptState] " + s);
            }

#if DEBUG
            Debug.WriteLine("[SubScriptState] " + message);
#endif
        }
    }
}
