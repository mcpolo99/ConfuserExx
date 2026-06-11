using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Confuser.Core;
using Confuser.Core.Services;
using Confuser.Renamer.Analyzers;
using dnlib.DotNet;

namespace Confuser.Renamer {
	internal class AnalyzePhase : ProtectionPhase {

		public AnalyzePhase(NameProtection parent)
			: base(parent) { }

		public override bool ProcessAll {
			get { return true; }
		}

		public override ProtectionTargets Targets {
			get { return ProtectionTargets.AllDefinitions; }
		}

		public override string Name {
			get { return "Name analysis"; }
		}

		void ParseParameters(IDnlibDef def, ConfuserContext context, NameService service, ProtectionParameters parameters) {
			var mode = parameters.GetParameter<RenameMode?>(context, def, "mode", null);
			if (mode != null)
				service.SetRenameMode(def, mode.Value);
		}

		protected override void Execute(ConfuserContext context, ProtectionParameters parameters) {
			var service = (NameService)context.Registry.GetService<INameService>();

			context.Logger.Debug("Building VTables & identifier list...");

			foreach (ModuleDef moduleDef in parameters.Targets.OfType<ModuleDef>())
				moduleDef.EnableTypeDefFindCache = true;

			foreach (IDnlibDef def in parameters.Targets.WithProgress(context.Logger)) {
				ParseParameters(def, context, service, parameters);

				if (def is ModuleDef module) {
					foreach (var res in module.Resources)
						service.AddReservedIdentifier(res.Name);
				}
				else {
					service.StoreNames(def);
				}

				if (def is TypeDef typeDef) {
					service.GetVTables().GetVTable(typeDef);
				}
				context.CheckCancellation();
			}

			context.Logger.Debug("Analyzing...");
			RegisterRenamers(context, service);
			IList<IRenamer> renamers = service.Renamers;
			foreach (IDnlibDef def in parameters.Targets.WithProgress(context.Logger)) {
				Analyze(service, context, parameters, def, true);
				context.CheckCancellation();
			}

			foreach (ModuleDef moduleDef in parameters.Targets.OfType<ModuleDef>()) {
				moduleDef.EnableTypeDefFindCache = false;
				moduleDef.ResetTypeDefFindCache();
			}
		}

		void RegisterRenamers(ConfuserContext context, NameService service) {
			bool wpf = false;
			bool caliburn = false;
			bool winforms = false;
			bool json = false;
			bool visualBasic = false;
			bool vsComposition = false;

			foreach (var module in context.Modules) {
				foreach (var asmRef in module.GetAssemblyRefs()) {
					if (asmRef.Name == "WindowsBase" || asmRef.Name == "PresentationCore" ||
						asmRef.Name == "PresentationFramework" || asmRef.Name == "System.Xaml") {
						wpf = true;
					}
					else if (asmRef.Name == "Caliburn.Micro") {
						caliburn = true;
					}
					else if (asmRef.Name == "System.Windows.Forms") {
						winforms = true;
					}
					else if (asmRef.Name == "Newtonsoft.Json") {
						json = true;
					}
					else if (asmRef.Name == "Microsoft.VisualStudio.Composition") {
						vsComposition = true;
					}
				}

				var vbEmbeddedAttribute = module.FindNormal("Microsoft.VisualBasic.Embedded");
				if (vbEmbeddedAttribute != null && vbEmbeddedAttribute.BaseType.FullName.Equals("System.Attribute")) {
					visualBasic = true;
				}
			}

			if (wpf) {
				var wpfAnalyzer = new WPFAnalyzer();
				context.Logger.Debug("WPF found, enabling compatibility.");
				service.Renamers.Add(wpfAnalyzer);
				if (caliburn) {
					context.Logger.Debug("Caliburn.Micro found, enabling compatibility.");
					service.Renamers.Add(new CaliburnAnalyzer(wpfAnalyzer));
				}
			}

			if (winforms) {
				var winformsAnalyzer = new WinFormsAnalyzer();
				context.Logger.Debug("WinForms found, enabling compatibility.");
				service.Renamers.Add(winformsAnalyzer);
			}

			if (json) {
				var jsonAnalyzer = new JsonAnalyzer();
				context.Logger.Debug("Newtonsoft.Json found, enabling compatibility.");
				service.Renamers.Add(jsonAnalyzer);
			}

			if (visualBasic) {
				var vbAnalyzer = new VisualBasicRuntimeAnalyzer();
				context.Logger.Debug("Visual Basic Embedded Runtime found, enabling compatibility.");
				service.Renamers.Add(vbAnalyzer);
			}

			if (vsComposition) {
				var analyzer = new VsCompositionAnalyzer();
				context.Logger.Debug("Visual Studio Composition found, enabling compatibility.");
				service.Renamers.Add(analyzer);
			}
		}

		internal void Analyze(NameService service, ConfuserContext context, ProtectionParameters parameters, IDnlibDef def, bool runAnalyzer) {
			if (def is TypeDef)
				Analyze(service, context, parameters, (TypeDef)def);
			else if (def is MethodDef)
				Analyze(service, context, parameters, (MethodDef)def);
			else if (def is FieldDef)
				Analyze(service, context, parameters, (FieldDef)def);
			else if (def is PropertyDef)
				Analyze(service, context, parameters, (PropertyDef)def);
			else if (def is EventDef)
				Analyze(service, context, parameters, (EventDef)def);
			else if (def is ModuleDef) {
				var renamingMode = parameters.GetParameter<RenameMode>(context, def, "mode");
				if (renamingMode == RenameMode.Reversible && service.reversibleRenamer == null) {
					var generatePassword = parameters.GetParameter<bool>(context, def, "generatePassword");
					var password = parameters.GetParameter<string>(context, def, "password");
					if (generatePassword || password == null) {
						password = context.Registry.GetService<IRandomService>().SeedString;
					}
					string dir = context.OutputDirectory;
					string path = Path.GetFullPath(Path.Combine(dir, CoreComponent.PasswordFileName));
					if (!Directory.Exists(dir))
						Directory.CreateDirectory(dir);
					File.WriteAllText(path, password);
					service.reversibleRenamer = new ReversibleRenamer(password);
				}

				service.SetCanRename(def, false);
			}

			if (!runAnalyzer || parameters.GetParameter(context, def, "forceRen", false))
				return;

			foreach (IRenamer renamer in service.Renamers)
				renamer.Analyze(context, service, parameters, def);
		}

		static bool IsVisibleOutside(ConfuserContext context, ProtectionParameters parameters, IMemberDef def) {
			var type = def as TypeDef;
			if (type == null)
				type = def.DeclaringType;

			var renPublic = parameters.GetParameter<bool?>(context, def, "renPublic", null);
			if (renPublic == null)
				return type.IsVisibleOutside();
			else
				return type.IsVisibleOutside(false) && !renPublic.Value;
		}

		void Analyze(NameService service, ConfuserContext context, ProtectionParameters parameters, TypeDef type) {
			if (IsVisibleOutside(context, parameters, type)) {
				service.SetCanRename(type, false);
			}
			else if (type.IsRuntimeSpecialName || type.IsGlobalModuleType) {
				service.SetCanRename(type, false);
			}

			if (parameters.GetParameter(context, type, "forceRen", false))
				return;

			if (type.InheritsFromCorlib("System.Attribute")) {
				service.ReduceRenameMode(type, RenameMode.Reflection);
			}

			if (type.InheritsFrom("System.Configuration.SettingsBase")) {
				service.SetCanRename(type, false);
			}

			if (type.HasAttribute("System.Runtime.Serialization.DataContractAttribute")) {
				service.SetCanRename(type, false);
			}
		}

		void Analyze(NameService service, ConfuserContext context, ProtectionParameters parameters, MethodDef method) {
			if (IsVisibleOutside(context, parameters, method.DeclaringType) &&
				(method.IsFamily || method.IsFamilyOrAssembly || method.IsPublic) &&
				IsVisibleOutside(context, parameters, method))
				service.SetCanRename(method, false);

			else if (method.IsRuntimeSpecialName)
				service.SetCanRename(method, false);

			else if (method.IsExplicitlyImplementedInterfaceMember())
				service.SetCanRename(method, false);

			else if (parameters.GetParameter(context, method, "forceRen", false))
				return;

			else if (method.DeclaringType.IsComImport() && !method.HasAttribute("System.Runtime.InteropServices.DispIdAttribute"))
				service.SetCanRename(method, false);

			else if (method.DeclaringType.IsDelegate())
				service.SetCanRename(method, false);
		}

		void Analyze(NameService service, ConfuserContext context, ProtectionParameters parameters, FieldDef field) {
			if (IsVisibleOutside(context, parameters, field.DeclaringType) &&
				(field.IsFamily || field.IsFamilyOrAssembly || field.IsPublic) &&
				IsVisibleOutside(context, parameters, field))
				service.SetCanRename(field, false);

			else if (field.IsRuntimeSpecialName)
				service.SetCanRename(field, false);

			else if (parameters.GetParameter(context, field, "forceRen", false))
				return;

			else if (field.DeclaringType.IsSerializable && !field.IsNotSerialized)
				service.SetCanRename(field, false);

			else if (field.IsLiteral && field.DeclaringType.IsEnum &&
				!parameters.GetParameter(context, field, "renEnum", false))
				service.SetCanRename(field, false);

			else if (field.HasAttribute("System.Runtime.Serialization.DataMemberAttribute"))
				service.SetCanRename(field, false);

			else if (field.HasAttribute("System.Runtime.Serialization.EnumMemberAttribute"))
				service.SetCanRename(field, false);
		}

		void Analyze(NameService service, ConfuserContext context, ProtectionParameters parameters, PropertyDef property) {
			if (IsVisibleOutside(context, parameters, property.DeclaringType) &&
				(property.IsFamily() || property.IsFamilyOrAssembly() || property.IsPublic()) &&
				IsVisibleOutside(context, parameters, property))
				service.SetCanRename(property, false);

			else if (property.IsRuntimeSpecialName)
				service.SetCanRename(property, false);

			else if (parameters.GetParameter(context, property, "forceRen", false))
				return;

			else if (property.DeclaringType.Implements("System.ComponentModel.INotifyPropertyChanged"))
				service.SetCanRename(property, false);

			else if (property.DeclaringType.Name.String.Contains("AnonymousType"))
				service.SetCanRename(property, false);

			else if (property.HasAttribute("System.Runtime.Serialization.DataMemberAttribute"))
				service.SetCanRename(property, false);
		}

		void Analyze(NameService service, ConfuserContext context, ProtectionParameters parameters, EventDef evt) {
			if (IsVisibleOutside(context, parameters, evt.DeclaringType) &&
				(evt.IsFamily() || evt.IsFamilyOrAssembly() || evt.IsPublic()) &&
				IsVisibleOutside(context, parameters, evt))
				service.SetCanRename(evt, false);

			else if (evt.IsRuntimeSpecialName)
				service.SetCanRename(evt, false);
		}
	}
}
