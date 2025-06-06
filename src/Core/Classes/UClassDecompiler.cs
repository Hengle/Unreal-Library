#if DECOMPILE
using System.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UELib.Core
{
    public partial class UClass
    {
        protected override string CPPTextKeyword => "cpptext";

        /**
         * Structure looks like this, even though said XX.GetFriendlyName() actually it's XX.Decompile() which handles then the rest on its own.
         * class GetName() extends SuperFieldName
         *      FormatFlags()
         *
         * CPPTEXT
         * {
         * }
         *
         * Constants
         * const C.GetFriendlyName() = C.Value
         *
         * Enums
         * enum En.GetFriendlyName()
         * {
         *      FormatProperties()
         * }
         *
         * Structs
         * struct FormatFlags() Str.GetFriendlyName() extends SuperFieldName
         * {
         *      FormatProperties()
         * }
         *
         * Properties
         * var(GetCategoryName) FormatFlags() Prop.GetFriendlyName()
         *
         * Replication
         * {
         *      SerializeToken()
         * }
         *
         * Functions
         * FormatFlags() GetFriendlyName() GetParms()
         * {
         *      FormatLocals()
         *      SerializeToken()
         * }
         *
         * States
         * FormatFlags() state GetFriendlyName() extends SuperFieldName
         * {
         *      FormatIgnores()
         *      FormatFunctions()
         *      SerializeToken();
         * }
         *
         * DefaultProperties
         * {
         * }
         */
        public override string Decompile()
        {
            string content = FormatHeader() +
                       FormatCPPText() +
                       FormatConstants() +
                       FormatEnums() +
                       FormatStructs() +
                       FormatProperties() +
                       FormatReplication() +
                       FormatFunctions() +
                       FormatStates() +
                       FormatDefaultProperties();

            return content;
        }

        [Obsolete("Deprecated", true)]
        public string GetDependencies()
        {
            throw new NotImplementedException();
        }

        [Obsolete("Deprecated", true)]
        private string GetImports()
        {
            throw new NotImplementedException();
        }

        [Obsolete("Deprecated", true)]
        public string GetStats()
        {
            throw new NotImplementedException();
        }

        public override string FormatHeader()
        {
            string output = (IsClassInterface() ? "interface " : "class ") + Name;
            string metaData = DecompileMeta();
            if (metaData != string.Empty)
            {
                output = metaData + "\r\n" + output;
            }

            // Object doesn't have an extension so only try add the extension if theres a SuperField
            if (Super != null
                && !(IsClassInterface() &&
                     string.Compare(Super.Name, "Object", StringComparison.OrdinalIgnoreCase) == 0))
            {
                output += $" {FormatExtends()} {Super.Name}";
            }

            if (IsClassWithin())
            {
                output += $" within {Within.Name}";
            }
#if VENGEANCE
            if (Package.Build == BuildGeneration.Vengeance)
            {
                if (Vengeance_Implements != null && Vengeance_Implements.Any()) 
                    output += $" implements {string.Join(", ", Vengeance_Implements.Select(i => i.Name))}";
            }
#endif
            string rules = FormatFlags().Replace("\t", UnrealConfig.Indention);
            return output + (string.IsNullOrEmpty(rules) ? ";" : rules);
        }

        private string FormatNameGroup(string groupName, IList<int> enumerableList)
        {
            var output = string.Empty;
            if (enumerableList != null && enumerableList.Any())
            {
                output += "\r\n\t" + groupName + "(";
                try
                {
                    foreach (int index in enumerableList)
                    {
                        output += Package.Names[index].Name + ",";
                    }

                    output = output.TrimEnd(',') + ")";
                }
                catch
                {
                    output += string.Format("\r\n\t/* An exception occurred while decompiling {0}. */", groupName);
                }
            }

            return output;
        }

        private string FormatObjectGroup(string groupName, IList<int> enumerableList)
        {
            var output = string.Empty;
            if (enumerableList != null && enumerableList.Any())
            {
                output += "\r\n\t" + groupName + "(";
                try
                {
                    foreach (int index in enumerableList)
                    {
                        output += Package.GetIndexObjectName(index) + ",";
                    }

                    output = output.TrimEnd(',') + ")";
                }
                catch
                {
                    output += string.Format("\r\n\t/* An exception occurred while decompiling {0}. */", groupName);
                }
            }

            return output;
        }

        private string FormatFlags()
        {
            var output = string.Empty;

            if ((ClassFlags & (uint)Flags.ClassFlags.Abstract) != 0)
            {
                output += "\r\n\tabstract";
            }

            if ((ClassFlags & (uint)Flags.ClassFlags.Transient) != 0)
            {
                output += "\r\n\ttransient";
            }
            else
            {
                // Only do if parent had Transient
                var parentClass = (UClass)Super;
                if (parentClass != null && (parentClass.ClassFlags & (uint)Flags.ClassFlags.Transient) != 0)
                {
                    output += "\r\n\tnotransient";
                }
            }

            if (HasObjectFlag(Flags.ObjectFlagsLO.Native))
            {
                output += "\r\n\t" + FormatNative();
                if (NativeClassName.Length != 0)
                {
                    output += $"({NativeClassName})";
                }
            }

            if (HasClassFlag(Flags.ClassFlags.NativeOnly))
            {
                output += "\r\n\tnativeonly";
            }

            if (HasClassFlag(Flags.ClassFlags.NativeReplication))
            {
                output += "\r\n\tnativereplication";
            }

            // BTClient.Menu.uc has Config(ClientBtimes) and this flag is not true???
            if ((ClassFlags & (uint)Flags.ClassFlags.Config) != 0)
            {
                string inner = ConfigName;
                if (string.Compare(inner, "None", StringComparison.OrdinalIgnoreCase) == 0
                    || string.Compare(inner, "System", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    output += "\r\n\tconfig";
                }
                else output += $"\r\n\tconfig({inner})";
            }

            if ((ClassFlags & (uint)Flags.ClassFlags.ParseConfig) != 0)
            {
                output += "\r\n\tparseconfig";
            }

            if ((ClassFlags & (uint)Flags.ClassFlags.PerObjectConfig) != 0)
            {
                output += "\r\n\tperobjectconfig";
            }
            else
            {
                // Only do if parent had PerObjectConfig
                var parentClass = (UClass)Super;
                if (parentClass != null && (parentClass.ClassFlags & (uint)Flags.ClassFlags.PerObjectConfig) != 0)
                {
                    output += "\r\n\tnoperobjectconfig";
                }
            }

#if DNF
            if (Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
            {
                if (HasClassFlag(0x00001000U))
                {
                    output += "\r\n\tobsolete";
                }
            }
            else
#endif
            {
                if ((ClassFlags & (uint)Flags.ClassFlags.EditInlineNew) != 0)
                {
                    output += "\r\n\teditinlinenew";
                }
                else
                {
                    // Only do if parent had EditInlineNew
                    var parentClass = (UClass)Super;
                    if (parentClass != null && (parentClass.ClassFlags & (uint)Flags.ClassFlags.EditInlineNew) != 0)
                    {
                        output += "\r\n\tnoteditinlinenew";
                    }
                }
            }

            if ((ClassFlags & (uint)Flags.ClassFlags.CollapseCategories) != 0)
            {
                output += "\r\n\tcollapsecategories";
            }

#if DNF
            if (Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
            {
                if (HasClassFlag(0x00004000))
                {
                    output += "\r\n\teditinlinenew";
                }
                else
                {
                    // Only do if parent had EditInlineNew
                    var parentClass = (UClass)Super;
                    if (parentClass != null && (parentClass.ClassFlags & 0x00004000) != 0)
                    {
                        output += "\r\n\tnoteditinlinenew";
                    }
                }
            }
            else
#endif
            // TODO: Might indicate "Interface" in later versions
            if (HasClassFlag(Flags.ClassFlags.ExportStructs) && Package.Version < 300)
            {
                output += "\r\n\texportstructs";
            }

            if ((ClassFlags & (uint)Flags.ClassFlags.NoExport) != 0)
            {
                output += "\r\n\tnoexport";
            }

            if (Extends("Actor"))
            {
#if DNF
                if (Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
                {
                    if (HasClassFlag(0x02000))
                    {
                        output += "\r\n\tplaceable";
                    }
                    else
                    {
                        output += $"\r\n\tnotplaceable";
                    }
                }
                else
#endif
                {
                    if ((ClassFlags & (uint)Flags.ClassFlags.Placeable) != 0)
                    {
                        output += Package.Version >= PlaceableVersion ? "\r\n\tplaceable" : "\r\n\tusercreate";
                    }
                    else
                    {
                        output += Package.Version >= PlaceableVersion ? "\r\n\tnotplaceable" : "\r\n\tnousercreate";
                    }
                }
            }

            if ((ClassFlags & (uint)Flags.ClassFlags.SafeReplace) != 0)
            {
                output += "\r\n\tsafereplace";
            }

            // Approx version
            if ((ClassFlags & (uint)Flags.ClassFlags.Instanced) != 0 && Package.Version < 150)
            {
                output += "\r\n\tinstanced";
            }

            if ((ClassFlags & (uint)Flags.ClassFlags.HideDropDown) != 0)
            {
                output += "\r\n\thidedropdown";
            }

            if (Package.Build == UnrealPackage.GameBuild.BuildName.UT2004)
            {
                if (HasClassFlag(Flags.ClassFlags.CacheExempt))
                {
                    output += "\r\n\tcacheexempt";
                }
            }

            if (Package.Version >= 749 && Super != null)
            {
                if (ForceScriptOrder && !((UClass)Super).ForceScriptOrder)
                {
                    output += "\r\n\tforcescriptorder(true)";
                }
                else if (!ForceScriptOrder && ((UClass)Super).ForceScriptOrder)
                    output += "\r\n\tforcescriptorder(false)";
            }

            if (DLLBindName != null
                && string.Compare(DLLBindName, "None", StringComparison.OrdinalIgnoreCase) != 0)
            {
                output += $"\r\n\tdllbind({DLLBindName})";
            }

            //if (ClassDependencies != null) foreach (var dependency in ClassDependencies)
            //{
            //    var obj = dependency.Class;
            //    if (obj != null && (int)obj > (int)this && obj != Super)
            //    {
            //        output += $"\r\n\tdependson({obj.Name})";
            //    }
            //}

            output += FormatNameGroup("dontsortcategories", DontSortCategories);
            output += FormatNameGroup("hidecategories", HideCategories);
            // TODO: Decompile ShowCategories (but this is not possible without traversing the super chain)

#if DNF
            if (Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
            {
                // FIXME: Store this data in UClass
                //output += FormatNameGroup("tags", new List<int>());
                // ...UnTags

                // Maybe dnfBool?
                //if (HasClassFlag(0x1000))
                //{
                //    output += "\r\n\tobsolete";
                //}
                if (HasClassFlag(0x2000000))
                {
                    output += "\r\n\tnativedestructor";
                }

                if (HasClassFlag(0x1000000))
                {
                    output += "\r\n\tnotlistable";
                }
                else
                {
                    var parentClass = (UClass)Super;
                    if (parentClass != null && parentClass.HasClassFlag(0x1000000))
                    {
                        output += "\r\n\tlistable";
                    }
                }
            }
#endif
#if GIGANTIC
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Gigantic)
            {
                if ((ClassFlags & (ulong)Branch.UE3.GIGANTIC.EngineBranchGigantic.ClassFlags.JsonImport) != 0)
                {
                    output += "\r\n\tjsonimport";
                }
            }
#endif
#if AHIT
            if (Package.Build == UnrealPackage.GameBuild.BuildName.AHIT)
            {
                if ((ClassFlags & (uint)Flags.ClassFlags.AHIT_AlwaysLoaded) != 0)
                {
                    output += "\r\n\tAlwaysLoaded";
                }
                if ((ClassFlags & (uint)Flags.ClassFlags.AHIT_IterOptimized) != 0)
                {
                    output += "\r\n\tIterationOptimized";
                }
            }
#endif

            output += FormatNameGroup("classgroup", ClassGroups);
            output += FormatNameGroup("autoexpandcategories", AutoExpandCategories);
            output += FormatNameGroup("autocollapsecategories", AutoCollapseCategories);
            output += FormatObjectGroup("implements", ImplementedInterfaces);

            return output + ";\r\n";
        }

        const ushort VReliableDeprecation = 189;

        public string FormatReplication()
        {
            if (DataScriptSize <= 0)
            {
                return string.Empty;
            }

            var replicatedObjects = new List<IUnrealNetObject>();
            if (Variables != null)
            {
                replicatedObjects.AddRange(Variables.Where(prop =>
                    prop.HasPropertyFlag(Flags.PropertyFlagsLO.Net) && prop.RepOffset != ushort.MaxValue));
            }

            if (Package.Version < VReliableDeprecation && Functions != null)
            {
                replicatedObjects.AddRange(Functions.Where(func =>
                    func.HasFunctionFlag(Flags.FunctionFlags.Net) && func.RepOffset != ushort.MaxValue));
            }

            if (replicatedObjects.Count == 0)
            {
                return string.Empty;
            }

            var statements = new Dictionary<uint, List<IUnrealNetObject>>();
            replicatedObjects.Sort((ro, ro2) => ro.RepKey.CompareTo(ro2.RepKey));
            for (var netIndex = 0; netIndex < replicatedObjects.Count; ++netIndex)
            {
                var firstObject = replicatedObjects[netIndex];
                var netObjects = new List<IUnrealNetObject> { firstObject };
                for (int nextIndex = netIndex + 1; nextIndex < replicatedObjects.Count; ++nextIndex)
                {
                    var nextObject = replicatedObjects[nextIndex];
                    if (nextObject.RepOffset != firstObject.RepOffset
                        || nextObject.RepReliable != firstObject.RepReliable
                       )
                    {
                        netIndex = nextIndex - 1;
                        break;
                    }

                    netObjects.Add(nextObject);
                }

                netObjects.Sort((o, o2) => string.Compare(o.Name, o2.Name, StringComparison.Ordinal));
                if (!statements.ContainsKey(firstObject.RepKey))
                    statements.Add(firstObject.RepKey, netObjects);
            }

            replicatedObjects.Clear();

            var output = new StringBuilder($"\r\nreplication{UnrealConfig.PrintBeginBracket()}");
            UDecompilingState.AddTab();

            foreach (var statement in statements)
            {
                try
                {
                    var pos = (ushort)(statement.Key & 0x0000FFFF);
                    var rel = Convert.ToBoolean(statement.Key & 0xFFFF0000);

                    output.Append("\r\n" + UDecompilingState.Tabs);
                    if (!UnrealConfig.SuppressComments)
                    {
                        output.AppendFormat("// Pos:0x{0:X3}\r\n{1}", pos, UDecompilingState.Tabs);
                    }

                    ByteCodeManager.Deserialize();
                    ByteCodeManager.JumpTo(pos);
                    string statementCode;
                    try
                    {
                        statementCode = ByteCodeManager.CurrentToken.Decompile();
                    }
                    catch (EndOfStreamException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        statementCode = $"/* An exception occurred while decompiling condition ({e}) */";
                    }

                    string statementType = Package.Version < VReliableDeprecation
                        ? rel ? "reliable" : "unreliable"
                        : string.Empty;
                    string statementFormat = statementType != string.Empty
                        ? $"{statementType} if({statementCode})"
                        : $"if({statementCode})";
                    output.Append(statementFormat);

                    UDecompilingState.AddTab();
                    // NetObjects
                    for (var i = 0; i < statement.Value.Count; ++i)
                    {
                        bool shouldSplit = i % 2 == 0;
                        if (shouldSplit)
                        {
                            output.Append("\r\n" + UDecompilingState.Tabs);
                        }

                        var netObject = statement.Value[i];
                        output.Append(netObject.Name);

                        bool isNotLast = i != statement.Value.Count - 1;
                        output.Append(isNotLast ? ", " : ";");
                    }

                    UDecompilingState.RemoveTab();

                    // IsNotLast
                    if (statements.Last().Key != statement.Key)
                    {
                        output.Append("\r\n");
                    }
                }
                catch (EndOfStreamException)
                {
                    break;
                }
                catch (Exception e)
                {
                    output.AppendFormat("/* An exception occurred while decompiling a statement! ({0}) */", e);
                }
            }

            UDecompilingState.RemoveTab();
            output.Append(UnrealConfig.PrintEndBracket() + "\r\n");
            return output.ToString();
        }

        private string FormatStates()
        {
            if (States == null || !States.Any())
                return string.Empty;

            var output = string.Empty;
            foreach (var scriptState in States)
            {
                output += "\r\n" + scriptState.Decompile() + "\r\n";
            }

            return output;
        }

        public IEnumerable<string> ExportableExtensions => new List<string> { "uc" };

        public bool CanExport()
        {
            return (int)this > 0;
        }

        public void SerializeExport(string desiredExportExtension, Stream exportStream)
        {
            string data = Decompile();
            var stream = new StreamWriter(exportStream);
            stream.Write(data);
        }
    }
}
#endif
