# Convert vendor SubScriptState.java -> C# (mechanical; fix compile errors after dotnet build).
import pathlib
import re

root = pathlib.Path(__file__).resolve().parents[1]
java_path = (
    root
    / "vendor/DeNCS/src/main/java/com/kotor/resource/formats/ncs/scriptutils/SubScriptState.java"
)
out_path = root / "src/NCSDecomp.Core/ScriptUtils/SubScriptState.cs"

text = java_path.read_text(encoding="utf-8")
m = re.search(r"public class SubScriptState\s*\{", text)
if not m:
    raise SystemExit("class not found")
body = text[m.end() :].rstrip()
if body.endswith("}"):
    body = body[:-1].rstrip()

body = body.replace("\r\n", "\n")

# Strip @Override and SuppressWarnings lines
lines = []
for line in body.split("\n"):
    s = line.strip()
    if s.startswith("@Override") or s.startswith("@SuppressWarnings"):
        continue
    lines.append(line)
body = "\n".join(lines)

header = """// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS SubScriptState.java (mechanical conversion + C# adjustments).

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using AstNode = global::NCSDecomp.Core.Node.Node;
using NCSDecomp.Core;
using NCSDecomp.Core.Node;
using NCSDecomp.Core.ScriptNode;
using NCSDecomp.Core.Stack;
using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core.ScriptUtils
{
    public class SubScriptState
    {
"""

footer = "\n    }\n}\n"


def sub_many(s, pairs):
    for a, b in pairs:
        s = s.replace(a, b)
    return s


body = sub_many(
    body,
    [
        ("Logger.trace", "SubScriptLogger.Trace"),
        ("System.out.println", "SubScriptLogger.Trace"),
        ("StringBuffer", "StringBuilder"),
        ("throw new RuntimeException", "throw new InvalidOperationException"),
        ("new RuntimeException", "new InvalidOperationException"),
        ("new Type(", "new DecompType("),
        ("Integer.toHexString(nodePos)", 'nodePos.ToString("X")'),
        ("catch (RuntimeException", "catch (Exception"),
        ("Hashtable<Variable, AVarDecl>", "Hashtable"),
        ("Hashtable<Type, Integer>", "Hashtable"),
        ("Hashtable<String, Integer>", "Hashtable"),
        ("new Hashtable<>(1)", "new Hashtable()"),
        ("new Hashtable(1)", "new Hashtable()"),
        ("Vector<Variable>", "List<Variable>"),
        ("ArrayList<", "List<"),
        ("new ArrayList<>()", "new List<ScriptNode>()"),
        ("new ArrayList<ScriptNode>(", "new List<ScriptNode>("),
        ("new ArrayList<AExpressionStatement>(", "new List<AExpressionStatement>("),
    ],
)

body = re.sub(r"Byte\.toString\(\s*(\w+)\s*\)", r"\1.ToString()", body)

# Vector init from keySet
body = re.sub(
    r"List<Variable> vars = new Vector<>\(this\.vardecs\.keySet\(\)\);",
    "var vars = new List<Variable>();\n      foreach (Variable __vk in this.vardecs.Keys)\n         vars.Add(__vk);",
    body,
)
body = body.replace("new Vector<>(this.vardecs.keySet())", "new List<Variable>() /*filled below*/")

body = re.sub(r"\bAExpression\b", "IAExpression", body)
body = body.replace("IAExpressionStatement", "AExpressionStatement")

# Node -> AstNode (whole word)
body = re.sub(r"(?<![\w.])Node(?![\w])", "AstNode", body)

# .class.isInstance(x) -> x is T  (pattern: X.class.isInstance(y))
body = re.sub(
    r"(\w+)\.class\.isInstance\(\s*([^)]+?)\s*\)",
    r"(\2 is \1)",
    body,
)

# instanceof
body = re.sub(r"(\w+)\s+instanceof\s+(\w+)", r"\2 \1 = \1 as \2", body)
# Fix broken double declarations — Java used inline; C# needs if — skip, do manual for instanceof

# Revert broken instanceof line — too risky; undo generic instanceof replace
# (not applied if complex)

for a, b in [
    (".getPos(", ".GetPos("),
    (".getDestination(", ".GetDestination("),
    ("protostate.type()", "protostate.Type()"),
    ("protostate.getId()", "protostate.GetId()"),
    ("protostate.getStart()", "protostate.GetStart()"),
    ("protostate.getEnd()", "protostate.GetEnd()"),
    ("protostate.getParamCount()", "protostate.GetParamCount()"),
    ("this.subdata.getState(", "this.subdata.GetState("),
    ("this.subdata.getGlobalStack()", "this.subdata.GetGlobalStack()"),
    ("this.subdata.globalState()", "this.subdata.GlobalState()"),
    ("this.subdata.addStruct(", "this.subdata.AddStruct("),
]:
    body = body.replace(a, b)

nu = [
    ("NodeUtils.getPreviousCommand", "NodeUtils.GetPreviousCommand"),
    ("NodeUtils.isJzPastOne", "NodeUtils.IsJzPastOne"),
    ("NodeUtils.isJz", "NodeUtils.IsJz"),
    ("NodeUtils.stackOffsetToPos", "NodeUtils.StackOffsetToPos"),
    ("NodeUtils.stackSizeToPos", "NodeUtils.StackSizeToPos"),
    ("NodeUtils.getNextCommand", "NodeUtils.GetNextCommand"),
    ("NodeUtils.getOp", "NodeUtils.GetOp"),
    ("NodeUtils.isArithmeticOp", "NodeUtils.IsArithmeticOp"),
    ("NodeUtils.isConditionalOp", "NodeUtils.IsConditionalOp"),
    ("NodeUtils.getReturnType", "NodeUtils.GetReturnType"),
    ("NodeUtils.getActionName", "NodeUtils.GetActionName"),
    ("NodeUtils.getActionId", "NodeUtils.GetActionId"),
    ("NodeUtils.getActionParamTypes", "NodeUtils.GetActionParamTypes"),
    ("NodeUtils.getActionParamCount", "NodeUtils.GetActionParamCount"),
    ("NodeUtils.isReturn", "NodeUtils.IsReturn"),
    ("NodeUtils.isGlobalStackOp", "NodeUtils.IsGlobalStackOp"),
]
for a, b in nu:
    body = body.replace(a, b)

body = body.replace("this.stack.size()", "this.stack.Size()")
body = re.sub(r"\bstack\.get\(", "stack.Get(", body)
body = body.replace("this.stack.get(", "this.stack.Get(")

for a, b in [
    (".varstruct()", ".Varstruct()"),
    (".isStruct()", ".IsStruct()"),
    (".isParam()", ".IsParam()"),
    (".isAssigned()", ".IsAssigned()"),
    (".isFcnReturn()", ".IsFcnReturn()"),
    (".assigned()", ".Assigned()"),
    (".typeSize()", ".TypeSize()"),
    (".getClass().getSimpleName()", ".GetType().Name"),
    (".hasChildren()", ".HasChildren()"),
    (".getLastChild()", ".GetLastChild()"),
    (".getFirstCase()", ".GetFirstCase()"),
    (".getLastCase()", ".GetLastCase()"),
    (".getNextCase(", ".GetNextCase("),
    (".getFirstCaseStart()", ".GetFirstCaseStart()"),
    (".addChild(", ".AddChild("),
    (".addChildren(", ".AddChildren("),
    (".removeLastChild()", ".RemoveLastChild()"),
    (".removeChild(", ".RemoveChild("),
    (".getChildLocation(", ".GetChildLocation("),
    (".replaceChild(", ".ReplaceChild("),
    (".getEnd()", ".GetEnd()"),
    (".getStart()", ".GetStart()"),
    (".end(", ".End("),
    (".switchExp(", ".SwitchExp("),
    (".switchExp()", ".SwitchExp()"),
    (".condition(", ".Condition("),
    (".condition()", ".Condition()"),
    (".expression()", ".Expression()"),
    (".stackentry()", ".Stackentry()"),
    (".stackentry(", ".Stackentry("),
    (".initializeExp(", ".InitializeExp("),
    (".removeExp(", ".RemoveExp("),
    (".exp()", ".Exp()"),
    (".var(", ".Var("),
    (".var()", ".Var()"),
    (".action()", ".Action()"),
    (".getParam(", ".GetParam("),
    (".getUnknowns()", ".GetUnknowns()"),
    (".replaceUnknown(", ".ReplaceUnknown("),
    (".getDestination()", ".GetDestination()"),
    (".chooseStructElement(", ".ChooseStructElement("),
    (".getBody()", ".GetBody()"),
    (".getHeader()", ".GetHeader()"),
    (".isMain(", ".IsMain("),
    (".isMain()", ".IsMain()"),
    (".name(", ".Name("),
    (".name()", ".Name()"),
    (".getRoot()", ".GetRoot()"),
    (".getProto()", ".GetProto()"),
    (".getVariables()", ".GetVariables()"),
    (".setName(", ".SetName("),
    (".setVarPrefix(", ".SetVarPrefix("),
    (".setStack(", ".SetStack("),
    (".parseDone(", ".ParseDone("),
    (".parent(", ".Parent("),
    (".parent()", ".Parent()"),
    (".close(", ".Close("),
    (".toString(", ".ToString("),
    (".getId()", ".GetId()"),
]:
    body = body.replace(a, b)

# Variable / DecompType .Type() from .type() — remaining param.type, var.type etc.
body = re.sub(r"\b(\w+)\.type\(\)", r"\1.Type()", body)
# root.type() already .Type()
body = body.replace("protostate.Type()", "protostate.Type()")

# Hashtable get/put/remove/keys
body = re.sub(r"this\.vardecs\.get\(\s*([^)]+)\s*\)", r"(AVarDecl)this.vardecs[\1]", body)
body = re.sub(
    r"this\.vardecs\.put\(\s*([^,]+)\s*,\s*([^)]+)\s*\)",
    r"this.vardecs[\1] = \2",
    body,
)
body = re.sub(r"this\.vardecs\.remove\(\s*([^)]+)\s*\)", r"this.vardecs.Remove(\1)", body)

body = re.sub(r"this\.varcounts\.get\(\s*([^)]+)\s*\)", r"(Integer)this.varcounts[\1]", body)
body = re.sub(
    r"this\.varcounts\.put\(\s*([^,]+)\s*,\s*Integer\.valueOf\(\s*([^)]+)\s*\)\s*\)",
    r"this.varcounts[\1] = \2",
    body,
)
body = re.sub(
    r"this\.varcounts\.put\(\s*([^,]+)\s*,\s*([^)]+)\s*\)",
    r"this.varcounts[\1] = \2",
    body,
)

body = body.replace("Integer.valueOf(1)", "1")
body = body.replace("(Integer)", "(int)")
body = body.replace("Integer ", "int ")
body = re.sub(
    r"int (\w+) = \(int\)this\.varcounts\[", r"object __o = this.varcounts[", body
)  # too hacky

# Simpler: Java Integer -> object box in Hashtable; use int unbox
body = body.replace("(int)this.varcounts[key]", "(int)this.varcounts[key]")
body = body.replace(
    "int curcount = (int)this.varcounts[key]",
    "object __cur = this.varcounts[key];\n         int curcount = __cur != null ? (int)__cur : 0",
)

# Fix varcounts get pattern manually after build

body = body.replace("this.varnames.containsKey(", "this.varnames.ContainsKey(")
body = re.sub(
    r"this\.varnames\.put\(\s*([^,]+)\s*,\s*Integer\.valueOf\(\s*1\s*\)\s*\)",
    r"this.varnames[\1] = 1",
    body,
)
body = re.sub(r"this\.varnames\.put\(\s*([^,]+)\s*,\s*1\s*\)", r"this.varnames[\1] = 1", body)

# parseDone Enumeration
body = re.sub(
    r"Enumeration<Variable> en = this\.vardecs\.keys\(\);\s*\n\s*while \(en\.hasMoreElements\(\)\) \{\s*\n\s*Variable var = en\.nextElement\(\);",
    "foreach (DictionaryEntry __de in this.vardecs)\n         {\n            Variable var = (Variable)__de.Key;",
    body,
)

# close Enumeration
body = re.sub(
    r"Enumeration<Variable> en = this\.vardecs\.keys\(\);\s*\n\s*while \(en\.hasMoreElements\(\)\) \{\s*\n\s*Variable var = en\.nextElement\(\);\s*\n\s*var\.Close\(\);",
    "foreach (DictionaryEntry __de in this.vardecs)\n         {\n            Variable var = (Variable)__de.Key;\n            var.Close();",
    body,
)

# public API
body = body.replace("public String ToString()", "public override string ToString()")
body = body.replace("public void close()", "public void Close()")
body = body.replace("private void checkStart", "private void CheckStart")
body = body.replace("private void checkEnd", "private void CheckEnd")
body = body.replace("checkStart(", "CheckStart(")
body = body.replace("checkEnd(", "CheckEnd(")
body = body.replace("private void assertState", "private void AssertState")
body = body.replace("assertState(", "AssertState(")

# super -> base for ScriptNode closes in Java there is no super for SubScriptState except none
# Java methods: transform* keep names PascalCase? User C# style — rename public transform to Transform
for name in [
    "transformPlaceholderVariableRemoved",
    "transformMoveSPVariablesRemoved",
    "transformEndDoLoop",
    "transformOriginFound",
    "transformLogOrExtraJump",
    "transformConditionalJump",
    "transformJump",
    "transformJSR",
    "transformAction",
    "transformReturn",
    "transformCopyDownSp",
    "transformCopyTopSp",
    "transformCopyDownBp",
    "transformCopyTopBp",
    "transformMoveSp",
    "transformRSAdd",
    "transformConst",
    "transformLogii",
    "transformBinary",
    "transformUnary",
    "transformStack",
    "transformDestruct",
    "transformBp",
    "transformStoreState",
    "transformDeadCode",
    "emitError",
    "inActionArg",
    "atLastCommand",
    "isMiddleOfReturn",
    "currentContainsVars",
    "getReturnExp",
    "setVarStructName",
]:
    pascal = name[0].upper() + name[1:]
    body = body.replace(name + "(", pascal + "(")

# fix double TransformTransform
body = body.replace("TransformTransform", "Transform")

# Iterator foreach for getVariables TreeSet
body = body.replace("TreeSet<VarStruct>", "SortedSet<VarStruct>")
body = body.replace("new TreeSet<>()", "new SortedSet<VarStruct>()")  # invalid

# Manual fix TreeSet -> List + Sort by name
body = sub_many(
    body,
    [
        (
            "SortedSet<VarStruct> varstructs = new SortedSet<VarStruct>();",
            "var varstructs = new List<VarStruct>();",
        ),
        ("Iterator<Variable> it = vars.iterator();", ""),
        ("while (it.hasNext())", "foreach (Variable var in new List<Variable>(vars))"),
    ],
)

out_path.write_text(header + body + footer, encoding="utf-8")
print("wrote", out_path)
