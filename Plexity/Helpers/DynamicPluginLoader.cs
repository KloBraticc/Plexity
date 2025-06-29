using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public class DynamicPluginLoaderFromStrings
{
    public Window LoadPluginWindowFromStrings(string xamlCode, string csCode)
    {
        if (string.IsNullOrWhiteSpace(xamlCode))
            throw new ArgumentException("XAML code cannot be null or empty.", nameof(xamlCode));
        if (string.IsNullOrWhiteSpace(csCode))
            throw new ArgumentException("Code-behind cannot be null or empty.", nameof(csCode));

        // Load Window from XAML string
        Window window;
        using (var stringReader = new StringReader(xamlCode))
        using (var xmlReader = System.Xml.XmlReader.Create(stringReader))
        {
            var obj = XamlReader.Load(xmlReader);
            if (obj is not Window w)
                throw new InvalidOperationException("XAML root element is not a Window.");
            window = w;
        }

        // Compile code-behind
        var assembly = CompileCodeBehindFromString(csCode);
        if (assembly == null)
            throw new InvalidOperationException("Code-behind compilation failed.");

        // Find a class with a constructor accepting Window (your logic class)
        var logicType = assembly.GetTypes()
            .FirstOrDefault(t => t.IsClass && t.GetConstructor(new[] { typeof(Window) }) != null);

        if (logicType == null)
            throw new InvalidOperationException("No logic class with constructor(Window) found in code-behind.");

        // Instantiate the logic class passing the Window instance
        var ctor = logicType.GetConstructor(new[] { typeof(Window) });
        ctor.Invoke(new object[] { window });

        return window;
    }

    private Assembly CompileCodeBehindFromString(string csCode)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(csCode);

        string assemblyName = Path.GetRandomFileName();

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        var compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            var failures = result.Diagnostics.Where(d =>
                d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error);

            string errors = string.Join(Environment.NewLine, failures.Select(f => $"{f.Id}: {f.GetMessage()}"));
            throw new Exception("Compilation failed:\n" + errors);
        }

        ms.Seek(0, SeekOrigin.Begin);
        return Assembly.Load(ms.ToArray());
    }
}
