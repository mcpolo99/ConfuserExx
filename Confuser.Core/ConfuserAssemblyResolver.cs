using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;

namespace Confuser.Core {
	internal sealed class ConfuserAssemblyResolver : IAssemblyResolver {
		internal AssemblyResolver InternalFuzzyResolver { get; } = new AssemblyResolver { FindExactMatch = false };
		internal AssemblyResolver InternalExactResolver { get; } = new AssemblyResolver { FindExactMatch = true };

		public bool EnableTypeDefCache {
			get => InternalFuzzyResolver.EnableTypeDefCache;
			set {
				InternalFuzzyResolver.EnableTypeDefCache = value;
				InternalExactResolver.EnableTypeDefCache = value;
			}
		}

		public ModuleContext DefaultModuleContext {
			get => InternalFuzzyResolver.DefaultModuleContext;
			set {
				InternalFuzzyResolver.DefaultModuleContext = value;
				InternalExactResolver.DefaultModuleContext = value;
			}
		}

		public IList<string> PostSearchPaths => new TeeList(InternalFuzzyResolver.PostSearchPaths, InternalExactResolver.PostSearchPaths);
		public IList<string> PreSearchPaths => new TeeList(InternalFuzzyResolver.PreSearchPaths, InternalExactResolver.PreSearchPaths);

		// Assemblies that are type-forwarded to mscorlib/System.Runtime on .NET Framework
		static readonly HashSet<string> TypeForwardedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
			"System.ValueTuple",
			"System.Memory",
			"System.Buffers",
			"System.Numerics.Vectors",
			"System.Runtime.CompilerServices.Unsafe",
			"System.Threading.Tasks.Extensions",
			"Microsoft.Bcl.AsyncInterfaces"
		};

		/// <inheritdoc />
		public AssemblyDef Resolve(IAssembly assembly, ModuleDef sourceModule) {
			if (assembly is AssemblyDef assemblyDef)
				return assemblyDef;

			var resolvedAssemblyDef =
				InternalExactResolver.Resolve(assembly, sourceModule) ??
				InternalFuzzyResolver.Resolve(assembly, sourceModule);

			// Fallback for type-forwarding NuGet packages: when the standalone assembly
			// isn't on disk (e.g., System.ValueTuple referenced by netstandard but types
			// live in mscorlib on .NET Framework), resolve to the source module's corlib.
			if (resolvedAssemblyDef == null && TypeForwardedAssemblies.Contains(assembly.Name)) {
				var corLibAsm = sourceModule.CorLibTypes.AssemblyRef;
				resolvedAssemblyDef =
					InternalExactResolver.Resolve(corLibAsm, sourceModule) ??
					InternalFuzzyResolver.Resolve(corLibAsm, sourceModule);
			}

			//	Remove AssemblyAttributes.PA_NoPlatform
			if (null != resolvedAssemblyDef &&
				(AssemblyAttributes.PA_Mask & resolvedAssemblyDef.Attributes) == AssemblyAttributes.PA_NoPlatform) {
				resolvedAssemblyDef.Attributes =
					resolvedAssemblyDef.Attributes & ~AssemblyAttributes.PA_FullMask;
			}

			if (resolvedAssemblyDef?.Name == "netstandard" && 0 < resolvedAssemblyDef.ManifestModule.ExportedTypes.Count) {
				//	Move types from AssemblyRef to here
				var module = resolvedAssemblyDef.ManifestModule;
				var newTypes = new List<TypeDef>();
				var allAssemblyRefs = new List<AssemblyDef>();

				module.ExportedTypes.Clear();

				foreach (var assemblyRef in module.GetAssemblyRefs()) {
					var subAss =
						InternalExactResolver.Resolve(assemblyRef, module) ??
						InternalFuzzyResolver.Resolve(assemblyRef, module);
					allAssemblyRefs.Add(subAss);
					foreach (var subModule in subAss?.Modules) {
						foreach (var defType in subModule.Types) {
							newTypes.Add(defType);
						}
						subModule.Types.Clear();
						foreach (var defType in newTypes) {
							module.Types.Add(defType);
						}
						newTypes.Clear();
					}
				}

				//	Remove them because their types has been removed.
				foreach (var subAss in allAssemblyRefs) {
					InternalExactResolver.Remove(subAss);
					InternalFuzzyResolver.Remove(subAss);
				}
			}

			return resolvedAssemblyDef;
		}

		public void Clear() {
			InternalExactResolver.Clear();
			InternalFuzzyResolver.Clear();
		}

		public IEnumerable<AssemblyDef> GetCachedAssemblies() => 
			InternalExactResolver.GetCachedAssemblies().Concat(InternalFuzzyResolver.GetCachedAssemblies());

		public void AddToCache(ModuleDefMD modDef) {
			InternalExactResolver.AddToCache(modDef);
			InternalFuzzyResolver.AddToCache(modDef);
		}

		private sealed class TeeList : IList<string> {
			private readonly IList<IList<string>> _lists;

			internal TeeList(params IList<string>[] lists) => _lists = lists;

			/// <inheritdoc />
			public IEnumerator<string> GetEnumerator() => _lists[0].GetEnumerator();

			/// <inheritdoc />
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			/// <inheritdoc />
			public void Add(string item) {
				foreach (var list in _lists) 
					list.Add(item);
			}

			/// <inheritdoc />
			public void Clear() {
				foreach (var list in _lists) 
					list.Clear();
			}

			/// <inheritdoc />
			public bool Contains(string item) => _lists[0].Contains(item);

			/// <inheritdoc />
			public void CopyTo(string[] array, int arrayIndex) => _lists[0].CopyTo(array, arrayIndex);

			/// <inheritdoc />
			public bool Remove(string item) =>
				_lists.Aggregate(true, (current, list) => current | list.Remove(item));

			/// <inheritdoc />
			public int Count => _lists[0].Count;

			/// <inheritdoc />
			public bool IsReadOnly => _lists[0].IsReadOnly;

			/// <inheritdoc />
			public int IndexOf(string item) => _lists[0].IndexOf(item);

			/// <inheritdoc />
			public void Insert(int index, string item) {
				foreach (var list in _lists) 
					list.Insert(index, item);
			}

			/// <inheritdoc />
			public void RemoveAt(int index) {
				foreach (var list in _lists) 
					list.RemoveAt(index);
			}

			/// <inheritdoc />
			public string this[int index] {
				get => _lists[0][index];
				set => _lists[0][index] = value;
			}
		}
	}
}
