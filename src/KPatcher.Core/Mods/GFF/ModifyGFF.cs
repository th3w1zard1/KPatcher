using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using KPatcher.Core.Common;
using KPatcher.Core.Formats.GFF;
using KPatcher.Core.Logger;
using KPatcher.Core.Memory;
using KPatcher.Core.Resources;

namespace KPatcher.Core.Mods.GFF
{

    /// <summary>
    /// GFF modification algorithms for KPatcher/KPatcher.
    ///
    /// This module implements GFF field modification logic for applying patches from changes.ini files.
    /// Handles field additions, modifications, list operations, and struct manipulations.
    ///
    /// References:
    /// ----------
    ///     vendor/KPatcher/KPatcher.pl - Perl GFF modification logic (broken and unfinished)
    ///     vendor/Kotor.NET/Kotor.NET.Patcher/ - Incomplete C# patcher
    /// </summary>

    /// <summary>
    /// Helper function to set localized string field.
    /// def set_locstring(struct: GFFStruct, label: str, value: LocalizedStringDelta, memory: PatcherMemory)
    /// </summary>
    internal static class GFFHelpers
    {
        internal static void SetLocString(GFFStruct struct_, string label, LocalizedStringDelta value, PatcherMemory memory)
        {
            LocalizedString original = new LocalizedString(0);
            value.Apply(original, memory);
            struct_.SetLocString(label, original);
        }
    }

    /// <summary>
    /// Abstract base for GFF modifications.
    /// </summary>
    public abstract class ModifyGFF
    {
        public abstract void Apply(object rootContainer, PatcherMemory memory, PatchLogger logger, Game game = Game.K1);

        protected static void SetFieldValue(GFFStruct gffStruct, string label, object value, GFFFieldType fieldType, PatcherMemory memory)
        {
            switch (fieldType)
            {
                case GFFFieldType.UInt8:
                    gffStruct.SetUInt8(label, Convert.ToByte(value));
                    break;
                case GFFFieldType.Int8:
                    gffStruct.SetInt8(label, Convert.ToSByte(value));
                    break;
                case GFFFieldType.UInt16:
                    gffStruct.SetUInt16(label, Convert.ToUInt16(value));
                    break;
                case GFFFieldType.Int16:
                    gffStruct.SetInt16(label, Convert.ToInt16(value));
                    break;
                case GFFFieldType.UInt32:
                    gffStruct.SetUInt32(label, Convert.ToUInt32(value));
                    break;
                case GFFFieldType.Int32:
                    gffStruct.SetInt32(label, Convert.ToInt32(value));
                    break;
                case GFFFieldType.UInt64:
                    gffStruct.SetUInt64(label, Convert.ToUInt64(value));
                    break;
                case GFFFieldType.Int64:
                    gffStruct.SetInt64(label, Convert.ToInt64(value));
                    break;
                case GFFFieldType.Single:
                    gffStruct.SetSingle(label, Convert.ToSingle(value));
                    break;
                case GFFFieldType.Double:
                    gffStruct.SetDouble(label, Convert.ToDouble(value));
                    break;
                case GFFFieldType.String:
                    gffStruct.SetString(label, value.ToString() ?? "");
                    break;
                case GFFFieldType.ResRef:
                    gffStruct.SetResRef(label, value as ResRef ?? ResRef.FromBlank());
                    break;
                case GFFFieldType.LocalizedString:
                    if (value is LocalizedStringDelta delta)
                    {
                        GFFHelpers.SetLocString(gffStruct, label, delta, memory);
                    }
                    else if (value is LocalizedString locString)
                    {
                        gffStruct.SetLocString(label, locString);
                    }
                    break;
                case GFFFieldType.Vector3:
                    if (value is Vector3 v3)
                    {
                        gffStruct.SetVector3(label, v3);
                    }
                    break;
                case GFFFieldType.Vector4:
                    if (value is Vector4 v4)
                    {
                        gffStruct.SetVector4(label, v4);
                    }
                    break;
                case GFFFieldType.List:
                    if (value is GFFList list)
                    {
                        gffStruct.SetList(label, list);
                    }
                    break;
                case GFFFieldType.Struct:
                    if (value is GFFStruct @struct)
                    {
                        gffStruct.SetStruct(label, @struct);
                    }
                    break;
            }
        }

        /// <summary>
        /// Navigates through gff lists/structs to find the specified path.
        ///
        /// Args:
        /// ----
        ///     root_container (GFFStruct): The root container to start navigation
        ///
        /// Returns:
        /// -------
        ///     container (GFFList | GFFStruct | None): The container at the end of the path or None if not found
        ///
        /// Processing Logic:
        /// ----------------
        ///     - Validates and walks each path segment
        ///     - Loops through each part of the path
        ///     - Acquires the container at each step from the parent container
        ///     - Returns the container at the end or None if not found along the path
        /// </summary>
        [CanBeNull]
        protected static object NavigateContainers(GFFStruct rootContainer, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return rootContainer;
            }

            string[] parts = path.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            // Can be null if not found
            object container = rootContainer;

            foreach (string step in parts)
            {
                // Skip >>##INDEXINLIST##<< sentinel - it's not a real path component
                if (step == ">>##INDEXINLIST##<<")
                {
                    continue;
                }

                if (container is GFFStruct gffStruct)
                {
                    // Try struct first, then list
                    // Can be null if struct not found
                    if (gffStruct.TryGetStruct(step, out GFFStruct childStruct))
                    {
                        container = childStruct;
                    }
                    // Can be null if list not found
                    else if (gffStruct.TryGetList(step, out GFFList childList))
                    {
                        container = childList;
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (container is GFFList gffList)
                {
                    if (int.TryParse(step, out int index))
                    {
                        container = gffList.At(index);
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }

            return container;
        }

        /// <summary>
        /// Helper method to split a path into parent path and label (filename).
        /// Handles both backslashes and forward slashes correctly on all platforms.
        /// </summary>
        protected static (string parentPath, string label) SplitPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return ("", "");
            }

            string[] parts = path.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return ("", "");
            }

            if (parts.Length == 1)
            {
                return ("", parts[0]);
            }

            string label = parts[parts.Length - 1];
            string parentPath = string.Join("\\", parts, 0, parts.Length - 1);
            return (parentPath, label);
        }

        protected static string CombinePath(string basePath, string childPath)
        {
            string trimmedBase = string.IsNullOrEmpty(basePath) ? "" : basePath.TrimEnd('\\', '/');
            string trimmedChild = string.IsNullOrEmpty(childPath) ? "" : childPath.Trim('\\', '/');
            if (string.IsNullOrEmpty(trimmedBase))
            {
                return trimmedChild;
            }
            if (string.IsNullOrEmpty(trimmedChild))
            {
                return trimmedBase;
            }
            return $"{trimmedBase}/{trimmedChild}";
        }

        /// <summary>
        /// Navigates to a field from the root gff struct from a path.
        /// Returns a tuple of (fieldType, value) or null if not found
        /// </summary>
        [CanBeNull]
        protected (GFFFieldType fieldType, object value)? NavigateToField(GFFStruct rootContainer, string path)
        {
            (string parentPath, string label) = SplitPath(path);
            // Can be null if not found
            object container = NavigateContainers(rootContainer, parentPath);

            if (container is GFFStruct gffStruct)
            {
                // Access field type and value - here we use TryGetFieldType and GetValue
                if (gffStruct.TryGetFieldType(label, out GFFFieldType fieldType))
                {
                    // Can be null if not found
                    object value = gffStruct.GetValue(label);
                    if (value != null)
                    {
                        return (fieldType, value);
                    }
                }
            }
            return null;
        }

        protected static string GetIdentifierForLogging(ModifyGFF modifier)
        {
            if (modifier is AddStructToListGFF addStruct)
            {
                return addStruct.Identifier;
            }
            if (modifier is AddFieldGFF addField)
            {
                return addField.Identifier;
            }
            if (modifier is ModifyFieldGFF modifyField)
            {
                return modifyField.Identifier;
            }
            if (modifier is Memory2DAModifierGFF mem2DA)
            {
                return mem2DA.Identifier;
            }
            return modifier.GetType().Name;
        }

        protected static string GetPathForLogging(ModifyGFF modifier)
        {
            if (modifier is AddStructToListGFF addStruct)
            {
                return addStruct.Path;
            }
            if (modifier is AddFieldGFF addField)
            {
                return addField.Path;
            }
            if (modifier is ModifyFieldGFF modifyField)
            {
                return modifyField.Path;
            }
            if (modifier is Memory2DAModifierGFF mem2DA)
            {
                return mem2DA.Path;
            }
            return "";
        }
    }

    /// <summary>
    /// Adds a new struct to a GFF list (HoloPatcher-compatible).
    /// </summary>
    public class AddStructToListGFF : ModifyGFF
    {
        public string Identifier { get; }
        public FieldValue Value { get; }
        public string Path { get; set; }
        public int? IndexToToken { get; }
        public List<ModifyGFF> Modifiers { get; } = new List<ModifyGFF>();

        public AddStructToListGFF(string identifier, [CanBeNull] FieldValue value, string path, int? indexToToken = null)
        {
            Identifier = identifier;
            Value = value;
            Path = path;
            IndexToToken = indexToToken;
        }

        /// <summary>
        /// Adds a new struct to a list.
        ///
        /// Args:
        /// ----
        ///     root_struct: The root struct to navigate and modify.
        ///     memory: The memory object to read/write values from.
        ///     logger: The logger to log errors or warnings.
        ///
        /// Processing Logic:
        /// ----------------
        ///     1. Navigates to the target list container using the provided path.
        ///     2. Checks if the navigated container is a list, otherwise logs an error.
        ///     3. Creates a new struct and adds it to the list.
        ///     4. Applies any additional field modifications specified in the modifiers.
        /// </summary>
        public override void Apply(object rootContainer, PatcherMemory memory, PatchLogger logger, Game game = Game.K1)
        {
            if (!(rootContainer is GFFStruct rootStruct))
            {
                logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.ExpectedGffStructButGot, rootContainer.GetType().Name));
                return;
            }

            GFFList listContainer = null;

            (_, string pathName) = SplitPath(Path);
            string workingPath = Path;
            if (pathName == ">>##INDEXINLIST##<<")
            {
                logger.AddVerbose($"Removing unique sentinel from AddStructToListGFF instance (ini section [{Identifier}]). Path: '{Path}'");
                // self.path = self.path.parent  # HACK(th3w1zard1): idk why conditional parenting is necessary but it works
                (workingPath, _) = SplitPath(Path);
                Path = workingPath;
            }

            // navigated_container: GFFList | GFFStruct | None = self._navigate_containers(root_container, self.path) if self.path.name else root_container
            (_, string workingPathName) = SplitPath(workingPath);
            // Can be null if not found
            object navigatedContainer = string.IsNullOrEmpty(workingPathName)
                ? rootStruct
                : NavigateContainers(rootStruct, workingPath);

            // if navigated_container is root_container:
            if (navigatedContainer == rootStruct)
            {
                logger.AddNote(string.Format(CultureInfo.CurrentCulture, PatcherResources.GffPathNotFoundDefaultingToRootFormat, workingPath));
            }

            // Branch: navigated container is a list
            if (navigatedContainer is GFFList gffList)
            {
                listContainer = gffList;
            }
            else
            {
                // reason: str = "Does not exist" if navigated_container is None else f"Path points to a '{navigated_container.__class__.__name__}', expected a GFFList."
                string reason = navigatedContainer is null
                    ? PatcherResources.DoesNotExist
                    : string.Format(CultureInfo.CurrentCulture, PatcherResources.PathPointsToExpectedGffList, navigatedContainer.GetType().Name);
                string pathDisplay = string.IsNullOrEmpty(workingPath) ? $"[{Identifier}]" : workingPath;
                logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.UnableToAddStructToList, pathDisplay, reason));
                return;
            }

            // new_struct: GFFStruct | None = [CanBeNull] None
            GFFStruct newStruct = null;
            try
            {
                // lookup: Any = self.value.value(memory, GFFFieldType.Struct)
                object lookup = Value.Value(memory, GFFFieldType.Struct);

                // if lookup == "listindex":
                if (lookup is string listIndexStr && listIndexStr == "listindex")
                {
                    // new_struct = GFFStruct(len(list_container._structs)-1)
                    newStruct = new GFFStruct(listContainer.Count - 1);
                }
                // Branch: lookup is an existing struct
                else if (lookup is GFFStruct gffStruct)
                {
                    newStruct = gffStruct;
                }
                else
                {
                    // raise ValueError(f"bad lookup: {lookup} ({lookup!r}) expected 'listindex' or GFFStruct")
                    throw new ArgumentException($"bad lookup: {lookup} ({lookup}) expected 'listindex' or GFFStruct");
                }
            }
            catch (KeyNotFoundException e)
            {
                // except KeyError as e:
                logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.IniSectionThrewException, Identifier, e));
            }

            // Missing or invalid new struct
            if (newStruct is null)
            {
                logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.FailedToAddNewStructToList, workingPath, Identifier, newStruct?.ToString() ?? "null", newStruct?.ToString() ?? "null", newStruct?.GetType().Name ?? "null"));
                return;
            }

            // list_container._structs.append(new_struct)
            // In C#, Add creates a new struct, so we need to add with the structId and then copy fields if it's an existing struct
            GFFStruct addedStruct = listContainer.Add(newStruct.StructId);
            // If newStruct is not the same as what Add created (i.e., it's an existing struct with fields), copy the fields
            if (newStruct.Count > 0)
            {
                // Copy all fields from newStruct to addedStruct
                foreach ((string label, GFFFieldType fieldType, object value) in newStruct)
                {
                    SetFieldValue(addedStruct, label, value, fieldType, memory);
                }
            }

            // if self.index_to_token is not None:
            if (IndexToToken.HasValue)
            {
                // length = str(len(list_container) - 1)
                string length = (listContainer.Count - 1).ToString();
                // logger.add_verbose(f"Set 2DAMEMORY{self.index_to_token}={length}")
                logger.AddVerbose($"Set 2DAMEMORY{IndexToToken.Value}={length}");
                // memory.memory_2da[self.index_to_token] = length
                memory.Memory2DA[IndexToToken.Value] = length;
            }

            // for add_field in self.modifiers:
            foreach (ModifyGFF addField in Modifiers)
            {
                // Modifier must be one of the supported GFF patch types
                if (
                    !(addField is AddFieldGFF)
                    && !(addField is AddStructToListGFF)
                    && !(addField is Memory2DAModifierGFF)
                    && !(addField is ModifyFieldGFF))
                {
                    logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.UnexpectedModifierType, addField.GetType().Name, addField));
                    continue;
                }

                // list_index = len(list_container) - 1
                int listIndex = listContainer.Count - 1;

                // newpath = self.path / str(list_index)
                string newpath = string.IsNullOrEmpty(workingPath)
                    ? listIndex.ToString()
                    : $"{workingPath}/{listIndex}";

                string addFieldIdentifier = GetIdentifierForLogging(addField);
                string addFieldPath = GetPathForLogging(addField);
                logger.AddVerbose($"Resolved GFFList path of [{addFieldIdentifier}] from '{addFieldPath}' --> '{newpath}'");
                // add_field.path = newpath
                if (addField is AddFieldGFF addFieldGFF)
                {
                    addFieldGFF.Path = newpath;
                }
                else if (addField is AddStructToListGFF addStructToListGFF)
                {
                    // We need to find a way to update the path
                    addStructToListGFF.Path = newpath; // FIXME: property setter; reference impl mutated path in place
                }

                // add_field.apply(root_container, memory, logger)
                addField.Apply(rootStruct, memory, logger, game);
            }
        }

        public static GFFFieldType FieldType => GFFFieldType.Struct;
    }

    /// <summary>
    /// Adds a new field to a GFF structure (HoloPatcher-compatible).
    /// </summary>
    public class AddFieldGFF : ModifyGFF
    {
        public string Identifier { get; }
        public string Label { get; }
        public GFFFieldType FieldType { get; }
        public FieldValue Value { get; }
        public string Path { get; set; }
        public List<ModifyGFF> Modifiers { get; } = new List<ModifyGFF>();

        public AddFieldGFF(string identifier, string label, GFFFieldType fieldType, FieldValue value, string path)
        {
            Identifier = identifier;
            Label = label;
            FieldType = fieldType;
            Value = value;
            Path = path;
        }

        /// <summary>
        /// Adds a new field to a GFF struct.
        ///
        /// Args:
        /// ----
        ///     root_struct: GFFStruct - The root GFF struct to navigate and modify.
        ///     memory: PatcherMemory - The memory state to read values from.
        ///     logger: PatchLogger - The logger to record errors to.
        ///
        /// Processing Logic:
        /// ----------------
        ///     - Navigates to the specified container path and gets the GFFStruct instance
        ///     - Resolves the field value using the provided value expression
        ///     - Resolves the value path if part of !FieldPath memory
        ///     - Sets the field on the struct instance using the appropriate setter based on field type
        ///     - Applies any modifier patches recursively
        /// </summary>
        public override void Apply(object rootContainer, PatcherMemory memory, PatchLogger logger, Game game = Game.K1)
        {
            if (!(rootContainer is GFFStruct rootStruct))
            {
                logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.ExpectedGffStructButGot, rootContainer.GetType().Name));
                return;
            }

            logger.AddVerbose($"Apply patch from INI section [{Identifier}] FieldType: {FieldType} GFF Path: '{Path}'");
            (string parentPath, string pathName) = SplitPath(Path);
            string containerPath = pathName == ">>##INDEXINLIST##<<" ? parentPath : Path ?? string.Empty;

            // Can be null if not found
            object navigatedContainer = NavigateContainers(rootStruct, containerPath);
            if (!(navigatedContainer is GFFStruct structContainer))
            {
                string reason = navigatedContainer is null ? "does not exist!" : "is not an instance of a GFFStruct.";
                logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.UnableToAddNewGffField, Label, containerPath, reason));
                return;
            }

            object value = Value.Value(memory, FieldType);

            if (value is string strValue && (strValue.Contains('/') || strValue.Contains('\\')))
            {
                string storedFieldpath = strValue;
                if (Value is FieldValue2DAMemory fieldValue2DA)
                {
                    logger.AddVerbose($"Looking up field pointer of stored !FieldPath ({storedFieldpath}) in 2DAMEMORY{fieldValue2DA.TokenId}");
                }
                else
                {
                    logger.AddVerbose($"Path string in value() lookup from non-FieldValue2DAMemory object? Path: \"{storedFieldpath}\" INI section: [{Identifier}]");
                }
                (string fromParentPath, string fromLabel) = SplitPath(storedFieldpath);
                // Can be null if not found
                object fromContainer = NavigateContainers(rootStruct, fromParentPath);
                if (!(fromContainer is GFFStruct fromStruct))
                {
                    string reason = fromContainer is null ? PatcherResources.PathDoesNotExist : PatcherResources.PathIsNotGffStructInstance;
                    logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.UnableToUseFieldPathFrom2DAMemoryFormat, fromParentPath, reason));
                    return;
                }
                value = fromStruct.GetValue(fromLabel) ?? value;
                logger.AddVerbose($"Acquired value '{value}' from 2DAMEMORY !FieldPath({storedFieldpath})");
            }

            logger.AddVerbose($"AddField: Creating field of type '{FieldType}' value: '{value}' at GFF path '{Path}'. INI section: [{Identifier}]");

            SetFieldValue(structContainer, Label, value, FieldType, memory);

            foreach (ModifyGFF addField in Modifiers)
            {
                if (
                    !(addField is AddFieldGFF)
                    && !(addField is AddStructToListGFF)
                    && !(addField is ModifyFieldGFF)
                    && !(addField is Memory2DAModifierGFF))
                {
                    logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.UnexpectedModifierType, addField.GetType().Name, addField));
                    continue;
                }

                // Merge child path with parent path segment-by-segment (longest-length zip; prefer parent segment when present).
                string childPath = addField is AddFieldGFF af ? af.Path : (addField is AddStructToListGFF asl ? asl.Path : string.Empty);
                List<string> newpathParts = new List<string>();
                string[] childParts = string.IsNullOrEmpty(childPath) ? Array.Empty<string>() : childPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                string[] selfParts = string.IsNullOrEmpty(Path) ? Array.Empty<string>() : Path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                int maxParts = Math.Max(childParts.Length, selfParts.Length);
                for (int i = 0; i < maxParts; i++)
                {
                    string part = i < childParts.Length ? childParts[i] : null;
                    string resolved = i < selfParts.Length ? selfParts[i] : null;
                    newpathParts.Add(!string.IsNullOrEmpty(resolved) ? resolved : part);
                }
                string newpath = string.Join("/", newpathParts.Where(p => !string.IsNullOrEmpty(p)));

                string childIdentifier = GetIdentifierForLogging(addField);
                logger.AddVerbose($"Resolved gff path of INI section [{childIdentifier}] from relative '{childPath}' --> absolute '{newpath}'");
                if (addField is AddFieldGFF addFieldGFF)
                {
                    addFieldGFF.Path = newpath;
                }
                else if (addField is AddStructToListGFF addStructToListGFF)
                {
                    addStructToListGFF.Path = newpath;
                }

                addField.Apply(rootStruct, memory, logger, game);
            }
        }
    }

    /// <summary>
    /// A modifier class used for !FieldPath support.
    /// </summary>
    public class Memory2DAModifierGFF : ModifyGFF
    {
        public string Identifier { get; }
        public string Path { get; }
        public int DestTokenId { get; }
        public int? SrcTokenId { get; }

        public Memory2DAModifierGFF(string identifier, string path, int destTokenId, int? srcTokenId = null)
        {
            Identifier = identifier;
            Path = path;
            DestTokenId = destTokenId;
            SrcTokenId = srcTokenId;
        }

        public override void Apply(object rootContainer, PatcherMemory memory, PatchLogger logger, Game game = Game.K1)
        {
            if (!(rootContainer is GFFStruct rootStruct))
            {
                return;
            }

            // dest_field: _GFFField | None = None
            // source_field: _GFFField | None = None
            (GFFFieldType fieldType, object value)? destFieldInfo = null;
            (GFFFieldType fieldType, object value)? sourceFieldInfo = null;

            // display_dest_name = f"2DAMEMORY{self.dest_token_id}"
            string displayDestName = $"2DAMEMORY{DestTokenId}";

            // display_src_name = f"2DAMEMORY{self.src_token_id}"
            string displaySrcName;

            // if self.src_token_id is None:  # assign the path and leave.
            if (SrcTokenId is null)
            {
                // display_src_name = f"!FieldPath({self.path})"
                displaySrcName = $"!FieldPath({Path})";
                // logger.add_verbose(f"Assign {display_dest_name}={display_src_name}")
                logger.AddVerbose($"Assign {displayDestName}={displaySrcName}");

                // memory.memory_2da[self.dest_token_id] = self.path
                // Store path string as-is (Windows backslashes preserved from config parsing)
                string windowsPath = Path ?? "";
                memory.Memory2DA[DestTokenId] = windowsPath;
                return;
            }

            // display_src_name = f"2DAMEMORY{self.src_token_id}"
            displaySrcName = $"2DAMEMORY{SrcTokenId.Value}";
            // logger.add_verbose(f"GFFList ptr !fieldpath: Assign {display_dest_name}={display_src_name} initiated. iniPath: {self.path}, section: [{self.identifier}]")
            logger.AddVerbose($"GFFList ptr !fieldpath: Assign {displayDestName}={displaySrcName} initiated. iniPath: {Path}, section: [{Identifier}]");

            // ptr_to_dest from memory or ini path
            // Can be null if not found
            object ptrToDest = memory.Memory2DA.TryGetValue(DestTokenId, out string destPath) ? destPath : Path;

            // String token may be a GFF path (including a single label like "AppearanceType"); navigate when it looks like a path
            if (ptrToDest is string destPathStr)
            {
                // dest_field = self._navigate_to_field(root_container, ptr_to_dest)
                destFieldInfo = NavigateToField(rootStruct, destPathStr);

                // if dest_field is None:
                if (destFieldInfo is null)
                {
                    // raise ValueError(f"Cannot assign 2DAMEMORY{self.dest_token_id}=2DAMEMORY{self.src_token_id}: LEFT side of assignment's path '{ptr_to_dest}' does not point to a valid GFF Field!")
                    throw new ArgumentException($"Cannot assign 2DAMEMORY{DestTokenId}=2DAMEMORY{SrcTokenId.Value}: LEFT side of assignment's path '{ptrToDest}' does not point to a valid GFF Field!");
                }

                // dest_field resolved to a GFF field
                // logger.add_verbose(f"LEFT SIDE 2DAMEMORY{self.src_token_id} lookup at '{ptr_to_dest}' returned '{dest_field.value()}'")
                logger.AddVerbose($"LEFT SIDE 2DAMEMORY{SrcTokenId.Value} lookup at '{ptrToDest}' returned '{destFieldInfo.Value.value}'");
            }
            // elif ptr_to_dest is None:
            else if (ptrToDest is null)
            {
                // logger.add_verbose(f"Left side {display_dest_name} is not defined yet.")
                logger.AddVerbose($"Left side {displayDestName} is not defined yet.");
            }
            else
            {
                // logger.add_verbose(f"Left side {display_dest_name} value of {ptr_to_dest} will be overwritten.")
                logger.AddVerbose($"Left side {displayDestName} value of {ptrToDest} will be overwritten.");
            }

            // # Lookup assigning value
            // ptr_to_src from memory
            if (!memory.Memory2DA.TryGetValue(SrcTokenId.Value, out string ptrToSrc))
            {
                ptrToSrc = null;
            }

            // if ptr_to_src is None:
            if (ptrToSrc is null)
            {
                // raise ValueError(f"Cannot assign {display_dest_name}={display_src_name} because RIGHT side of assignment is undefined.")
                throw new ArgumentException($"Cannot assign {displayDestName}={displaySrcName} because RIGHT side of assignment is undefined.");
            }

            // Path-shaped string: !FieldPath indirection
            if (ptrToSrc is string srcPathStr && (srcPathStr.Contains('/') || srcPathStr.Contains('\\')))
            {
                // logger.add_verbose(f"Assigner {display_src_name} is a pointer !FieldPath to another field located at '{ptr_to_src}'")
                logger.AddVerbose($"Assigner {displaySrcName} is a pointer !FieldPath to another field located at '{ptrToSrc}'");

                // source_field = self._navigate_to_field(root_container, ptr_to_src)
                sourceFieldInfo = NavigateToField(rootStruct, ptrToSrc);

                // source_field must be a resolved GFF field
                if (sourceFieldInfo is null)
                {
                    throw new InvalidOperationException($"Source field at '{ptrToSrc}' is not a valid GFF Field");
                }
            }
            else
            {
                // logger.add_verbose(f"Assigner {display_src_name} holds literal value '{ptr_to_src}'. other stored info debug: Path: '{self.path}' INI section: [{self.identifier}]")
                logger.AddVerbose($"Assigner {displaySrcName} holds literal value '{ptrToSrc}'. other stored info debug: Path: '{Path}' INI section: [{Identifier}]");
            }

            // Assign into resolved destination field when present
            if (destFieldInfo.HasValue)
            {
                // logger.add_verbose("assign dest ptr field.")
                logger.AddVerbose("assign dest ptr field.");

                // assert source_field is None or dest_field.field_type() is source_field.field_type(), f"Not a _GFFField: {ptr_to_src} ({display_src_name}) OR {dest_field.field_type()} != {source_field.field_type()}"
                if (sourceFieldInfo.HasValue && destFieldInfo.Value.fieldType != sourceFieldInfo.Value.fieldType)
                {
                    throw new InvalidOperationException($"Not a _GFFField: {ptrToSrc} ({displaySrcName}) OR {destFieldInfo.Value.fieldType} != {sourceFieldInfo.Value.fieldType}");
                }

                // dest_field._value = FieldValueConstant(ptr_to_src).value(memory, dest_field.field_type())
                // Get the destination field path to set it
                string destFieldPath = ptrToDest is string destPathForSetting && (destPathForSetting.Contains('/') || destPathForSetting.Contains('\\'))
                    ? destPathForSetting
                    : Path;
                (string destParentPath, string destLabel) = SplitPath(destFieldPath);
                // Can be null if not found
                object destContainer = NavigateContainers(rootStruct, destParentPath);
                if (destContainer is GFFStruct destStruct)
                {
                    // dest_field._value = FieldValueConstant(ptr_to_src).value(memory, dest_field.field_type())
                    // Note: ptr_to_src can be either a path string or a literal value, FieldValueConstant handles both
                    object convertedValue = new FieldValueConstant(ptrToSrc).Value(memory, destFieldInfo.Value.fieldType);
                    SetFieldValue(destStruct, destLabel, convertedValue, destFieldInfo.Value.fieldType, memory);
                }
            }
            else
            {
                // memory.memory_2da[self.dest_token_id] = ptr_to_dest
                memory.Memory2DA[DestTokenId] = ptrToDest?.ToString() ?? "";
            }
        }
    }

    /// <summary>
    /// Modifies an existing field in a GFF structure (HoloPatcher-compatible).
    /// </summary>
    public class ModifyFieldGFF : ModifyGFF
    {
        public string Path { get; }
        public FieldValue Value { get; }
        public string Identifier { get; }

        public ModifyFieldGFF(string path, FieldValue value, string identifier = "")
        {
            Path = path;
            Value = value;
            Identifier = identifier;
        }

        /// <summary>
        /// Applies a patch to an existing field in a GFF structure.
        ///
        /// Args:
        /// ----
        ///     root_struct: {GFF structure}: Root GFF structure to navigate and modify
        ///     memory: {PatcherMemory}: Memory context to retrieve values
        ///     logger: {PatchLogger}: Logger to record errors
        ///
        /// Processing Logic:
        /// ----------------
        ///     - Navigates container hierarchy to the parent of the field using the patch path
        ///     - Checks if parent container exists and is a GFFStruct
        ///     - Gets the field type from the parent struct
        ///     - Converts the patch value to the correct type
        ///     - Calls the corresponding setter method on the parent struct
        /// </summary>
        public override void Apply(object rootContainer, PatcherMemory memory, PatchLogger logger, Game game = Game.K1)
        {
            if (!(rootContainer is GFFStruct rootStruct))
            {
                logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.ExpectedGffStructButGot, rootContainer.GetType().Name));
                return;
            }

            // label: str = self.path.name
            // navigated_container: GFFList | GFFStruct | None = self._navigate_containers(root_container, self.path.parent)
            (string parentPath, string label) = SplitPath(Path);
            // Can be null if not found
            object navigatedContainer = NavigateContainers(rootStruct, parentPath);

            if (!(navigatedContainer is GFFStruct navigatedStruct))
            {
                string reason = navigatedContainer is null ? PatcherResources.PathDoesNotExist : PatcherResources.PathIsNotGffStructInstance;
                logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.UnableToModifyGffFieldFormat, label, Path, reason));
                return;
            }

            // field_type: GFFFieldType = navigated_struct._fields[label].field_type()
            if (!navigatedStruct.TryGetFieldType(label, out GFFFieldType fieldType))
            {
                // Field does not exist; align with KPatcher behavior by creating it on-the-fly.
                fieldType = GFFFieldType.Int32;
            }

            // value: Any = self.value.value(memory, field_type)
            object value = Value.Value(memory, fieldType);

            // Path-shaped string: !FieldPath
            if (value is string strValue && (strValue.Contains('/') || strValue.Contains('\\')))
            {
                string storedFieldpath = strValue;
                if (Value is FieldValue2DAMemory fieldValue2DA)
                {
                    logger.AddVerbose($"Looking up field pointer of stored !FieldPath ({storedFieldpath}) in 2DAMEMORY{fieldValue2DA.TokenId}");
                }
                else
                {
                    logger.AddVerbose($"Path string in value() lookup from non-FieldValue2DAMemory object? Path: \"{storedFieldpath}\" INI section: [{Identifier}]");
                }
                (string fromParentPath, string fromLabel) = SplitPath(storedFieldpath);
                // Can be null if not found
                object fromContainer = NavigateContainers(rootStruct, fromParentPath);
                if (!(fromContainer is GFFStruct fromStruct))
                {
                    string reason = fromContainer is null ? PatcherResources.PathDoesNotExist : PatcherResources.PathIsNotGffStructInstance;
                    logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.UnableToUseFieldPathFrom2DAMemoryFormat, fromParentPath, reason));
                    return;
                }
                value = fromStruct.GetValue(fromLabel) ?? value;
                logger.AddVerbose($"Acquired value '{value}' from field at !FieldPath '{storedFieldpath}'");
            }

            // try: orig_value = FIELD_TYPE_TO_GETTER[field_type](navigated_struct, label)
            try
            {
                // Can be null if not found
                object origValue = GetFieldValue(navigatedStruct, label, fieldType);
                logger.AddVerbose($"Found original value of '{origValue}' ({origValue}) at GFF Path {Path}: Patch section: [{Identifier}]");
            }
            catch (KeyNotFoundException)
            {
                string msg = $"The field {fieldType} did not exist at {Path} in INI section [{Identifier}]. Use AddField if you need to create fields/structs.\nDue to the above error, no value will be set here.";
                logger.AddError(msg);
                return;
            }

            logger.AddVerbose($"Direct set value of determined field type '{fieldType}' at GFF path '{Path}' to new value '{value}'. INI section: [{Identifier}]");

            // if field_type is not GFFFieldType.LocalizedString:
            if (fieldType != GFFFieldType.LocalizedString)
            {
                SetFieldValue(navigatedStruct, label, value, fieldType, memory);
                return;
            }

            // LocalizedString field
            if (!(value is LocalizedString locString))
            {
                logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.ExpectedLocalizedStringButGotFormat, value?.GetType().Name ?? "null"));
                return;
            }

            // if not navigated_struct.exists(label):
            if (!navigatedStruct.Exists(label))
            {
                navigatedStruct.SetLocString(label, locString);
            }
            else
            {
                // LocalizedStringDelta field
                if (!(value is LocalizedStringDelta delta))
                {
                    logger.AddError(string.Format(CultureInfo.CurrentCulture, PatcherResources.ExpectedLocalizedStringDeltaButGotFormat, value.GetType().Name));
                    return;
                }
                LocalizedString original = navigatedStruct.GetLocString(label);
                delta.Apply(original, memory);
                navigatedStruct.SetLocString(label, original);
            }
        }

        private static object GetFieldValue(GFFStruct gffStruct, string label, GFFFieldType fieldType)
        {
            switch (fieldType)
            {
                case GFFFieldType.Int8: return gffStruct.GetInt8(label);
                case GFFFieldType.UInt8: return gffStruct.GetUInt8(label);
                case GFFFieldType.Int16: return gffStruct.GetInt16(label);
                case GFFFieldType.UInt16: return gffStruct.GetUInt16(label);
                case GFFFieldType.Int32: return gffStruct.GetInt32(label);
                case GFFFieldType.UInt32: return gffStruct.GetUInt32(label);
                case GFFFieldType.Int64: return gffStruct.GetInt64(label);
                case GFFFieldType.UInt64: return gffStruct.GetUInt64(label);
                case GFFFieldType.Single: return gffStruct.GetSingle(label);
                case GFFFieldType.Double: return gffStruct.GetDouble(label);
                case GFFFieldType.String: return gffStruct.GetString(label);
                case GFFFieldType.ResRef: return gffStruct.GetResRef(label);
                case GFFFieldType.LocalizedString: return gffStruct.GetLocString(label);
                case GFFFieldType.Vector3: return gffStruct.GetVector3(label);
                case GFFFieldType.Vector4: return gffStruct.GetVector4(label);
                case GFFFieldType.List: return gffStruct.GetList(label);
                case GFFFieldType.Struct: return gffStruct.GetStruct(label);
                default:
                    throw new KeyNotFoundException($"Unknown field type: {fieldType}");
            }
        }
    }
}

