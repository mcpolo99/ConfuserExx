using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Confuser.Core;

namespace ConfuserEx {
	internal class ComponentDiscovery {
		public static void LoadComponents(IList<ConfuserComponent> protections, IList<ConfuserComponent> packers, string pluginPath) {
			var alc = new PluginLoadContext(pluginPath);
			try {
				Assembly assembly = alc.LoadFromAssemblyPath(pluginPath);
				foreach (var module in assembly.GetLoadedModules())
					foreach (var i in module.GetTypes()) {
						if (i.IsAbstract || !PluginDiscovery.HasAccessibleDefConstructor(i))
							continue;

						if (typeof(Protection).IsAssignableFrom(i)) {
							var prot = (Protection)Activator.CreateInstance(i);
							AddProtection(protections, Info.FromComponent(prot, pluginPath));
						}
						else if (typeof(Packer).IsAssignableFrom(i)) {
							var packer = (Packer)Activator.CreateInstance(i);
							AddPacker(packers, Info.FromComponent(packer, pluginPath));
						}
					}
			}
			finally {
				alc.Unload();
			}
		}

		public static void RemoveComponents(IList<ConfuserComponent> protections, IList<ConfuserComponent> packers, string pluginPath) {
			protections.RemoveWhere(comp => comp is InfoComponent && ((InfoComponent)comp).info.path == pluginPath);
			packers.RemoveWhere(comp => comp is InfoComponent && ((InfoComponent)comp).info.path == pluginPath);
		}

		static void AddProtection(IList<ConfuserComponent> protections, Info info) {
			foreach (var comp in protections) {
				if (comp.Id == info.id)
					return;
			}
			protections.Add(new InfoComponent(info));
		}

		static void AddPacker(IList<ConfuserComponent> packers, Info info) {
			foreach (var comp in packers) {
				if (comp.Id == info.id)
					return;
			}
			packers.Add(new InfoComponent(info));
		}

		sealed class PluginLoadContext : AssemblyLoadContext {
			readonly AssemblyDependencyResolver resolver;

			public PluginLoadContext(string pluginPath)
				: base(isCollectible: true) {
				resolver = new AssemblyDependencyResolver(pluginPath);
			}

			protected override Assembly Load(AssemblyName assemblyName) {
				// Defer to the default context for Confuser.Core types to maintain type identity
				if (assemblyName.Name == "Confuser.Core" ||
				    assemblyName.Name == "Confuser.Protections" ||
				    assemblyName.Name == "Confuser.Renamer" ||
				    assemblyName.Name == "Confuser.DynCipher" ||
				    assemblyName.Name == "dnlib")
					return null;

				string assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
				if (assemblyPath != null)
					return LoadFromAssemblyPath(assemblyPath);

				return null;
			}

			protected override IntPtr LoadUnmanagedDll(string unmanagedDllName) {
				string libraryPath = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
				if (libraryPath != null)
					return LoadUnmanagedDllFromPath(libraryPath);

				return IntPtr.Zero;
			}
		}

		class Info {
			public string desc;
			public string fullId;
			public string id;
			public string name;
			public string path;

			public static Info FromComponent(ConfuserComponent component, string pluginPath) {
				var ret = new Info();
				ret.name = component.Name;
				ret.desc = component.Description;
				ret.id = component.Id;
				ret.fullId = component.FullId;
				ret.path = pluginPath;
				return ret;
			}
		}

		class InfoComponent : ConfuserComponent {
			public readonly Info info;

			public InfoComponent(Info info) {
				this.info = info;
			}

			public override string Name {
				get { return info.name; }
			}

			public override string Description {
				get { return info.desc; }
			}

			public override string Id {
				get { return info.id; }
			}

			public override string FullId {
				get { return info.fullId; }
			}

			protected override void Initialize(ConfuserContext context) {
				throw new NotSupportedException();
			}

			protected override void PopulatePipeline(ProtectionPipeline pipeline) {
				throw new NotSupportedException();
			}
		}
	}
}
