# -*- coding: utf-8 -*-
"""
Port vendor/DeNCS .../node/*.java SableCC classes to C# under src/NCSDecomp.Core/Node/{Declarations,Expressions,Statements}/.
Skips: tokens (T*), Token.java, Node.java, Switch*.java, EOF, AProgram, ACommandBlock (hand-ported), Cast.java (interface -> ICast.cs).
"""
from __future__ import print_function
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
JAVA_DIR = ROOT / "vendor/DeNCS/src/main/java/com/kotor/resource/formats/ncs/node"
OUT_BASE = ROOT / "src/NCSDecomp.Core/Node"

HEADER = """// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Analysis;

namespace NCSDecomp.Core.Node
{
"""

FOOTER = """
}
"""

DECL = {"AProgram", "ASubroutine", "PProgram", "PSubroutine"}

EXPR = {
    "AAddBinaryOp", "ADivBinaryOp", "AEqualBinaryOp", "AExclOrLogiiOp", "AFloatConstant", "AGeqBinaryOp",
    "AGtBinaryOp", "AIncibpStackOp", "AIncispStackOp", "AInclOrLogiiOp", "AIntConstant", "ALeqBinaryOp",
    "ALtBinaryOp", "AModBinaryOp", "AMulBinaryOp", "ANegUnaryOp", "ANequalBinaryOp", "ANonzeroJumpIf",
    "ANotUnaryOp", "AOrLogiiOp", "ARestorebpBpOp", "ASavebpBpOp", "AShleftBinaryOp", "AShrightBinaryOp",
    "ASize", "AStringConstant", "ASubBinaryOp", "AUnrightBinaryOp", "AZeroJumpIf",
    "AAndLogiiOp", "ABitAndLogiiOp", "ACompUnaryOp", "ADecibpStackOp", "ADecispStackOp",
    "PBinaryOp", "PUnaryOp", "PLogiiOp", "PConstant", "PStackOp", "PBpOp", "PSize", "PJumpIf",
}

SKIP_NAMES = {
    "Token", "Node", "Switch", "Switchable", "Cast", "TypedLinkedList", "NodeCast", "NoCast",
    "Start", "XPCmd", "X1PCmd", "X2PCmd", "XPSubroutine", "X1PSubroutine", "X2PSubroutine",
    "AProgram", "ACommandBlock", "EOF",
}


def subfolder(name):
    if name in DECL:
        return "Declarations"
    if name in EXPR:
        return "Expressions"
    if name.startswith("T"):
        return None
    return "Statements"


def pascal_get_set(text):
    text = re.sub(r"public\s+(\S+)\s+get(\w+)\s*\(", r"public \1 Get\2(", text)
    text = re.sub(r"public\s+void\s+set(\w+)\s*\(", r"public void Set\1(", text)
    return text


def port_clone_block(text):
    def repl_node(m):
        body = m.group(1)
        body = re.sub(r"\bthis\.cloneNode\(", "CloneNode(", body)
        body = re.sub(r"\bthis\.cloneList\(", "CloneTypedList(", body)
        body = re.sub(r"\bthis\._", "_", body)
        body = re.sub(r"\breturn\s+new\s+", "return (object)new ", body)
        return "public override object Clone()\n        {\n        " + body.strip() + "\n        }"

    def repl_typed(m):
        body = m.group(2)
        body = re.sub(r"\bthis\.cloneNode\(", "CloneNode(", body)
        body = re.sub(r"\bthis\.cloneList\(", "CloneTypedList(", body)
        body = re.sub(r"\bthis\._", "_", body)
        body = re.sub(r"\breturn\s+new\s+", "return (object)new ", body)
        return "public override object Clone()\n        {\n        " + body.strip() + "\n        }"

    text = re.sub(
        r"@Override\s*\n\s*public\s+Node\s+clone\(\)\s*\{([\s\S]*?)\n\s*\}",
        repl_node,
        text,
    )
    text = re.sub(
        r"@Override\s*\n\s*public\s+(\w+)\s+clone\(\)\s*\{([\s\S]*?)\n\s*\}",
        repl_typed,
        text,
    )
    return text


def port_body(java_inner):
    t = java_inner
    t = port_clone_block(t)
    t = re.sub(r"@Override\s*\n\s*", "", t)
    t = re.sub(r"\bLinkedList<", "TypedLinkedList<", t)
    t = re.sub(r"\bnew\s+LinkedList<", "new TypedLinkedList<", t)
    t = re.sub(r"List<\? extends (\w+)>", r"IEnumerable<\1>", t)
    t = re.sub(r"\bList<(\w+)>\s+list\)", r"IEnumerable<\1> list)", t)
    t = t.replace(".parent(null)", ".SetParent(null)")
    t = t.replace(".parent(this)", ".SetParent(this)")
    t = re.sub(r"\b(\w+)\.parent\(\)", r"\1.Parent()", t)
    t = re.sub(r"\bthis\.cloneNode\(", "CloneNode(", t)
    t = re.sub(r"\bthis\.cloneList\(", "CloneTypedList(", t)
    t = re.sub(r"\bthis\.toString\(", "ToString(", t)
    t = re.sub(r"\bvoid\s+removeChild\(Node", "internal override void RemoveChild(Node", t)
    t = re.sub(r"\bvoid\s+replaceChild\(Node", "internal override void ReplaceChild(Node", t)
    t = re.sub(r"\bpublic\s+void\s+apply\(Switch sw\)", "public override void Apply(Switch sw)", t)
    t = t.replace("((Analysis)sw).case", "((IAnalysis)sw).Case")
    t = t.replace("instanceof", "is")
    t = t.replace("ClassCastException", "InvalidCastException")
    t = re.sub(r"public\s+String\s+toString\(\)", "public override string ToString()", t)
    t = t.replace(".removeChild(", ".RemoveChild(")
    t = re.sub(r"\bthis\.set(\w+)\(", r"this.Set\1(", t)
    t = re.sub(r"\bthis\.get(\w+)\(", r"this.Get\1(", t)
    t = t.replace(".clone()", ".Clone()")
    t = pascal_get_set(t)
    return t


def extract_class_block(raw, class_name):
    m = re.search(
        r"public\s+(?:final\s+|abstract\s+)?class\s+" + re.escape(class_name) + r"\b[\s\S]*?\{",
        raw,
    )
    if not m:
        return None
    start = m.end() - 1  # position of {
    depth = 0
    for i in range(start, len(raw)):
        if raw[i] == "{":
            depth += 1
        elif raw[i] == "}":
            depth -= 1
            if depth == 0:
                return raw[start + 1 : i]
    return None


def class_declaration(raw, class_name):
    m = re.search(
        r"public\s+(final\s+|abstract\s+)?class\s+" + re.escape(class_name) + r"\s+extends\s+(\w+)\s*\{?",
        raw,
    )
    if not m:
        return None
    mod = (m.group(1) or "").strip()
    base = m.group(2)
    if mod == "abstract":
        return "    public abstract class %s : %s" % (class_name, base)
    return "    public sealed class %s : %s" % (class_name, base)


def process_file(path: Path):
    name = path.stem
    if name in SKIP_NAMES:
        return
    if name.startswith("T") and name != "TT":
        return
    folder = subfolder(name)
    if folder is None:
        return

    raw = path.read_text(encoding="utf-8")
    if "implements Cast" in raw or "private class" in raw:
        print("skip (inner/cast):", name)
        return

    inner = extract_class_block(raw, name)
    if inner is None:
        print("skip (parse):", name)
        return

    decl = class_declaration(raw, name)
    if not decl:
        print("skip (decl):", name)
        return

    inner_cs = port_body(inner)
    # indent inner 8 spaces
    ilines = []
    for line in inner_cs.splitlines():
        if line.strip() == "":
            ilines.append("")
        else:
            ilines.append("        " + line)
    body = "\n".join(ilines)

    out_dir = OUT_BASE / folder
    out_dir.mkdir(parents=True, exist_ok=True)
    out_path = out_dir / (name + ".cs")
    out_path.write_text(HEADER + decl + "\n    {\n" + body + "\n    }\n" + FOOTER, encoding="utf-8")
    print("ok", out_path.relative_to(ROOT))


def main():
    for p in sorted(JAVA_DIR.glob("*.java")):
        process_file(p)


if __name__ == "__main__":
    main()
