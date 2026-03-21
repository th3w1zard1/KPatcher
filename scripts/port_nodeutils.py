# One-off: DeNCS NodeUtils.java -> NCSDecomp NodeUtils.cs (mechanical)
from __future__ import annotations

import pathlib
import re

j = pathlib.Path(r"vendor/DeNCS/src/main/java/com/kotor/resource/formats/ncs/utils/NodeUtils.java").read_text(encoding="utf-8")
body = re.sub(r"(?s)^.*?public final class NodeUtils \{", "", j)
body = body.strip().rstrip("}").rstrip()
body = body.replace("public static", "public static")
body = body.replace("com.kotor.resource.formats.ncs.ActionsData", "ActionsData")
body = body.replace("import ", "// import ")
# remove java imports block remnants
body = re.sub(r"// import .*?\n", "", body)
body = body.replace("Type", "DecompType")
body = body.replace("DecompType.parseType", "DecompType.ParseType")
body = body.replace("new DecompType(", "new DecompType(")
body = body.replace("Byte.parseByte", "SByteParse")
body = body.replace("Integer.parseInt", "int.Parse")
body = body.replace("Long.parseLong", "long.Parse")
body = body.replace("Float.parseFloat", "float.Parse")
body = body.replace("RuntimeException", "InvalidOperationException")
body = body.replace("String", "string")
body = body.replace("boolean", "bool")
body = body.replace(".class.isInstance(", " is ")
body = body.replace(" instanceof ", " is ")
body = body.replace("node.get", "node.Get")
body = body.replace("((A", "((")
# fix botched
body = re.sub(r"(\w+) is (\w+)\)", r"\1 is \2)", body)
# Actually java isInstance is wrong direction for C# - manual fix needed
print("SCRIPT_INCOMPLETE")
