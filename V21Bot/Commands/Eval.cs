using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using V21Bot.Helper;

namespace V21Bot.Commands
{
	public class Eval
	{
		static readonly string EvalNamespace = "V21Bot.Eval.Sandbox";
		static readonly string EvalClass = "EvalSandbox";
		static readonly string EvalFunction = "Execute";
		static readonly MetadataReference[] References;
		static Eval()
		{
			//Path of the dotnet
			string assemblyPath = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);

			List<MetadataReference> references = new List<MetadataReference>();
			references.AddRange(Directory.EnumerateFiles(assemblyPath, "System.*.dll").Select(x => MetadataReference.CreateFromFile(x)).ToArray());	//Standard System libraries
			references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "netstandard.dll")));										//Netstandard LIbraries
			references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "mscorlib.dll")));                                           //C#

			references.Add(MetadataReference.CreateFromFile(typeof(V21).GetTypeInfo().Assembly.Location));                                          //Self Library
			references.Add(MetadataReference.CreateFromFile(typeof(DiscordClient).GetTypeInfo().Assembly.Location));								//D#+
			references.Add(MetadataReference.CreateFromFile(typeof(CommandContext).GetTypeInfo().Assembly.Location));								//Command Next
			references.Add(MetadataReference.CreateFromFile(typeof(StackExchange.Redis.ClientInfo).GetTypeInfo().Assembly.Location));				//Redis Library
			references.Add(MetadataReference.CreateFromFile(typeof(ImageMagick.MagickImage).GetTypeInfo().Assembly.Location));						//MagickImage Library
			References = references.ToArray();
		}

		struct Evaluation
		{
			//public Func<CommandContext, Task<object>> function;
			public Evaluatable evaluatable;
			public IEnumerable<Diagnostic> errors;
			public bool compiled;
			public long duration;
		}

		readonly string[] EvalIncludes = new string[] 
		{
			"System",
			"System.Collections.Generic",
			"System.Linq",
			"System.Threading.Tasks",		
			"DSharpPlus.CommandsNext",
			"V21Bot.Redis",
			"V21Bot.Helper",
			"V21Bot.Imgur",
			"DSharpPlus.Entities"
		};

		[Command("evaluate")]
		[Aliases("eval", "$")]
		[Description("Evaluates C#")]
		[RequireOwner]
		public async Task EvaluateRoslyn(CommandContext ctx, [RemainingText] string message)
		{
			string code = "";
			if (message.Contains("\n"))
			{
				int charcount = 10;
				int start = message.IndexOf("```csharp\n");
				if (start < 0)
				{
					start = message.IndexOf("```cs\n");
					charcount = 6;
				}

				int end = message.LastIndexOf("\n```");
				if (start >= 0 && end >= 0)
				{
					message = message.Substring(start + charcount, end - start - charcount);
					message = message.Trim(' ', '\r', '\n', '\t');
					code = GenerateClass(message);
				}
				else
				{
					await ctx.RespondAsync(":interrobang: Unable to evaluate the code as it does not contain a valid codeblock. Multiline statements must be within a C# codeblock.");
					return;
				}
			}
			else
			{
				message = message.Trim(';', ' ', '\r', '\n', '\t');
				if (message.StartsWith("return")) message = message.Substring(6);
				code = GenerateClass("return " + message +";");
			}

			ResponseBuilder response = new ResponseBuilder(ctx);
			response.WithDescription("Evaluating C# Code: ```csharp\r\n{0}\r\n```", message)
					.AddField("Compile Time", "Compiling...", true)
					.AddField("Execute Time", "??", true)
					.WithColor(DiscordColor.LightGray);
			var msg = await ctx.RespondAsync(embed: response);

			//Compile the class and edit the message to the current value
			await ctx.TriggerTypingAsync();
			Evaluation eval = EvaluateClass(code);

			//Update the message
			response = new ResponseBuilder(ctx);
			response.WithDescription("Evaluating C# Code: ```csharp\r\n{0}\r\n```", message)
					.AddField("Compile Time", eval.duration + "ms", true)
					.AddField("Execute Time", eval.compiled ? "Executing..." : "N/A", true)
					.WithColor(eval.compiled ? DiscordColor.Orange : DiscordColor.Red);
			await msg.ModifyAsync(embed: response);

			if (eval.compiled)
			{
				//Update the trigger so they know we are working
				await ctx.TriggerTypingAsync();

				//Prepare our outputs
				Exception exception = null;
				object output = null;

				//Time how long it takes to execute
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();

				//Evaluate the contents
				try
				{
					//Attempt to execute and store our output
					output = await eval.evaluatable.Evaluate(ctx);
				}
				catch(Exception e)
				{
					//Store the E, so we know we failed executing.
					exception = e;
				}

				//Stop the stopwatch
				stopwatch.Stop();

				//Log our output
				//Update the message
				response = new ResponseBuilder(ctx);
				response.WithDescription("Evaluating C# Code: ```csharp\r\n{0}\r\n```\r\n{1}", message, !string.IsNullOrEmpty(eval.evaluatable.output) ? "Output: ```\r\n" + eval.evaluatable.output + "\r\n```" : "")
						.AddField("Compile Time", eval.duration + "ms", true)
						.AddField("Execute Time", stopwatch.ElapsedMilliseconds + "ms", true)
						.WithColor(exception == null ? DiscordColor.Green : DiscordColor.Red);
				await msg.ModifyAsync(embed: response);
				
				//Log out new output
				if (output != null)
				{
					await ctx.RespondAsync("```csharp\r\n" + output.ToString() + "\r\n```");
				}
				else
				{
					await ctx.RespondAsync("```haskell\r\n<no response>\r\n```");
				}

				//Log out the exception
				if (exception != null)
				{
					await ctx.RespondException(exception);
				}
			}
			else
			{
				StringBuilder errorBuilder = new StringBuilder();
				errorBuilder.Append("```haskel");
				foreach (var diagnostic in eval.errors)
				{
					string c = string.Format("\r\n{2}: [{0}] {1}", diagnostic.Id, diagnostic.GetMessage(), diagnostic.Location.GetLineSpan().StartLinePosition.Line);
					errorBuilder.Append(c);
				}
				errorBuilder.Append("\r\n```");
				await ctx.RespondAsync(embed: new ResponseBuilder(ctx).WithColor(DiscordColor.Red).WithDescription("Compile Failure:" + errorBuilder.ToString()));
			}
		}

		private Evaluation EvaluateClass(string source)
		{
			//Generate and start the stopwatch
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
						
			//Prepare the compilatin unit
			CSharpCompilation compilation = CSharpCompilation.Create(
				Path.GetRandomFileName() + ".dll",
				syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
				references: References,
				options: new CSharpCompilationOptions(
					outputKind: OutputKind.DynamicallyLinkedLibrary,
					usings: EvalIncludes,
					optimizationLevel: OptimizationLevel.Debug,
					platform: Platform.AnyCpu
					)
				);

			using (var ms = new MemoryStream())
			{
				EmitResult result = compilation.Emit(ms);

				if (!result.Success)
				{
					//Compile failed, so we will debug the errors
					IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
						diagnostic.IsWarningAsError ||
						diagnostic.Severity == DiagnosticSeverity.Error);

					//Put the errors in the class and return our results.
					return new Evaluation()
					{
						errors = failures,
						duration = stopwatch.ElapsedMilliseconds,
						compiled = false
					};
				}
				else
				{
					//Reseek to the start of the memory stream
					ms.Seek(0, SeekOrigin.Begin);

					//Load the assembly from MS and prepare its fullname
					Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
					string fullname = string.Format("{0}.{1}", EvalNamespace, EvalClass);

					//get a instance of the type
					var type = assembly.GetType(fullname);
					var instance = (Evaluatable) assembly.CreateInstance(fullname);
					instance.source = source;

					//Find the executing method and return a delegate for it (so we can execute it later).
					return new Evaluation()
					{
						evaluatable = instance,
						duration = stopwatch.ElapsedMilliseconds,
						compiled = true
					};
				}
			}
		}
			   
		/// <summary>
		/// Generates a class from the supplied code. Attempts to sanatize the code.
		/// </summary>
		/// <param name="code"></param>
		/// <returns></returns>
		public string GenerateClass(string code)
		{
			StringBuilder builder = new StringBuilder();

			//Find all the includes from the script. We dont want repeats
			HashSet<string> includes = new HashSet<string>(EvalIncludes);
			string[] statements = code.Split(';');
			
			int statement_length = 0;
			foreach (string statement in statements)
			{
				var trimming = statement.Trim(' ', '\t', '\r', '\n');
				if (!trimming.StartsWith("using")) break;
				includes.Add(trimming.Substring(6));
				statement_length += statement.Length + 1;
			}

			code = code.Substring(statement_length);

			//Add the usings fields
			foreach (var u in includes)
				builder.Append("using ").Append(u).Append(";");

			//Add the namespace
			builder.Append("namespace ").Append(EvalNamespace).Append("{");
			{
				//Add the class
				builder.Append("class ").Append(EvalClass).Append(" : V21Bot.Commands.Evaluatable {");
				{
					//Append the function
					builder.Append("public override async Task<object> ").Append(EvalFunction).Append("(){");
					{
						if (!code.Contains("await ")) builder.Append("await Task.Delay(0);");
						builder.Append(code);
						if (!code.Contains("return ")) builder.Append("; return null;");
					}
					builder.Append("}");
				}
				builder.Append("}");
			}
			builder.Append("}");
			return builder.ToString();
			
		}
	}
	
	public abstract class Evaluatable
	{
		public string source { get; set; }
		public CommandContext ctx { get; private set; }
		public DiscordClient client { get { return ctx.Client; } }
		public DiscordMember member { get { return ctx.Member; } }
		public DiscordUser user { get { return ctx.User; } }
		public DiscordGuild guild { get { return ctx.Guild; } }
		public DiscordChannel channel { get { return ctx.Channel; } }
		public V21 v21 { get { return V21.Instance; } }
		public string resources => v21.Config.Resources;

		public string output { get { return Console.GetOutput(); } }

		public abstract Task<object> Execute();

		public virtual async Task<object> Evaluate(CommandContext ctx)
		{
			Console.Rebuild();
			this.ctx = ctx;
			return await Execute();
		}

		public async Task<DiscordMessage> Respond(string content = null, DiscordEmbed embed = null) { return await ctx.RespondAsync(content: content, embed: embed); }
		public async Task<DiscordMessage> RespondWithFileAsync(string filename, Action<Stream> action, string content = null, DiscordEmbed embed = null)
		{
			await ctx.TriggerTypingAsync();
			return await Task.Run(async() =>
			{
				using (MemoryStream mem = new MemoryStream())
				{
					action(mem);
					mem.Seek(0, SeekOrigin.Begin);
					return await ctx.RespondWithFileAsync(mem, filename, content: content, embed: embed);
				}
			});			
		}

		public string GetProperties(object obj) { return GetProperties(obj.GetType()); }
		public string GetProperties<T>() { return GetProperties(typeof(T)); }
		public string GetProperties(Type type)
		{
			var properties = type.GetProperties();
			return string.Join("\r\n", properties.Select(p => string.Format("{0} {1} {2}", GetTypeName(p.PropertyType), p.Name, p.CanRead && p.CanWrite ? "{ get; set; }" : (p.CanRead ? "{ get; }" : "{ set; }"))));
		}

		public string GetFields(object obj) { return GetFields(obj.GetType()); }
		public string GetFields<T>() { return GetFields(typeof(T)); }
		public string GetFields(Type type)
		{
			var properties = type.GetFields();
			return string.Join("\r\n", properties.Select(p => string.Format("{0} {1}", GetTypeName(p.FieldType), p.Name)));
		}

		public string GetMethods(object obj) { return GetMethods(obj.GetType()); }
		public string GetMethods<T>() { return GetMethods(typeof(T)); }
		public string GetMethods(Type type)
		{
			bool isFirstMethod = true;
			StringBuilder output = new StringBuilder();
			foreach (var method in type.GetMethods())
			{
				if (isFirstMethod) isFirstMethod = false;
				else output.Append("\r\n");

				if (method.IsPublic) output.Append("public ");
				if (method.IsPrivate) output.Append("private ");
				if (method.IsVirtual) output.Append("static ");
				if (method.IsVirtual) output.Append("virtual ");
				if (method.IsAbstract) output.Append("abstract ");
				
				output.Append(GetTypeName(method.ReturnType)).Append(" ").Append(method.Name);
				if (method.IsGenericMethod) output.Append("<T>");

				output.Append("(");

				bool isFirstProperty = true;
				foreach(var p in method.GetParameters())
				{
					if (isFirstProperty) isFirstProperty = false;
					else output.Append(", ");

					if (p.IsOut) output.Append("out ");					
					output.Append(GetTypeName(p.ParameterType)).Append(" ").Append(p.Name);
					if (p.IsOptional) output.Append("?");
				}

				output.Append(")");
			}

			return output.ToString();
		}

		public static string GetTypeName(Type type)
		{
			if (type == typeof(void))		return "void";
			if (type == typeof(string))		return "string";
			if (type == typeof(object))		return "object";
			if (type.IsGenericType) return type.Name + "<" + string.Join(", ", type.GenericTypeArguments.Select(t => GetTypeName(t))) + ">";


			if (!type.IsPrimitive)			return type.Name;			
			if (type == typeof(long))		return "long";
			if (type == typeof(ulong))		return "ulong";
			if (type == typeof(int))		return "int";
			if (type == typeof(uint))		return "uint";
			if (type == typeof(short))		return "short";
			if (type == typeof(ushort))		return "ushort";
			if (type == typeof(byte))		return "byte";
			if (type == typeof(float))		return "float";
			if (type == typeof(double))		return "double";
			if (type == typeof(float))		return "float";
			if (type == typeof(bool))		return "bool";
			return type.Name;
		}

		public class Console
		{
			private static StringBuilder builder;
			public static void Rebuild() { builder = new StringBuilder(); }
			public static string GetOutput() { return builder.ToString(); }

			public static void WriteLine() { builder.Append("\r\n"); }
			public static void WriteLine(object obj) { builder.Append(obj.ToString()).Append("\r\n"); }
			public static void WriteLine(string text) { builder.Append(text).Append("\r\n"); }
			public static void WriteLine(string format, params object[] objs) { builder.Append(string.Format(format, objs)).Append("\r\n"); }
			
			public static void Write(object obj) { builder.Append(obj.ToString()); }
			public static void Write(string text) { builder.Append(text); }
			public static void Write(string format, params object[] objs) { builder.Append(string.Format(format, objs)); }
		}
	}
	
}
