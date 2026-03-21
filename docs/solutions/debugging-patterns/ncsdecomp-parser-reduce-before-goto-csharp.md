---
title: "NCSDecomp Parser: call reduce (NewN) before GoTo in C#"
category: debugging-patterns
tags: [ncsdecomp, parser, sablecc, csharp, java-port]
module: NCSDecomp.Core
symptom: "InvalidCastException in Parser.New0/NewN when popping AST nodes (wrong types on stack)"
root_cause: "C# evaluates method arguments left-to-right; Java DeNCS push(goTo(), newN()) evaluates newN() first"
---

## Symptom

Managed pipeline `NcsParsePipeline.ParseAst` / `Parser.Parse` fails with `InvalidCastException` inside generated reduce factories (e.g. `New0` expected `PReturn` but saw `ACommandBlock` or `X2PSubroutine`). Lexer/parser `.dat` files can be byte-identical to Java and the bug still appears.

## Root cause

LR reduce steps must:

1. Pop the RHS from the parser stack (`newN()` / `NewN()` in SableCC output).
2. Read the **new** top-of-stack state for the **goto** table (`goTo(productionIndex)` / `GoTo(...)`).

In **Java**, `push(this.goTo(0), this.new0(), true)` evaluates **`new0()`** before **`goTo(0)`** (argument evaluation order).

In **C#**, `Push(GoTo(0), New0(), true)` evaluates **`GoTo(0)`** first, so `GoTo` sees the pre-reduce stack and pushes the wrong state. The parse then desynchronizes and casts fail later.

## Fix

For every reduce arm, do not pass `NewN()` as an inline argument to `Push(GoTo(...), ...)`. Use a local:

```csharp
AstNode r = New0();
Push(GoTo(0), r, true);
```

## Prevention

- Keep the comment above the reduce `switch` in `Parser.cs`.
- Regenerating or re-porting SableCC `Parser` output: preserve **reduce-before-goto** ordering for C#.

## Verification

- `KPatcher.Tests`: `NcsManagedFullDecompileSmokeTests` and full suite should pass after the fix.
