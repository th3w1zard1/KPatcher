// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Diagnostics;
using System.IO;
using KCompiler.Diagnostics;
using KPatcher.Core.Formats.NCS.Decompiler;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NCSDecomp.Core.Diagnostics;
using NCSDecomp.Core.Node;
using LexerImpl = global::NCSDecomp.Core.Lexer.Lexer;
using ParserImpl = global::NCSDecomp.Core.Parser.Parser;

namespace NCSDecomp.Core
{
    /// <summary>
    /// DeNCS pipeline stages on shared KPatcher.Core bytecode: decode -> lexer -> parser AST.
    /// Full NSS emission: run <see cref="FileDecompiler"/> after parse.
    /// </summary>
    public static class NcsParsePipeline
    {
        /// <summary>
        /// Decode NCS bytes to the DeNCS token stream (same string <see cref="NCSDecompiler"/> produces for tests).
        /// </summary>
        public static string DecodeToTokenStream(byte[] ncsBytes, IActionsData actions = null)
        {
            if (ncsBytes == null || ncsBytes.Length == 0)
            {
                throw new ArgumentException("NCS bytes null or empty.", nameof(ncsBytes));
            }

            var decoder = new Decoder(ncsBytes);
            return decoder.Decode(actions);
        }

        /// <summary>
        /// Run decode -> SableCC lexer -> parser and return the root <see cref="Start"/> node.
        /// </summary>
        public static Start ParseAst(byte[] ncsBytes, IActionsData actions = null, ILogger log = null)
        {
            ILogger logger = log ?? NullLogger.Instance;
            string cid = ToolCorrelation.ReadOptional();
            var swDecode = Stopwatch.StartNew();
            string tokenStream;
            try
            {
                tokenStream = DecodeToTokenStream(ncsBytes, actions);
            }
            catch (Exception ex)
            {
                swDecode.Stop();
                logger.LogError(
                    ex,
                    "Tool=NCSDecomp.Core Operation=parse Phase={Phase} CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} InputBytes={Bytes} Message=NCS decode to token stream failed",
                    DecompPhaseNames.DecompBytecodeToTokens,
                    cid ?? "",
                    swDecode.ElapsedMilliseconds,
                    ncsBytes.Length);
                throw;
            }

            swDecode.Stop();
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    "Tool=NCSDecomp.Core Operation=parse Phase={Phase} CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} InputBytes={Bytes} TokenChars={Chars}",
                    DecompPhaseNames.DecompBytecodeToTokens,
                    cid ?? "",
                    swDecode.ElapsedMilliseconds,
                    ncsBytes.Length,
                    tokenStream?.Length ?? 0);
            }

            var swCc = Stopwatch.StartNew();
            Start ast;
            try
            {
                var lexer = new LexerImpl(new StringReader(tokenStream));
                var parser = new ParserImpl(lexer);
                ast = parser.Parse();
            }
            catch (Exception ex)
            {
                swCc.Stop();
                logger.LogError(
                    ex,
                    "Tool=NCSDecomp.Core Operation=parse Phase={Phase} CorrelationId={CorrelationId} ElapsedMs={ElapsedMs} InputBytes={Bytes} TokenChars={Chars} Message=SableCC lexer/parser failed",
                    DecompPhaseNames.DecompSableCc,
                    cid ?? "",
                    swCc.ElapsedMilliseconds,
                    ncsBytes.Length,
                    tokenStream?.Length ?? 0);
                throw;
            }

            swCc.Stop();
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    "Tool=NCSDecomp.Core Operation=parse Phase={Phase} CorrelationId={CorrelationId} ElapsedMs={ElapsedMs}",
                    DecompPhaseNames.DecompSableCc,
                    cid ?? "",
                    swCc.ElapsedMilliseconds);
            }

            return ast;
        }
    }
}
