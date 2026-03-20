# Simplified Implementations in KPatcher

This document lists all instances where KPatcher code has "simplified" implementations compared to the PyKotor vendor reference code, along with what needs to be fully ported.

## MDL Format Handlers

### 1. MDLAsciiReader
**File:** `src/KPatcher.Core/Formats/MDL/MDLAsciiReader.cs`  
**Status:** Simplified - minimal stub  
**PyKotor Reference:** `vendor/PyKotor/Libraries/PyKotor/src/pykotor/resource/formats/mdl/io_mdl_ascii.py` (MDLAsciiReader class, ~2500+ lines)

**What's Missing:**
- Complete ASCII MDL parsing (currently just reads file as text)
- Node hierarchy parsing (dummy, light, emitter, reference, trimesh, skin, danglymesh, aabb, lightsaber nodes)
- Mesh data parsing (vertices, normals, UVs, faces, materials, smoothing groups)
- Controller parsing (position, orientation, scale, alpha, color, etc.)
- Animation parsing
- Light, emitter, reference, saber, skin, dangly, walkmesh data parsing
- Face material and smoothing group unpacking
- Quaternion/angle-axis conversions
- Vector normalization

**Key Differences:**
- KPatcher: Only reads file as ASCII text, returns empty MDL with placeholder name
- PyKotor: Full parser with ~2500+ lines handling all node types, controllers, animations, and mesh data

---

### 2. MDLAsciiWriter
**File:** `src/KPatcher.Core/Formats/MDL/MDLAsciiWriter.cs`  
**Status:** Simplified - placeholder stub  
**PyKotor Reference:** `vendor/PyKotor/Libraries/PyKotor/src/pykotor/resource/formats/mdl/io_mdl_ascii.py` (MDLAsciiWriter class, ~2000+ lines)

**What's Missing:**
- Complete ASCII MDL writing (currently just writes placeholder comment)
- Node hierarchy serialization
- Mesh data serialization (vertices, faces, materials, smoothing groups)
- Controller serialization
- Animation serialization
- Light, emitter, reference, saber, skin, dangly, walkmesh serialization
- Proper formatting matching MDLOps ASCII format
- Face material and smoothing group packing
- Quaternion/angle-axis conversions

**Key Differences:**
- KPatcher: Writes only "# MDL ASCII export placeholder\n"
- PyKotor: Full writer with ~2000+ lines generating complete MDLOps-compatible ASCII output

---

### 3. MDLBinaryReader
**File:** `src/KPatcher.Core/Formats/MDL/MDLBinaryReader.cs`  
**Status:** Simplified - minimal stub  
**PyKotor Reference:** `vendor/PyKotor/Libraries/PyKotor/src/pykotor/resource/formats/mdl/io_mdl.py` (MDLBinaryReader class, ~800+ lines)

**What's Missing:**
- Complete binary MDL/MDX parsing (currently returns empty MDL)
- MDL header parsing (name, supermodel, classification, fog, animation scale, bounding box, radius)
- Node hierarchy parsing with all node types
- Mesh data parsing (vertices, normals, UVs, faces, materials)
- Controller parsing (position, orientation, scale, alpha, color, etc.)
- Animation parsing from MDX file
- Light, emitter, reference, saber, skin, dangly, walkmesh (AABB) parsing
- Fast-load mode support (skip animations/controllers for rendering)
- Game version handling (K1 vs K2 differences)
- AABB tree parsing with safety checks (KOQ200 walkmesh fix)

**Key Differences:**
- KPatcher: Returns empty MDL, just seeks to position 0
- PyKotor: Full binary parser with ~800+ lines, supports fast-load mode, handles K1/K2 differences, includes AABB safety fixes

---

### 4. MDLBinaryWriter
**File:** `src/KPatcher.Core/Formats/MDL/MDLBinaryWriter.cs`  
**Status:** Simplified - minimal stub  
**PyKotor Reference:** `vendor/PyKotor/Libraries/PyKotor/src/pykotor/resource/formats/mdl/io_mdl.py` (MDLBinaryWriter class, ~500+ lines)

**What's Missing:**
- Complete binary MDL/MDX writing (currently writes two zero uint32s)
- MDL header writing
- Node hierarchy serialization
- Mesh data serialization
- Controller serialization
- Animation serialization to MDX file
- Light, emitter, reference, saber, skin, dangly, walkmesh serialization
- Proper binary format layout matching game engine expectations

**Key Differences:**
- KPatcher: Writes only `WriteUInt32(0)` for both MDL and MDX
- PyKotor: Full binary writer with ~500+ lines generating complete MDL/MDX files

---

### 5. MDLAuto
**File:** `src/KPatcher.Core/Formats/MDL/MDLAuto.cs`  
**Status:** Simplified detector and dispatcher  
**PyKotor Reference:** `vendor/PyKotor/Libraries/PyKotor/src/pykotor/resource/formats/mdl/mdl_auto.py` (~485 lines)

**What's Missing:**
- AABB seek safety patching (KOQ200 walkmesh fix)
- Self-healing MDL reader after bad seek errors
- Walkmesh-safe reader preparation
- Error recovery mechanisms
- Proper error handling for AABB seek issues

**Key Differences:**
- KPatcher: Basic format detection (binary vs ASCII) only
- PyKotor: Includes runtime patching of vulnerable AABB reader, self-healing after errors, walkmesh-safe reader preparation

---

## TPC Format Handlers

### 6. TPCBinaryReader
**File:** `src/KPatcher.Core/Formats/TPC/TPCBinaryReader.cs`  
**Status:** Simplified - reads core header, mipmaps, and TXI text without conversions  
**PyKotor Reference:** `vendor/PyKotor/Libraries/PyKotor/src/pykotor/resource/formats/tpc/io_tpc.py` (TPCBinaryReader class, ~450+ lines)

**What's Missing:**
- Texture format conversion support (DXT decompression, RGB/RGBA conversions, BGRA deswizzling)
- Animated texture support (TXI proceduretype="cycle" detection, layer count calculation from numx/numy)
- Cube map normalization (rotation handling)
- Dimension validation (MAX_DIMENSIONS check)
- Data size validation for compressed textures
- BGRA deswizzling (swizzle pattern removal)
- Cube map rotation handling

**Key Differences:**
- KPatcher: Reads header, mipmaps, and TXI text but doesn't perform format conversions or handle animated textures
- PyKotor: Full reader with format conversions, animated texture support, cube map normalization, and validation

---

### 7. TPCBinaryWriter
**File:** `src/KPatcher.Core/Formats/TPC/TPCBinaryWriter.cs`  
**Status:** Simplified writer matching core of PyKotor  
**PyKotor Reference:** `vendor/PyKotor/Libraries/PyKotor/src/pykotor/resource/formats/tpc/io_tpc.py` (TPCBinaryWriter class, ~200+ lines)

**What's Missing:**
- Texture format conversion support (DXT compression, RGB/RGBA conversions, BGRA swizzling)
- Animated texture support (TXI proceduretype="cycle" handling)
- Cube map swizzling/rotation
- Proper data size calculation for all formats
- Format validation

**Key Differences:**
- KPatcher: Basic writer that writes header and mipmap data
- PyKotor: Full writer with format conversions, animated texture support, and cube map handling

---

## NCS Compiler

### 8. Interpreter (NCS Bytecode Interpreter)
**File:** `src/KPatcher.Core/Formats/NCS/Compiler/Interpreter.cs`  
**Status:** Partially implemented  
**PyKotor Reference:** `vendor/PyKotor/Libraries/PyKotor/src/pykotor/resource/formats/ncs/compiler/interpreter.py`

**What's Missing:**
- Complete instruction implementation (currently throws `NotImplementedException` for unimplemented instructions)
- Action queue storage (comment says "simplified for now")
- Full action execution support
- Complete stack snapshot functionality

**Key Differences:**
- KPatcher: Throws `NotImplementedException` for many instruction types, simplified action queue
- PyKotor: Full interpreter with complete instruction support and action queue management

**Specific Issues:**
- Line 597: `throw new NotImplementedException($"Instruction {instruction.InsType} not implemented");`
- Line 604: Comment indicates action queue storage is simplified

---

## NSS Parser

### 9. NssParser - String Unescaping
**File:** `src/KPatcher.Core/Formats/NCS/Compiler/NSS/NssParser.cs`  
**Status:** TODO - unescape string  
**PyKotor Reference:** `vendor/PyKotor/Libraries/PyKotor/src/pykotor/resource/formats/ncs/compiler/nss/lexer.py`

**What's Missing:**
- String literal unescaping (escape sequences like `\n`, `\t`, `\\`, `\"`, etc.)

**Key Differences:**
- KPatcher: Line 2426 has `// TODO: unescape string` - strings are read but not unescaped
- PyKotor: Full string unescaping support in lexer

---

## GFF Modifications

### 10. ModifyGFF - Path Mutability
**File:** `src/KPatcher.Core/Mods/GFF/ModifyGFF.cs`  
**Status:** FIXME - C# limitation  
**PyKotor Reference:** `vendor/PyKotor/Libraries/PyKotor/src/pykotor/kpatcher/mods/gff.py`

**What's Missing:**
- Proper path mutability handling for `AddStructToListGFF`

**Key Differences:**
- KPatcher: Line 505 has `// FIXME: This is a limitation - in Python it's mutable but in C# it's not`
- PyKotor: Paths are mutable in Python implementation

**Note:** This may require a design change in C# to support mutable paths, possibly using a wrapper class or different data structure.

---

## INI Serialization

### 11. KPatcherINISerializer - HACKList
**File:** `src/KPatcher.Core/Mods/KPatcherINISerializer.cs`  
**Status:** TODO - HACKList serialization  
**PyKotor Reference:** `vendor/PyKotor/Libraries/PyKotor/src/pykotor/kpatcher/mods/ncs.py`

**What's Missing:**
- HACKList (NCS modification) serialization

**Key Differences:**
- KPatcher: Line 78 has `// TODO: Add HACKList (NCS) serialization`
- PyKotor: Full HACKList serialization support

---

## Other Not Implemented Features

### 12. LZMA Helper
**File:** `src/KPatcher.Core/Common/LZMA/LzmaHelper.cs`  
**Status:** Not implemented  
**Note:** This is not a PyKotor port issue, but a missing dependency

**What's Missing:**
- LZMA decompression (for BZF files)
- LZMA compression (for BZF file writing)

**Key Differences:**
- KPatcher: Throws `NotImplementedException` - requires LZMA library (SevenZipSharp, LZMA SDK, or SharpCompress)
- PyKotor: Uses Python's lzma module

---

### 13. LIP Format - XML/JSON
**File:** `src/KPatcher.Core/Formats/LIP/LIPAuto.cs`  
**Status:** Not implemented  
**Note:** May not be in PyKotor vendor reference

**What's Missing:**
- LIP XML format support
- LIP JSON format support

**Key Differences:**
- KPatcher: Throws `NotImplementedException` for XML and JSON formats
- Current: Only binary LIP format is supported

---

### 14. NCS Optimizer - RemoveUnusedGlobalsInStack
**File:** `src/KPatcher.Core/Formats/NCS/Optimizers/RemoveUnusedGlobalsInStackOptimizer.cs`  
**Status:** Not implemented (matches Python)  
**PyKotor Reference:** `vendor/PyKotor/Libraries/PyKotor/src/pykotor/resource/formats/ncs/compiler/optimizers.py`

**What's Missing:**
- RemoveUnusedGlobalsInStack optimizer implementation

**Key Differences:**
- KPatcher: Throws `NotImplementedException` - matches Python which also raises `NotImplementedError`
- PyKotor: Also not implemented (raises `NotImplementedError`)

---

## Summary

### High Priority (Core Format Handlers)
1. **MDLAsciiReader** - Complete ASCII MDL parsing (~2500 lines in PyKotor)
2. **MDLAsciiWriter** - Complete ASCII MDL writing (~2000 lines in PyKotor)
3. **MDLBinaryReader** - Complete binary MDL/MDX parsing (~800 lines in PyKotor)
4. **MDLBinaryWriter** - Complete binary MDL/MDX writing (~500 lines in PyKotor)
5. **TPCBinaryReader** - Format conversions, animated textures, cube maps
6. **TPCBinaryWriter** - Format conversions, animated textures, cube maps

### Medium Priority (Compiler/Interpreter)
7. **Interpreter** - Complete instruction implementation and action queue
8. **NssParser** - String unescaping

### Low Priority (Utilities/Features)
9. **ModifyGFF** - Path mutability workaround
10. **KPatcherINISerializer** - HACKList serialization
11. **MDLAuto** - AABB safety patching and error recovery

### Not PyKotor-Related
12. **LZMA Helper** - Requires external library
13. **LIP XML/JSON** - May not be in PyKotor
14. **RemoveUnusedGlobalsInStackOptimizer** - Also not implemented in PyKotor

---

## Recommendations

1. **Start with MDL format handlers** - These are the most critical and have the largest gaps
2. **Focus on MDLBinaryReader first** - Binary format is more commonly used than ASCII
3. **Port TPC format conversions** - Needed for proper texture handling
4. **Complete Interpreter** - Needed for NCS testing and validation
5. **Address string unescaping** - Simple fix but important for correctness

Each simplified implementation should be ported by:
1. Reading the corresponding PyKotor file
2. Understanding the data structures and algorithms
3. Porting to C# while maintaining the same logic flow
4. Adding appropriate error handling and validation
5. Writing tests to verify parity with PyKotor behavior
