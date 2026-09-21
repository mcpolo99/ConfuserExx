using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Confuser.Core;
using Confuser.Core.Helpers;
using Confuser.Core.Services;
using Confuser.Protections.Compress;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.PE;
using Microsoft.Extensions.Logging;
using FileAttributes = dnlib.DotNet.FileAttributes;
using SR = System.Reflection;

namespace Confuser.Protections {
	internal class Compressor : Packer {
		public const string _Id = "compressor";
		public const string _FullId = "Ki.Compressor";
		public const string _ServiceId = "Ki.Compressor";
		public static readonly object ContextKey = new object();

		public override string Name {
			get { return "Compressing Packer"; }
		}

		public override string Description {
			get { return "This packer reduces the size of output."; }
		}

		public override string Id {
			get { return _Id; }
		}

		public override string FullId {
			get { return _FullId; }
		}

		protected override void Initialize(ConfuserContext context) { }

		protected override void PopulatePipeline(ProtectionPipeline pipeline) {
			pipeline.InsertPreStage(PipelineStage.WriteModule, new ExtractPhase(this));
		}

		protected override void Pack(ConfuserContext context, ProtectionParameters parameters) {
			var ctx = context.Annotations.Get<CompressorContext>(context, ContextKey);
			if (ctx == null) {
				context.Logger.LogError("No executable module!");
				throw new ConfuserException(null);
			}

			ModuleDefMD originModule = context.Modules[ctx.ModuleIndex];
			ctx.OriginModuleDef = originModule;

			var stubModule = new ModuleDefUser(ctx.ModuleName, originModule.Mvid, originModule.CorLibTypes.AssemblyRef);
			if (ctx.CompatMode) {
				var assembly = new AssemblyDefUser(originModule.Assembly);
				assembly.Name += ".cr";
				assembly.Modules.Add(stubModule);
			}
			else {
				ctx.Assembly.Modules.Insert(0, stubModule);
				ImportAssemblyTypeReferences(originModule, stubModule);
			}
			stubModule.Characteristics = originModule.Characteristics;
			stubModule.Cor20HeaderFlags = originModule.Cor20HeaderFlags;
			stubModule.Cor20HeaderRuntimeVersion = originModule.Cor20HeaderRuntimeVersion;
			stubModule.DllCharacteristics = originModule.DllCharacteristics;
			stubModule.EncBaseId = originModule.EncBaseId;
			stubModule.EncId = originModule.EncId;
			stubModule.Generation = originModule.Generation;
			stubModule.Kind = ctx.Kind;
			stubModule.Machine = originModule.Machine;
			stubModule.RuntimeVersion = originModule.RuntimeVersion;
			stubModule.TablesHeaderVersion = originModule.TablesHeaderVersion;
			stubModule.Win32Resources = originModule.Win32Resources;

			InjectStub(context, ctx, parameters, stubModule);

			var snKey = context.Annotations.Get<StrongNameKey>(originModule, Marker.SNKey);
			var snPubKey = context.Annotations.Get<StrongNamePublicKey>(originModule, Marker.SNPubKey);
			var snDelaySig = context.Annotations.Get<bool>(originModule, Marker.SNDelaySig, false);
			var snSigKey = context.Annotations.Get<StrongNameKey>(originModule, Marker.SNSigKey);
			var snPubSigKey = context.Annotations.Get<StrongNamePublicKey>(originModule, Marker.SNSigPubKey);

			using (var ms = new MemoryStream()) {
				var options = new ModuleWriterOptions(stubModule) {
					StrongNameKey = snKey,
					StrongNamePublicKey = snPubKey,
					DelaySign = snDelaySig
				};
				var injector = new KeyInjector(ctx);
				options.WriterEvent += injector.WriterEvent;

				stubModule.Write(ms, options);
				context.CheckCancellation();
				ProtectStub(context, context.OutputPaths[ctx.ModuleIndex], ms.ToArray(), snKey, snPubKey, snSigKey, snPubKey, snDelaySig, new StubProtection(ctx, originModule));
			}
		}

		static string GetId(byte[] module) {
			var md = MetadataFactory.CreateMetadata(new PEImage(module));
			var assembly = new AssemblyNameInfo();
			if (md.TablesStream.TryReadAssemblyRow(1, out var assemblyRow)) {
				assembly.Name = md.StringsStream.ReadNoNull(assemblyRow.Name);
				assembly.Culture = md.StringsStream.ReadNoNull(assemblyRow.Locale);
				assembly.PublicKeyOrToken = new PublicKey(md.BlobStream.Read(assemblyRow.PublicKey));
				assembly.HashAlgId = (AssemblyHashAlgorithm)assemblyRow.HashAlgId;
				assembly.Version = new Version(assemblyRow.MajorVersion, assemblyRow.MinorVersion, assemblyRow.BuildNumber, assemblyRow.RevisionNumber);
				assembly.Attributes = (AssemblyAttributes)assemblyRow.Flags;
			}
			return GetId(assembly);
		}

		static string GetId(IAssembly assembly) {
			return new SR.AssemblyName(assembly.FullName).FullName.ToUpperInvariant();
		}

		void PackModules(ConfuserContext context, CompressorContext compCtx, ModuleDef stubModule, ICompressionService comp, RandomGenerator random) {
			int maxLen = 0;
			var modules = new Dictionary<string, byte[]>();
			for (int i = 0; i < context.OutputModules.Count; i++) {
				if (i == compCtx.ModuleIndex)
					continue;

				string id = GetId(context.Modules[i].Assembly);
				modules.Add(id, context.OutputModules[i]);

				int strLen = Encoding.UTF8.GetByteCount(id);
				if (strLen > maxLen)
					maxLen = strLen;
			}
			foreach (var extModule in context.ExternalModules) {
				var name = GetId(extModule).ToUpperInvariant();
				modules.Add(name, extModule);

				int strLen = Encoding.UTF8.GetByteCount(name);
				if (strLen > maxLen)
					maxLen = strLen;
			}

			byte[] key = random.NextBytes(4 + maxLen);
			key[0] = (byte)(compCtx.EntryPointToken >> 0);
			key[1] = (byte)(compCtx.EntryPointToken >> 8);
			key[2] = (byte)(compCtx.EntryPointToken >> 16);
			key[3] = (byte)(compCtx.EntryPointToken >> 24);
			for (int i = 4; i < key.Length; i++) // no zero bytes
				key[i] |= 1;
			compCtx.KeySig = key;

			int moduleIndex = 0;
			foreach (var entry in modules) {
				byte[] name = Encoding.UTF8.GetBytes(entry.Key);
				for (int i = 0; i < name.Length; i++)
					name[i] *= key[i + 4];

				uint state = compCtx.LcgInit;
				foreach (byte chr in name)
					state = state * compCtx.LcgMultiplier + chr;
				byte[] encrypted = compCtx.Encrypt(comp, entry.Value, state, progress => {
					progress = (progress + moduleIndex) / modules.Count;
					context.ProgressReporter.Progress((int)(progress * 10000), 10000);
				});
				context.CheckCancellation();

				var resource = new EmbeddedResource(Convert.ToBase64String(name), encrypted, ManifestResourceAttributes.Private);
				stubModule.Resources.Add(resource);
				moduleIndex++;
			}
			context.ProgressReporter.EndProgress();
		}

		void InjectData(ConfuserContext context, ModuleDef stubModule, MethodDef method, byte[] data) {
			var dataType = new TypeDefUser("", "DataType", stubModule.CorLibTypes.GetTypeRef("System", "ValueType"));
			dataType.Layout = TypeAttributes.ExplicitLayout;
			dataType.Visibility = TypeAttributes.NotPublic;
			dataType.IsSealed = true;
			dataType.ClassLayout = new ClassLayoutUser(1, (uint)data.Length);
			stubModule.Types.Add(dataType);

			var dataField = new FieldDefUser("DataField", new FieldSig(dataType.ToTypeSig())) {
				IsStatic = true,
				HasFieldRVA = true,
				InitialValue = data,
				Access = FieldAttributes.CompilerControlled
			};
			stubModule.GlobalType.Fields.Add(dataField);

			MutationHelper.ReplacePlaceholder(method, arg => {
				var repl = new List<Instruction>();
				repl.AddRange(arg);
				repl.Add(Instruction.Create(OpCodes.Dup));
				repl.Add(Instruction.Create(OpCodes.Ldtoken, dataField));
				repl.Add(Instruction.Create(OpCodes.Call, stubModule.Import(
					context, typeof(RuntimeHelpers), "InitializeArray")));
				return repl.ToArray();
			});
		}

		void InjectStub(ConfuserContext context, CompressorContext compCtx, ProtectionParameters parameters, ModuleDef stubModule) {
			var rt = context.Registry.GetService<IRuntimeService>();
			RandomGenerator random = context.Registry.GetService<IRandomService>().GetRandomGenerator(Id);
			var comp = context.Registry.GetService<ICompressionService>();

			var rtType = rt.GetRuntimeType(compCtx.CompatMode ? "Confuser.Runtime.CompressorCompat" : "Confuser.Runtime.Compressor");
			IEnumerable<IDnlibDef> defs = InjectHelper.Inject(rtType, stubModule.GlobalType, stubModule);

			switch (parameters.GetParameter(context, context.CurrentModule, "key", Mode.Normal)) {
				case Mode.Normal:
					compCtx.Deriver = new NormalDeriver();
					break;
				case Mode.Dynamic:
					compCtx.Deriver = new DynamicDeriver();
					break;
				default:
					throw new UnreachableException();
			}
			compCtx.Deriver.Init(context, random);

			context.Logger.LogDebug("Encrypting modules...");

			// Main
			MethodDef entryPoint = defs.OfType<MethodDef>().Single(method => method.Name == "Main");
			stubModule.EntryPoint = entryPoint;

			if (compCtx.EntryPoint.HasAttribute("System.STAThreadAttribute")) {
				var attrType = stubModule.CorLibTypes.GetTypeRef("System", "STAThreadAttribute");
				var ctorSig = MethodSig.CreateInstance(stubModule.CorLibTypes.Void);
				entryPoint.CustomAttributes.Add(new CustomAttribute(
					new MemberRefUser(stubModule, ".ctor", ctorSig, attrType)));
			}
			else if (compCtx.EntryPoint.HasAttribute("System.MTAThreadAttribute")) {
				var attrType = stubModule.CorLibTypes.GetTypeRef("System", "MTAThreadAttribute");
				var ctorSig = MethodSig.CreateInstance(stubModule.CorLibTypes.Void);
				entryPoint.CustomAttributes.Add(new CustomAttribute(
					new MemberRefUser(stubModule, ".ctor", ctorSig, attrType)));
			}

			// Randomize the encryption feedback constant (a fixed value in the stub is a
			// fingerprint). Any value works since it is purely additive; the same value is injected
			// into the runtime Decrypt method below (Mutation.KeyI0) so encrypt/decrypt stay in sync.
			compCtx.Feedback = random.NextUInt32();

			// Randomize the rolling-hash used to derive each library's seed from its name. The
			// multiplier is kept odd for good distribution. The same values are injected into the
			// runtime Resolve method below (Mutation.KeyI0 = init, Mutation.KeyI1 = multiplier).
			compCtx.LcgInit = random.NextUInt32();
			compCtx.LcgMultiplier = random.NextUInt32() | 1;

			// Randomize the key-derivation generator moduli (were the fixed 0x143fc089 / 0x444d56fb
			// / 0x8a5cb7). Picked from curated sets that preserve key-stream quality; the same values
			// are injected into the runtime Decrypt below (Mutation.KeyI1 / KeyI2 / KeyI3).
			compCtx.StatePrime = CompressorPrimes.State[random.NextInt32(CompressorPrimes.State.Length)];
			compCtx.KeyWordPrime = CompressorPrimes.KeyWord[random.NextInt32(CompressorPrimes.KeyWord.Length)];
			compCtx.StreamPrime = CompressorPrimes.Stream[random.NextInt32(CompressorPrimes.Stream.Length)];

			uint seed = random.NextUInt32();
			compCtx.OriginModule = context.OutputModules[compCtx.ModuleIndex];

			byte[] encryptedModule = compCtx.Encrypt(comp, compCtx.OriginModule, seed,
													 progress => context.ProgressReporter.Progress((int)(progress * 10000), 10000));
			context.ProgressReporter.EndProgress();
			context.CheckCancellation();

			compCtx.EncryptedModule = encryptedModule;

			MutationHelper.InjectKeys(entryPoint,
									  new[] { 0, 1 },
									  new[] { encryptedModule.Length >> 2, (int)seed });
			InjectData(context, stubModule, entryPoint, encryptedModule);

			// Decrypt
			MethodDef decrypter = defs.OfType<MethodDef>().Single(method => method.Name == "Decrypt");
			decrypter.Body.SimplifyMacros(decrypter.Parameters);
			List<Instruction> instrs = decrypter.Body.Instructions.ToList();
			for (int i = 0; i < instrs.Count; i++) {
				Instruction instr = instrs[i];
				if (instr.OpCode == OpCodes.Call) {
					var method = (IMethod)instr.Operand;
					if (method.DeclaringType.Name == "Mutation" &&
						method.Name == "Crypt") {
						Instruction ldDst = instrs[i - 2];
						Instruction ldSrc = instrs[i - 1];
						Debug.Assert(ldDst.OpCode == OpCodes.Ldloc && ldSrc.OpCode == OpCodes.Ldloc);
						instrs.RemoveAt(i);
						instrs.RemoveAt(i - 1);
						instrs.RemoveAt(i - 2);
						instrs.InsertRange(i - 2, compCtx.Deriver.EmitDerivation(decrypter, context, (Local)ldDst.Operand, (Local)ldSrc.Operand));
					}
					else if (method.DeclaringType.Name == "Lzma" &&
							 method.Name == "Decompress") {
						MethodDef decomp = comp.GetRuntimeDecompressor(stubModule, member => { });
						instr.Operand = decomp;
					}
				}
			}
			decrypter.Body.Instructions.Clear();
			foreach (Instruction instr in instrs)
				decrypter.Body.Instructions.Add(instr);

			// Sync the runtime Decrypt constants with the values used during encryption: the feedback
			// constant (KeyI0) and the three key-derivation moduli (KeyI1/KeyI2/KeyI3). The runtime
			// exposes them as Mutation.KeyI* placeholders; injection is verified so a runtime-source
			// change that drops a placeholder fails the build loudly instead of silently shipping a
			// stub that can no longer decrypt what we encrypted.
			InjectStubKeys(context, decrypter, new[] { 0, 1, 2, 3 },
						   new[] {
							   (int)compCtx.Feedback, (int)compCtx.StatePrime,
							   (int)compCtx.KeyWordPrime, (int)compCtx.StreamPrime
						   });

			// Sync the runtime rolling-hash constants (init + odd multiplier) used to derive each
			// library's seed from its name (see PackModules) with the Mutation.KeyI0 / KeyI1
			// placeholders the runtime Resolve exposes.
			MethodDef resolver = defs.OfType<MethodDef>().Single(method => method.Name == "Resolve");
			InjectStubKeys(context, resolver, new[] { 0, 1 },
						   new[] { (int)compCtx.LcgInit, (int)compCtx.LcgMultiplier });

			// Pack modules
			PackModules(context, compCtx, stubModule, comp, random);
		}

		// Injects mutation key literals into an injected runtime stub method and first verifies
		// every expected Mutation.KeyI* placeholder is actually present. If the runtime source is
		// changed so a placeholder is removed or renamed, this throws instead of silently emitting
		// a stub whose baked-in constants no longer match the obfuscator side.
		static void InjectStubKeys(ConfuserContext context, MethodDef method, int[] keyIds, int[] values) {
			var missing = new HashSet<string>(
				keyIds.Select(id => "KeyI" + id.ToString(CultureInfo.InvariantCulture)));
			foreach (Instruction instr in method.Body.Instructions) {
				if (instr.OpCode == OpCodes.Ldsfld && instr.Operand is IField field &&
					field.DeclaringType?.FullName == "Mutation")
					missing.Remove(field.Name);
			}
			if (missing.Count != 0) {
				context.Logger.LogError(
					"Compressor stub is out of sync: runtime method '{Method}' is missing expected mutation placeholder(s) {Missing}. The runtime source and the injector must be updated together.",
					method.Name, string.Join(", ", missing.OrderBy(name => name, StringComparer.Ordinal)));
				throw new ConfuserException(null);
			}

			MutationHelper.InjectKeys(method, keyIds, values);
		}

		void ImportAssemblyTypeReferences(ModuleDef originModule, ModuleDef stubModule) {
			var assembly = stubModule.Assembly;
			foreach (var ca in assembly.CustomAttributes) {
				if (ca.AttributeType.Scope == originModule)
					ca.Constructor = (ICustomAttributeType)stubModule.Import(ca.Constructor);
			}
			foreach (var ca in assembly.DeclSecurities.SelectMany(declSec => declSec.CustomAttributes)) {
				if (ca.AttributeType.Scope == originModule)
					ca.Constructor = (ICustomAttributeType)stubModule.Import(ca.Constructor);
			}
		}

		class KeyInjector {
			readonly CompressorContext ctx;

			public KeyInjector(CompressorContext ctx) {
				this.ctx = ctx;
			}

			public void WriterEvent(object sender, ModuleWriterEventArgs args) {
				OnWriterEvent(args.Writer, args.Event);
			}

			private void OnWriterEvent(ModuleWriterBase writer, ModuleWriterEvent evt) {
				if (evt == ModuleWriterEvent.MDBeginCreateTables) {
					// Add key signature
					uint sigBlob = writer.Metadata.BlobHeap.Add(ctx.KeySig);
					uint sigRid = writer.Metadata.TablesHeap.StandAloneSigTable.Add(new RawStandAloneSigRow(sigBlob));
					Debug.Assert(sigRid == 1);
					uint sigToken = 0x11000000 | sigRid;
					ctx.KeyToken = sigToken;
					MutationHelper.InjectKey(writer.Module.EntryPoint, 2, (int)sigToken);
				}
				else if (evt == ModuleWriterEvent.MDBeginAddResources && !ctx.CompatMode) {
					// Compute hash
					byte[] hash = SHA1.Create().ComputeHash(ctx.OriginModule);
					uint hashBlob = writer.Metadata.BlobHeap.Add(hash);

					MDTable<RawFileRow> fileTbl = writer.Metadata.TablesHeap.FileTable;
					uint fileRid = fileTbl.Add(new RawFileRow(
												   (uint)FileAttributes.ContainsMetadata,
												   writer.Metadata.StringsHeap.Add("koi"),
												   hashBlob));
					uint impl = CodedToken.Implementation.Encode(new MDToken(Table.File, fileRid));

					// Add resources
					MDTable<RawManifestResourceRow> resTbl = writer.Metadata.TablesHeap.ManifestResourceTable;
					foreach (var resource in ctx.ManifestResources)
						resTbl.Add(new RawManifestResourceRow(resource.Offset, resource.Flags, writer.Metadata.StringsHeap.Add(resource.Value), impl));

					// Add exported types
					var exTbl = writer.Metadata.TablesHeap.ExportedTypeTable;
					foreach (var type in ctx.OriginModuleDef.GetTypes()) {
						if (!type.IsVisibleOutside())
							continue;
						exTbl.Add(new RawExportedTypeRow((uint)type.Attributes, 0,
														 writer.Metadata.StringsHeap.Add(type.Name),
														 writer.Metadata.StringsHeap.Add(type.Namespace), impl));
					}
				}
			}
		}
	}
}
