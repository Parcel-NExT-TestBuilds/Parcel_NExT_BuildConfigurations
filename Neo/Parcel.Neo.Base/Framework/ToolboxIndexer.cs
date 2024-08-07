using Parcel.Neo.Base.Toolboxes.Basic;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using System.IO;
using Parcel.CoreEngine.Helpers;
using Parcel.NExT.Interpreter.Types;
using Parcel.CoreEngine.Service.Interpretation;
using System.Numerics;
using System.Drawing;

namespace Parcel.Neo.Base.Framework
{
    public static class ToolboxIndexer
    {
        #region Cache
        private static Dictionary<string, ToolboxNodeExport[]>? _toolboxes;
        public static Dictionary<string, ToolboxNodeExport[]> Toolboxes
        {
            get
            {
                _toolboxes ??= IndexToolboxes();
                return _toolboxes;
            }
        }
        #endregion

        #region Method
        public static void AddTool(string toolbox, ToolboxNodeExport node)
            => RegisterTool(Toolboxes, toolbox, node);
        public static void AddTools(string toolbox, ToolboxNodeExport[] nodes)
            => RegisterTools(Toolboxes, toolbox, nodes);
        public static void AddTools(string assemblyPath)
            => RegisterAssembly(assemblyPath);
        private static Dictionary<string, ToolboxNodeExport[]> IndexToolboxes()
        {
            Dictionary<string, Assembly> toolboxAssemblies = [];
            // Register Parcel packages (environment path)
            foreach ((string Name, string Path) package in GetPackages())
            {
                try
                {
                    RegisterToolbox(toolboxAssemblies, package.Name, Assembly.LoadFrom(package.Path));
                }
                catch (Exception) { continue; }
            }
            // Register entire (referenced) assemblies
            RegisterToolbox(toolboxAssemblies, "Generator", Assembly.Load("Parcel.Generators"));
            RegisterToolbox(toolboxAssemblies, "Vector", Assembly.Load("Parcel.Vector"));
            RegisterToolbox(toolboxAssemblies, "Yahoo Finance", Assembly.Load("Parcel.YahooFinance"));
            // Index toolbox nodes
            Dictionary<string, ToolboxNodeExport?[]> toolboxes = IndexToolboxes(toolboxAssemblies);

            // Register front-end specific toolboxes (In general we try to eliminate those, or to say the least standardization effort is needed to make sure those are understood across implementations
            AddToolbox(toolboxes, "Basic", new BasicToolbox());
            // Register DSL specific types - Image processing
            RegisterType(toolboxes, "Image Processing", typeof(Types.Image));
            // Register specific types - Parcel "Standard"
            RegisterType(toolboxes, "Plotting", typeof(Parcel.Graphing.Plot));
            RegisterType(toolboxes, "Plotting", typeof(Parcel.Graphing.MakeConfigurations));
            RegisterType(toolboxes, "Plotting", typeof(Parcel.Graphing.StatisticalFacts));
            RegisterType(toolboxes, "Plotting", typeof(Parcel.Graphing.DrawHelper));
            RegisterType(toolboxes, "Data Grid", typeof(Types.DataGrid));
            RegisterType(toolboxes, "Data Grid", typeof(Types.DataGridOperationsHelper));
            RegisterType(toolboxes, "Math", typeof(Processing.Utilities.Calculator));
            // Register specific types - The Real Parcel Standard
            RegisterType(toolboxes, "String Processing", typeof(Standard.Types.StringRoutines));
            RegisterType(toolboxes, "Boolean Logic", typeof(Standard.Types.BooleanRoutines));
            RegisterType(toolboxes, "Boolean Logic", typeof(Standard.Types.LogicRoutines));
            RegisterType(toolboxes, "File System", typeof(Standard.System.FileSystem));
            // Register specific types - directly borrow from libraries
            RegisterType(toolboxes, "Types", typeof(Vector2));
            RegisterType(toolboxes, "Types", typeof(Size));
            RegisterType(toolboxes, "Statistics", typeof(MathNet.Numerics.Statistics.Statistics)); // TODO: Might provide selective set of functions instead of everything; Alternative, figure out how to do in-app documentation
            RegisterType(toolboxes, "Statistics", typeof(MathNet.Numerics.Statistics.Correlation));
            // RegisterType(toolboxes, "String Processing", typeof(InflectorExtensions)); // TODO: Provide Humanizer equivalent functions in PSL string processing
            RegisterType(toolboxes, "Console", typeof(Standard.System.Console));
            // Remark: Notice that boolean algebra and String are available in PSL - Pending deciding whether we need dedicated exposure
            
            return toolboxes;
        }
        #endregion

        #region Internal Method (Serialization Use)
        /// <summary>
        /// Loads a tool from a resource identifier.
        /// </summary>
        internal static FunctionalNodeDescription? LoadTool(string functionResourceIdentifier)
        {
            // TODO: Notice this at the moment doesn't handle front-end implemented tools; Because during deserialization those are handled directly by deserialization process

            IEnumerable<ToolboxNodeExport> availableNodes = Toolboxes.SelectMany(toolbox => toolbox.Value);
            ToolboxNodeExport? found = availableNodes.FirstOrDefault(n => (!n?.IsFrontendNative ?? false) && n?.Descriptor.Method.GetRuntimeNodeTypeIdentifier() == functionResourceIdentifier);
            if (found == null)
                throw new ApplicationException($"Node not found. This could be due to name change or assembly not loaded. Notice if the assembly is not loaded already - loading from arbitrary dynamic assembly during serialization is not supported at this moment!");
            return found.Descriptor;
        }
        #endregion

        #region Routines
        private static void RegisterAssembly(string assemblyPath)
        {
            string toolboxName = Path.GetFileNameWithoutExtension(assemblyPath);
            Assembly assembly = Assembly.LoadFrom(assemblyPath);

            var nodes = LoadAssembly(assembly, toolboxName);
            RegisterTools(Toolboxes, toolboxName, nodes.ToArray());
        }
        #endregion

        #region Helpers
        private static Dictionary<string, ToolboxNodeExport[]> IndexToolboxes(Dictionary<string, Assembly> assemblies)
        {
            Dictionary<string, ToolboxNodeExport[]> toolboxes = [];

            foreach (string toolboxName in assemblies.Keys.OrderBy(k => k))
            {
                Assembly assembly = assemblies[toolboxName];
                toolboxes[toolboxName] = [];

                IEnumerable<ToolboxNodeExport?>? exportedNodes = LoadAssembly(assembly, toolboxName);

                toolboxes[toolboxName] = exportedNodes!.Select(n => n!).ToArray();
            }

            return toolboxes;
        }

        private static IEnumerable<ToolboxNodeExport?> LoadAssembly(Assembly assembly, string toolboxName)
        {
            // Load either old PV1 toolbox or new Parcel package
            IEnumerable<ToolboxNodeExport?>? exportedNodes = null;
            if (assembly
                .GetTypes()
                .Any(p => typeof(IToolboxDefinition).IsAssignableFrom(p)))
            {
                // Loading per old PV1 convention
                exportedNodes = GetExportNodesFromConvention(toolboxName, assembly);
            }
            else
            {
                // Remark-cz: In the future we will utilize Parcel.CoreEngine.Service for this
                // Load generic Parcel package
                exportedNodes = GetExportNodesFromGenericAssembly(assembly);
            }

            return exportedNodes;
        }

        private static void RegisterToolbox(Dictionary<string, Assembly> toolboxAssemblies, string name, Assembly? assembly)
        {
            if (assembly == null)
                throw new ArgumentException($"Assembly is null.");

            if (toolboxAssemblies.ContainsKey(name))
                throw new InvalidOperationException($"Assembly `{assembly.FullName}` is already registered.");

            toolboxAssemblies.Add(name, assembly);
        }
        private static void AddToolbox(Dictionary<string, ToolboxNodeExport?[]> toolboxes, string name, IToolboxDefinition toolbox)
        {
            List<ToolboxNodeExport?> nodes = [];

            foreach (ToolboxNodeExport? nodeExport in toolbox?.ExportNodes ?? [])
                nodes.Add(nodeExport);

            toolboxes[name] = [.. nodes];
        }
        private static (string Name, string Path)[] GetPackages()
        {
            string packageImportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Parcel NExT", "Packages");

            string? environmentOverride = Environment.GetEnvironmentVariable("PARCEL_PACKAGES");
            if (environmentOverride != null && Directory.Exists(environmentOverride))
                packageImportPath = environmentOverride;

            if (Directory.Exists(packageImportPath))
                return Directory
                    .EnumerateFiles(packageImportPath)
                    .Where(file => Path.GetExtension(file).Equals(".dll", StringComparison.CurrentCultureIgnoreCase))
                    .Select(file => (Path.GetFileNameWithoutExtension(file), file))
                    .ToArray();
            return [];
        }
        private static void RegisterType(Dictionary<string, ToolboxNodeExport?[]> toolboxes, string name, Type type)
        {
            List<ToolboxNodeExport> nodes = [..GetConstructors(type), .. GetInstanceMethods(type), .. GetStaticMethods(type)];

            if (toolboxes.ContainsKey(name))
                // Add divider
                toolboxes[name] = [.. toolboxes[name], null, .. nodes];
            else
                toolboxes[name] = [.. nodes];
        }
        private static void RegisterTool(Dictionary<string, ToolboxNodeExport?[]> toolboxes, string name, ToolboxNodeExport node)
        {
            if (toolboxes.ContainsKey(name))
                // Add divider
                toolboxes[name] = [.. toolboxes[name], null, node];
            else
                toolboxes[name] = [node];
        }
        private static void RegisterTools(Dictionary<string, ToolboxNodeExport?[]> toolboxes, string name, ToolboxNodeExport[] nodes)
        {
            if (toolboxes.ContainsKey(name))
                // Add divider
                toolboxes[name] = [.. toolboxes[name], null, .. nodes];
            else
                toolboxes[name] = [.. nodes];
        }
        private static IEnumerable<ToolboxNodeExport> GetConstructors(Type type)
        {
            IEnumerable<ConstructorInfo> constructors = type
                            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                            .Where(m => m.DeclaringType != typeof(object))
                            .OrderBy(t => t.Name);
            foreach (ConstructorInfo constructor in constructors)
                yield return new ToolboxNodeExport($"Make {type.Name}", new Callable(constructor));
        }
        private static IEnumerable<ToolboxNodeExport> GetStaticMethods(Type type)
        {
            IEnumerable<MethodInfo> methods = type
                            .GetMethods(BindingFlags.Public | BindingFlags.Static)
                            .Where(m => m.DeclaringType != typeof(object))
                            .OrderBy(t => t.Name);
            foreach (MethodInfo method in methods)
                yield return new ToolboxNodeExport(method.Name, new Callable(method));
        }
        private static IEnumerable<ToolboxNodeExport> GetInstanceMethods(Type type)
        {
            IEnumerable<MethodInfo> methods = type
                            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                            .Where(m => m.DeclaringType != typeof(object))
                            .OrderBy(t => t.Name);
            foreach (MethodInfo method in methods)
                yield return new ToolboxNodeExport(method.Name, new Callable(method));
        }
        private static IEnumerable<ToolboxNodeExport?> GetExportNodesFromConvention(string name, Assembly assembly)
        {
            string formalName = $"{name.Replace(" ", string.Empty)}";
            string toolboxHelperTypeName = $"Parcel.Toolbox.{formalName}.{formalName}Helper";
            foreach (Type type in assembly
                .GetTypes()
                .Where(p => typeof(IToolboxDefinition).IsAssignableFrom(p)))
            {
                IToolboxDefinition? toolbox = (IToolboxDefinition?)Activator.CreateInstance(type);
                if (toolbox == null) continue;

                foreach (ToolboxNodeExport nodeExport in toolbox.ExportNodes)
                    yield return nodeExport;
            }
        }
        private static IEnumerable<ToolboxNodeExport?> GetExportNodesFromGenericAssembly(Assembly assembly)
        {
            // Try get enhanced annotation
            string xmlDocumentationPath = GetDefaultXMLDocumentationPath(assembly.Location);
            Dictionary<string, string>? nodeSummary = null;
            if (File.Exists(assembly.Location) && File.Exists(xmlDocumentationPath))
            {
                DocumentationHelper.Documentation documentation = DocumentationHelper.ParseXML(xmlDocumentationPath);
                nodeSummary = documentation.Members
                    .Where(m => m.MemberType == DocumentationHelper.MemberType.Member)
                    .ToDictionary(m => m.Signature, m => m.Summary);
            }

            // Export nodes from types
            // TODO: Support instance methods
            // TODO: Support non-static type's static methods
            Type[] typesA = assembly.GetExportedTypes() // Public static (abstract) export types
                .Where(t => t.IsAbstract)
                .Where(t => t.Name != "Object")
                .ToArray();
            Type[] typesB = assembly.GetExportedTypes() // Public classes with static methods
                .Where(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static).Length > 0)
                .Where(t => t.Name != "Object")
                .ToArray();
            Type[] types = typesA
                .Concat(typesB)
                .Distinct()
                .ToArray();

            foreach (Type type in types)
            {
                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    // Every static class seems to export the methods exposed by System.Object, i.e. Object.Equal, Object.ReferenceEquals, etc. and we don't want that. // Remark-cz: Might because of BindingFlags.FlattenHierarchy, now we removed that, this shouldn't be an issue, pending verfication
                    .Where(m => m.DeclaringType != typeof(object))
                    .ToArray();

                foreach (MethodInfo method in methods)
                {
                    string? tooltip = null;
                    string signature = RetrieveXMLMethodSignature(method);
                    nodeSummary?.TryGetValue(signature, out tooltip);
                    yield return new ToolboxNodeExport(method.Name, new Callable(method))
                    {
                        Tooltip = tooltip
                    };
                }

                // Add divider
                yield return null;
            }

            static string GetDefaultXMLDocumentationPath(string assemblyLocation)
            {
                string filename = Path.GetFileNameWithoutExtension(assemblyLocation);
                string extension = ".xml";
                string folder = Path.GetDirectoryName(assemblyLocation);
                return Path.Combine(folder, filename + extension);
            }
            static string RetrieveXMLMethodSignature(MethodInfo methodInfo)
            {
                return $"M:{methodInfo.DeclaringType.FullName}.{methodInfo.Name}({string.Join(",", methodInfo.GetParameters().Select(p => p.ParameterType.FullName))})";
            }
        }
        #endregion
    }
}
