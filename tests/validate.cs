// Compiles every schema and validates every example against the schema
// named in its xsi:noNamespaceSchemaLocation, using System.Xml.Schema.
//
// Run from the repository root:
//   dotnet run tests/validate.cs

using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

var root = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
var failures = 0;
var schemas = new Dictionary<string, XmlSchemaSet?>(StringComparer.OrdinalIgnoreCase);

XmlSchemaSet? LoadSchema(string path)
{
    path = Path.GetFullPath(path);
    if (schemas.TryGetValue(path, out var cached))
    {
        return cached;
    }

    var set = new XmlSchemaSet();
    var ok = true;
    set.ValidationEventHandler += (_, e) =>
    {
        ok = false;
        Console.WriteLine($"  {Rel(path)}({e.Exception.LineNumber},{e.Exception.LinePosition}): {e.Severity}: {e.Message}");
    };

    try
    {
        set.Add(null, path);
        set.Compile();
    }
    catch (Exception ex) when (ex is XmlException or XmlSchemaException)
    {
        ok = false;
        Console.WriteLine($"  {Rel(path)}: {ex.Message}");
    }

    schemas[path] = ok ? set : null;
    return ok ? set : null;
}

string Rel(string path) => Path.GetRelativePath(root, path).Replace('\\', '/');

Console.WriteLine("Schemas:");
foreach (var xsd in Directory.EnumerateFiles(root, "*.xsd", SearchOption.AllDirectories).Order())
{
    if (LoadSchema(xsd) is null)
    {
        failures++;
        Console.WriteLine($"FAIL {Rel(xsd)}");
    }
    else
    {
        Console.WriteLine($"ok   {Rel(xsd)}");
    }
}

Console.WriteLine();
Console.WriteLine("Examples:");
foreach (var xml in Directory.EnumerateFiles(root, "*.xml", SearchOption.AllDirectories)
             .Where(f => f.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Contains("examples"))
             .Order())
{
    var errors = new List<string>();

    try
    {
        using var reader = XmlReader.Create(xml);
        var doc = XDocument.Load(reader, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

        if (doc.Root!.Name.Namespace != XNamespace.None)
        {
            errors.Add($"root element is in namespace '{doc.Root.Name.NamespaceName}'; the schemas have no target namespace");
        }

        var location = (string?)doc.Root.Attribute(xsi + "noNamespaceSchemaLocation");
        if (string.IsNullOrWhiteSpace(location))
        {
            errors.Add("missing xsi:noNamespaceSchemaLocation");
        }
        else
        {
            var xsdPath = Path.Combine(Path.GetDirectoryName(xml)!, location);
            if (!File.Exists(xsdPath))
            {
                errors.Add($"schema not found: {location}");
            }
            else if (LoadSchema(xsdPath) is not { } set)
            {
                errors.Add($"schema does not compile: {location}");
            }
            else
            {
                doc.Validate(set, (_, e) =>
                    errors.Add($"({e.Exception.LineNumber},{e.Exception.LinePosition}): {e.Severity}: {e.Message}"));
            }
        }
    }
    catch (XmlException ex)
    {
        errors.Add($"({ex.LineNumber},{ex.LinePosition}): {ex.Message}");
    }

    if (errors.Count == 0)
    {
        Console.WriteLine($"ok   {Rel(xml)}");
    }
    else
    {
        failures++;
        Console.WriteLine($"FAIL {Rel(xml)}");
        foreach (var error in errors)
        {
            Console.WriteLine($"  {error}");
        }
    }
}

Console.WriteLine();
Console.WriteLine(failures == 0 ? "All passed." : $"{failures} file(s) failed.");
return failures == 0 ? 0 : 1;
