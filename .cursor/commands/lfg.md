As an AI code assistant, please follow these instructions to carry out the porting and integration task. Be meticulous and ensure all criteria and validation steps are covered.



## Policy: managed NSS/NCS only (no registry spoofer, no nwnnsscomp.exe)



- **Registry spoofer:** **Do not add or rely on registry spoofing.** Target state is **zero** registry-spoofer surface area for this workstream. The original DeNCS/Java flow used it to fake install paths so **external** `nwnnsscomp.exe` could run; KPatcher’s path is **in-process managed compilation**, not shelling out to BioWare-era tools.

- **NSS → NCS:** Use **`src/KCompiler.Core`** and **`src/KCompiler.NET`** (managed compiler). Do **not** require, prefer, or document `nwnnsscomp.exe` as part of completion criteria. Integration in **`src/KPatcher.Core/Formats/NCS`** should use the same managed compiler story as the rest of the patcher.

- **NCS → NSS:** Use **`src/NCSDecomp.Core`** (library) plus **`src/NCSDecomp.NET`** (CLI host) and **`src/KPatcher.Core/Formats/NCS`** for patcher-facing APIs—not external decomp tools for core flows.



*(Rationale aligned with “library in-process vs spawn exe”: MSBuild **UsingTask** runs task code in the build process; **Exec** runs a separate executable—prefer the former pattern conceptually for product code. See [UsingTask](https://learn.microsoft.com/en-us/visualstudio/msbuild/usingtask-element-msbuild?view=vs-2022) vs [Exec task](https://learn.microsoft.com/en-us/visualstudio/msbuild/exec-task?view=vs-2022). Optional CLI features can follow [.NET Generic Host](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host?tabs=appbuilder) composition patterns.)*



## Steps



1. **Full Porting and Integration**

   - Port all Java source files from `C:/GitHub/KPatcher/vendor/DeNCS` into C# under **`src/NCSDecomp.Core`** (library). **`src/NCSDecomp.NET`** remains the thin CLI entry that references Core—not the home for the whole AST/parser stack.

   - Identify all relevant decompiler logic. Merge and adapt integration points into **`C:/GitHub/KPatcher/src/KPatcher.Core/Formats/NCS`** as appropriate (e.g. managed decompiler wrapper, decoder alignment).

   - Ensure *every* `.java` file with relevant decompiler/compiler logic is ported or explicitly superseded (e.g. Swing UI replaced by **`src/NCSDecomp.UI`** with **no** registry-spoofer requirement).

   - When retiring Java-only “external compiler + registry” paths, **remove or no-op** spoofing hooks rather than porting them as product features.



2. **Test Verification and Roundtrip Tests**

   - Locate all NCS/NSS related tests in the codebase, especially roundtrip tests.

   - There should be approximately 8 NCS/NSS C# test fixtures/classes worth of coverage (compiler, format, interpreter, optimizer, roundtrip, lexer/decomp smoke, etc.). Ensure coverage is present.

   - If roundtrip NCS/NSS tests are missing or incomplete, examine:

     - Test code in `dencs` within the imported sources,

     - The test suite in `C:/GitHub/Andastra/`.

   - Port any relevant roundtrip tests into **`tests/KPatcher.Tests`**, using **managed** compile/decompile paths unless a test is explicitly marked as legacy/external-only (avoid making `nwnnsscomp.exe` a default gate).

   - Run all tests and confirm correctness and roundtrip fidelity.



3. **Completion Criteria**

   - All relevant `.java` files are accounted for as C# in **`NCSDecomp.Core`** / **`KPatcher.Core/Formats/NCS`** (and CLI/UI hosts as appropriate)—with **no** dependency on registry spoofing for success.

   - Compiler and decompiler code is functionally integrated via **KCompiler** + **NCSDecomp.Core** with no unjustified logic loss relative to the Java reference (document deliberate omissions, e.g. external-exe workflows).

   - All NCS/NSS related tests, including roundtrip tests, exist and pass successfully.

   - Any required test code from external sources (`dencs`, `Andastra`) is incorporated in a way that **passes with managed tooling**.



Once these steps are completed, provide a summary verification and confirm all areas above have been addressed completely and successfully.

