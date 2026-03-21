---
title: "NCSDecomp lexer PushBack must match Java (suffix pushback, not prefix)"
category: debugging-patterns
tags: [ncsdecomp, lexer, dencs, porting, tokenization]
module: NCSDecomp.Core
symptom: "Decompiler token stream wrong after lexing; e.g. space after MOVSP lexed as TT instead of TBlank; parser/AST failures downstream"
root_cause: "C# Lexer.PushBack pushed the wrong character range vs Java Lexer.pushBack — accepted lexeme prefix was pushed back instead of the unread suffix"
---

## Symptom

- Tokenization of decompiler intermediate text (e.g. after `MOVSP`) does not match Java DeNCS.
- A single space may be emitted as the wrong token type (e.g. **`TT`** instead of **`TBlank`**), breaking the parser.
- Downstream: **`ParseAst`**, **`FileDecompiler`**, or CLI decompile may fail with lexer errors or parser stack errors.

## Root cause

Java **`Lexer.pushBack(int acceptLength)`** (DeNCS / SableCC-style) pushes back characters from the **end** of the accumulated buffer **down to** `acceptLength` — i.e. the **suffix** that was read **after** the accepted lexeme, not the accepted prefix.

A C# port that pushes back indices **`0 .. acceptLength-1`** (prefix) leaves the buffer state inconsistent with Java and produces incorrect tokens on the next read.

## Working fix (KPatcher)

Align **`NCSDecomp.Core.Lexer.Lexer.PushBack(int acceptLength)`** with Java:

```csharp
private void PushBack(int acceptLength)
{
    string t = _text.ToString();
    for (int i = t.Length - 1; i >= acceptLength; i--)
    {
        _eof = false;
        _in.Unread(t[i]);
    }
}
```

Keep **`PushbackReader.PushBack(string, int)`** consistent if it implements the same contract.

## Verification

- Unit/smoke tests: single space after a keyword lexes as **`TBlank`**; token sequence for lines such as **`T 8 <n>;`** matches expectations.
- See **`KPatcher.Tests`** (e.g. **`NcsLexerSmokeTest`**) when present.

## Prevention

- When porting generated lexers from Java SableCC, **diff `pushBack` / `getToken` control flow** against the vendor source; do not assume “push back” means “rewind the accepted part.”
- Add a **small lexer smoke test** for any hand-maintained lexer.dat port whenever lexer code changes.

## References

- Vendor: `vendor/DeNCS/...` Java lexer (compare **`pushBack`**).
- Repo: `src/NCSDecomp.Core/Lexer/Lexer.cs`, `PORTING_STATUS.md`.
- Related compound (CI): [`deployment-issues/gha-pwsh-shell-syntax-mismatch.md`](../deployment-issues/gha-pwsh-shell-syntax-mismatch.md).
