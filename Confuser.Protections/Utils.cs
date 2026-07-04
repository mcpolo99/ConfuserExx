using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Confuser.Core;
using dnlib.DotNet;

namespace Confuser.Protections {
	internal static class Utils {
		public static IMethod Import(this ModuleDef module, ConfuserContext context, Type classType, string method) {
			var corLib = context.Resolver.Resolve(context.CurrentModule?.CorLibTypes.AssemblyRef, context.CurrentModule);
			var typeInfo = corLib?.ManifestModule.Find(classType.FullName, true);
			if (typeInfo != null)
				return module.Import(typeInfo.FindMethod(method));

			// CX001: intentional last-resort fallback. When the target module's corlib type cannot be
			// resolved through the context (rare), host reflection is the only available source for the
			// member reference. New code must resolve through the context instead of copying this.
#pragma warning disable CX001
			return module.Import(classType.GetMethod(method));
#pragma warning restore CX001
		}
	}
}
