# KCompiler.Core

NuGet package **`KCompiler.Core`**: cross-platform **managed** NSS→NCS compiler used by **KPatcher.Core**.

- **CLR types** for the compiler pipeline remain in the `KPatcher.Core.*` namespaces (single shared source tree under `src/KPatcher.Core` on disk, compiled into this assembly).
- **`KCompiler` namespace**: `ManagedNwnnsscomp` (overload-friendly API) and `KCompiler.Cli.NwnnsscompCliParser` for nwnnsscomp-style argv.

**CLI**: see **`KCompiler.NET`** (`dotnet run --project ../KCompiler.NET/... -- -c in.nss -o out.ncs -g 1`).

Pack: `dotnet pack src/KCompiler.Core/KCompiler.Core.csproj -c Release`
