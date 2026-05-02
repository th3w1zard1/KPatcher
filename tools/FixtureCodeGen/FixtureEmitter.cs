using System.Text;
using KPatcher.Core.Common.Capsule;
using KPatcher.Core.Resources;

namespace FixtureCodeGen;

internal static class FixtureEmitter
{
	private static readonly HashSet<string> GffExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		".are", ".bic", ".dlg", ".git", ".ifo", ".jrl", ".pth", ".utc", ".utd", ".ute", ".uti", ".utm", ".utp", ".uts", ".utt", ".utw", ".gff",
	};

	internal static int Run(string[] args)
	{
		if (args.Length < 2)
		{
			Console.Error.WriteLine("Usage: dotnet run -- <caseId> <className> [outputFileNameWithoutExtension]");
			return 1;
		}

		string caseId = args[0];
		string className = args[1];
		string outputFileName = args.Length >= 3 ? args[2] : args[1];

		string repoRoot = FindRepoRoot();
		string fixtureRoot = Path.Combine(repoRoot, "tests", "KPatcher.Tests", "test_files", "integration_tslpatcher_mods", "fixtures", caseId, "tslpatchdata");
		string outputPath = Path.Combine(repoRoot, "tests", "KPatcher.Tests", "Integration", "Generated", outputFileName + ".g.cs");

		if (!Directory.Exists(fixtureRoot))
		{
			Console.Error.WriteLine("Missing fixture root: " + fixtureRoot);
			return 1;
		}

		Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
		File.WriteAllText(outputPath, GenerateFixtureClass(caseId, className, fixtureRoot), new UTF8Encoding(false));
		Console.WriteLine("Generated: " + outputPath);
		return 0;
	}

	private static string GenerateFixtureClass(string caseId, string className, string fixtureRoot)
	{
		List<FixtureFile> files = Directory.EnumerateFiles(fixtureRoot, "*", SearchOption.AllDirectories)
			.Select(path => FixtureFile.FromPath(fixtureRoot, path))
			.OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
			.ToList();

		var sb = new StringBuilder();
		sb.AppendLine("using System;");
		sb.AppendLine("using System.Collections.Generic;");
		sb.AppendLine("using System.IO;");
		sb.AppendLine("using System.Linq;");
		sb.AppendLine("using KPatcher.Core.Common;");
		sb.AppendLine("using KPatcher.Core.Formats.ERF;");
		sb.AppendLine("using KPatcher.Core.Formats.GFF;");
		sb.AppendLine("using KPatcher.Core.Resources;");
		sb.AppendLine();
		sb.AppendLine("namespace KPatcher.Core.Tests.Integration");
		sb.AppendLine("{");
		sb.AppendLine($"    internal static class {className}");
		sb.AppendLine("    {");
		sb.AppendLine($"        internal const string CaseId = \"{EscapeString(caseId)}\";");
		sb.AppendLine();
		sb.AppendLine("        internal static void Materialize(string modRoot, string tslRoot)");
		sb.AppendLine("        {");
		sb.AppendLine("            _ = modRoot;");
		sb.AppendLine("            Directory.CreateDirectory(tslRoot);");

		foreach (FixtureFile file in files)
		{
			string relativeLiteral = EscapeString(file.RelativePath.Replace('/', '\\'));
			if (file.IsChangesIni)
			{
				sb.AppendLine($"            WriteTextFile(Path.Combine(tslRoot, \"{relativeLiteral}\"), {file.Identifier}Text);");
			}
			else if (file.IsModule)
			{
				sb.AppendLine($"            Write{file.Identifier}Module(Path.Combine(tslRoot, \"{relativeLiteral}\"));");
			}
			else if (file.IsGff)
			{
				sb.AppendLine($"            WriteGffFile(Path.Combine(tslRoot, \"{relativeLiteral}\"), {file.Identifier}Bytes);");
			}
			else
			{
				sb.AppendLine($"            WriteBinaryFile(Path.Combine(tslRoot, \"{relativeLiteral}\"), {file.Identifier}Bytes);");
			}
		}

		sb.AppendLine("        }");
		sb.AppendLine();

		List<FixtureFile> moduleFiles = files.Where(file => file.IsModule).ToList();
		if (moduleFiles.Count == 1)
		{
			sb.AppendLine($"        internal static IReadOnlyList<(string ResName, ResourceType ResType)> SourceModuleResources => Array.AsReadOnly(ERFAuto.ReadErf({moduleFiles[0].Identifier}ModuleBytes).Select(resource => (resource.ResRef.ToString(), resource.ResType)).ToArray());");
			sb.AppendLine();
		}

		foreach (FixtureFile file in files.Where(file => !file.IsModule && !file.IsChangesIni))
		{
			sb.AppendLine($"        internal static byte[] {file.Identifier}Bytes => _{CamelCase(file.Identifier)}Bytes;");
		}

		foreach (FixtureFile moduleFile in moduleFiles)
		{
			sb.AppendLine($"        internal static byte[] {moduleFile.Identifier}ModuleBytes => _{CamelCase(moduleFile.Identifier)}ModuleBytes;");
		}

		if (files.Any(file => !file.IsModule && !file.IsChangesIni) || moduleFiles.Count > 0)
		{
			sb.AppendLine();
		}

		sb.AppendLine("        private static void WriteTextFile(string path, string text)");
		sb.AppendLine("        {");
		sb.AppendLine("            string directory = Path.GetDirectoryName(path);");
		sb.AppendLine("            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);");
		sb.AppendLine("            File.WriteAllText(path, text, new System.Text.UTF8Encoding(false));");
		sb.AppendLine("        }");
		sb.AppendLine();
		sb.AppendLine("        private static void WriteBinaryFile(string path, byte[] bytes)");
		sb.AppendLine("        {");
		sb.AppendLine("            string directory = Path.GetDirectoryName(path);");
		sb.AppendLine("            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);");
		sb.AppendLine("            File.WriteAllBytes(path, bytes);");
		sb.AppendLine("        }");
		sb.AppendLine();
		sb.AppendLine("        private static void WriteGffFile(string path, byte[] bytes)");
		sb.AppendLine("        {");
		sb.AppendLine("            string directory = Path.GetDirectoryName(path);");
		sb.AppendLine("            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);");
		sb.AppendLine("            File.WriteAllBytes(path, GFF.FromBytes(bytes).ToBytes());");
		sb.AppendLine("        }");
		sb.AppendLine();

		foreach (FixtureFile moduleFile in moduleFiles)
		{
			sb.AppendLine($"        private static void Write{moduleFile.Identifier}Module(string path)");
			sb.AppendLine("        {");
					sb.AppendLine("            string directory = Path.GetDirectoryName(path);");
			sb.AppendLine("            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);");
			sb.AppendLine($"            ERF mod = ERFAuto.ReadErf({moduleFile.Identifier}ModuleBytes);");
			sb.AppendLine("            mod.ErfType = ERFType.MOD;");
			sb.AppendLine("            mod.IsSaveErf = true;");
			sb.AppendLine("            File.WriteAllBytes(path, new ERFBinaryWriter(mod).Write());");
			sb.AppendLine("        }");
			sb.AppendLine();
		}

		foreach (FixtureFile file in files)
		{
			if (file.IsChangesIni)
			{
				AppendStringConstant(sb, file.Identifier + "Text", File.ReadAllText(file.FullPath));
			}
			else if (!file.IsModule)
			{
				AppendByteArrayLiteral(sb, "_" + CamelCase(file.Identifier) + "Bytes", File.ReadAllBytes(file.FullPath));
			}
			else
			{
				AppendByteArrayLiteral(sb, "_" + CamelCase(file.Identifier) + "ModuleBytes", File.ReadAllBytes(file.FullPath));
			}
			sb.AppendLine();
		}

		sb.AppendLine("    }");
		sb.AppendLine("}");
		return sb.ToString();
	}

	private static void AppendStringConstant(StringBuilder sb, string name, string value)
	{
		sb.AppendLine($"        private const string {name} =");
		string normalized = value.Replace("\r\n", "\n");
		string[] lines = normalized.Split('\n');
		for (int index = 0; index < lines.Length; index++)
		{
			string suffix = index == lines.Length - 1 ? string.Empty : "\\r\\n";
			string terminator = index == lines.Length - 1 ? ";" : " +";
			sb.AppendLine($"            \"{EscapeString(lines[index])}{suffix}\"{terminator}");
		}
	}

	private static void AppendBase64Constant(StringBuilder sb, string name, byte[] data)
	{
		sb.AppendLine($"        private const string {name} =");
		string base64 = Convert.ToBase64String(data);
		const int chunkSize = 120;
		for (int index = 0; index < base64.Length; index += chunkSize)
		{
			int length = Math.Min(chunkSize, base64.Length - index);
			string chunk = base64.Substring(index, length);
			string terminator = index + length >= base64.Length ? ";" : " +";
			sb.AppendLine($"            \"{chunk}\"{terminator}");
		}
	}

	private static void AppendByteArrayLiteral(StringBuilder sb, string name, byte[] data)
	{
		sb.AppendLine($"        private static readonly byte[] {name} = new byte[]");
		sb.AppendLine("        {");
		const int valuesPerLine = 24;
		for (int index = 0; index < data.Length; index += valuesPerLine)
		{
			int length = Math.Min(valuesPerLine, data.Length - index);
			var chunk = new StringBuilder("            ");
			for (int offset = 0; offset < length; offset++)
			{
				if (offset > 0)
				{
					chunk.Append(' ');
				}
				chunk.Append("0x");
				chunk.Append(data[index + offset].ToString("X2"));
				chunk.Append(',');
			}
			sb.AppendLine(chunk.ToString());
		}
		sb.AppendLine("        };");
	}

	private static string GetResourceTypeLiteral(string extension)
	{
		return extension.Equals("2da", StringComparison.OrdinalIgnoreCase) ? "TwoDA" : extension.ToUpperInvariant();
	}

	private static string EscapeString(string value)
	{
		return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
	}

	private static string ToPascalCase(string value)
	{
		var sb = new StringBuilder();
		bool upper = true;
		foreach (char ch in value)
		{
			if (!char.IsLetterOrDigit(ch))
			{
				upper = true;
				continue;
			}
			sb.Append(upper ? char.ToUpperInvariant(ch) : ch);
			upper = false;
		}
		return sb.Length == 0 ? "Value" : sb.ToString();
	}

	private static string CamelCase(string value)
	{
		return string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value.Substring(1);
	}

	private static string FindRepoRoot()
	{
		DirectoryInfo? current = new DirectoryInfo(Directory.GetCurrentDirectory());
		while (current != null)
		{
			if (File.Exists(Path.Combine(current.FullName, "KPatcher.sln"))) return current.FullName;
			current = current.Parent;
		}
		throw new DirectoryNotFoundException("Unable to locate KPatcher.sln from current directory.");
	}

	private sealed class FixtureFile
	{
		public string FullPath { get; private set; } = string.Empty;
		public string RelativePath { get; private set; } = string.Empty;
		public string Identifier { get; private set; } = string.Empty;
		public bool IsChangesIni { get; private set; }
		public bool IsModule { get; private set; }
		public bool IsGff { get; private set; }
		public List<FixtureModuleResource> ModuleResources { get; private set; } = new List<FixtureModuleResource>();

		public static FixtureFile FromPath(string fixtureRoot, string fullPath)
		{
			string relativePath = Path.GetRelativePath(fixtureRoot, fullPath).Replace('\\', '/');
			string extension = Path.GetExtension(fullPath);
			bool isModule = extension.Equals(".mod", StringComparison.OrdinalIgnoreCase);
			var file = new FixtureFile
			{
				FullPath = fullPath,
				RelativePath = relativePath,
				Identifier = ToPascalCase(relativePath),
				IsChangesIni = relativePath.Equals("changes.ini", StringComparison.OrdinalIgnoreCase),
				IsModule = isModule,
				IsGff = GffExtensions.Contains(extension),
			};

			if (isModule)
			{
				var capsule = new Capsule(fullPath, createIfNotExist: false);
				var usedIdentifiers = new Dictionary<string, int>(StringComparer.Ordinal);
				file.ModuleResources = capsule.OrderBy(resource => resource.ResName, StringComparer.OrdinalIgnoreCase)
					.ThenBy(resource => resource.ResType.Extension, StringComparer.OrdinalIgnoreCase)
					.Select(resource => FixtureModuleResource.FromResource(file.Identifier, resource, usedIdentifiers))
					.ToList();
			}

			return file;
		}
	}

	private sealed class FixtureModuleResource
	{
		public string Identifier { get; private set; } = string.Empty;
		public string ResName { get; private set; } = string.Empty;
		public ResourceType ResType { get; private set; } = ResourceType.INVALID;
		public byte[] Data { get; private set; } = Array.Empty<byte>();

		public static FixtureModuleResource FromResource(
			string parentIdentifier,
			CapsuleResource resource,
			Dictionary<string, int> usedIdentifiers)
		{
			string baseIdentifier = parentIdentifier + ToPascalCase(resource.ResName + "_" + resource.ResType.Extension);
			usedIdentifiers.TryGetValue(baseIdentifier, out int collisions);
			usedIdentifiers[baseIdentifier] = collisions + 1;
			string identifier = collisions == 0 ? baseIdentifier : baseIdentifier + (collisions + 1).ToString();

			return new FixtureModuleResource
			{
				Identifier = identifier,
				ResName = resource.ResName,
				ResType = resource.ResType,
				Data = resource.Data,
			};
		}
	}
}