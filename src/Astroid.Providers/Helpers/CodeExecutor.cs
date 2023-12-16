using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;

namespace Astroid.Providers;

public class CodeExecutor
{
	private readonly string CodeTemplate = @"
		using System;
		using System.Collections.Generic;
		using System.Linq;
		using Astroid.Providers;

		namespace Astroid.Providers;

		public class DynamicClass
		{{
			public {0} Execute({1})
			{{
				{2}
			}}
		}}
	";

	public CodeExecutionResult<T> Execute<T>(string code, params object[] args)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(code);
		var assemblyName = Path.GetRandomFileName();
		var rootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new Exception("Could not find assembly path");
		if (rootPath == null)
			return CodeExecutionResult<T>.Error("Could not find root path");

		var executingAssemblyReferences = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
		var references = executingAssemblyReferences
			.Select(assemblyName => MetadataReference.CreateFromFile(Assembly.Load(assemblyName).Location))
			.ToList();

		references.AddRange(new[] {
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(AMOrderBookEntry).Assembly.Location),
		});

		var compilation = CSharpCompilation.Create(
			assemblyName,
			syntaxTrees: new[] { syntaxTree },
			references: references,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		using var ms = new MemoryStream();
		var result = compilation.Emit(ms);

		if (!result.Success)
		{
			var errors = result.Diagnostics
				.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
				.Select(x => x.GetMessage())
				.ToList();

			return CodeExecutionResult<T>.Error($"Compilation Error: {errors.FirstOrDefault()}", errors);
		}

		ms.Seek(0, SeekOrigin.Begin);
		var assembly = Assembly.Load(ms.ToArray());
		var a = assembly.GetTypes();
		var type = assembly.GetType("Astroid.Providers.DynamicClass");
		var method = type!.GetMethod("Execute");
		var instance = Activator.CreateInstance(type);
		var data = method!.Invoke(instance, args);

		return CodeExecutionResult<T>.Success((T)data!);
	}

	public CodeExecutionResult<decimal> ExecuteComputationMethod(string code, params object[] args)
	{
		code = string.Format(CodeTemplate, "decimal", "List<AMOrderBookEntry> entries", code);
		var result = Execute<decimal>(code, args);

		return result;
	}
}

public class CodeExecutionResult<T>
{
	public bool IsSuccess { get; set; }
	public string? Message { get; set; }
	public List<string> Errors { get; set; } = new();
	public T? Data { get; set; }

	public static CodeExecutionResult<T> Success(T? result) => new()
	{
		IsSuccess = true,
		Data = result
	};

	public static CodeExecutionResult<T> Error(string message, List<string>? errors = default)
	{
		errors ??= new();

		return new()
		{
			IsSuccess = false,
			Message = message,
			Errors = errors
		};
	}
}
