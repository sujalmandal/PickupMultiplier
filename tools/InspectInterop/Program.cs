using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace InspectInterop;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("usage: InspectInterop <assembly> <filter> [filter...]");
            return 2;
        }

        using var stream = File.OpenRead(args[0]);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        var filters = args.Skip(1).ToArray();

        var matched = 0;
        foreach (var typeHandle in reader.TypeDefinitions)
        {
            matched += DumpType(reader, typeHandle, filters);
        }

        Console.WriteLine($"-- {matched} type(s) matched --");
        return matched > 0 ? 0 : 1;
    }

    private static int DumpType(MetadataReader reader, TypeDefinitionHandle typeHandle, string[] filters)
    {
        var type = reader.GetTypeDefinition(typeHandle);
        var typeName = ReadTypeName(reader, type);
        var members = ReadMembers(reader, type, Matches(filters, typeName), filters);
        if (members.Count == 0)
        {
            return 0;
        }

        Console.WriteLine();
        Console.WriteLine($"### {typeName}  [{DescribeType(type.Attributes)}]");
        foreach (var member in members)
        {
            Console.WriteLine($"    {member}");
        }

        return 1;
    }

    private static List<string> ReadMembers(MetadataReader reader, TypeDefinition type, bool typeMatches, string[] filters)
    {
        var provider = new TypeNameProvider();
        var lines = new List<string>();

        foreach (var fieldHandle in type.GetFields())
        {
            var field = reader.GetFieldDefinition(fieldHandle);
            var name = reader.GetString(field.Name);
            if (!typeMatches && !Matches(filters, name))
            {
                continue;
            }

            var fieldType = field.DecodeSignature(provider, null);
            lines.Add($"field {name} : {fieldType}{ReadConstant(reader, field)}");
        }

        foreach (var propertyHandle in type.GetProperties())
        {
            var property = reader.GetPropertyDefinition(propertyHandle);
            var name = reader.GetString(property.Name);
            if (!typeMatches && !Matches(filters, name))
            {
                continue;
            }

            var propertyType = property.DecodeSignature(provider, null).ReturnType;
            lines.Add($"prop  {name} : {propertyType}");
        }

        foreach (var methodHandle in type.GetMethods())
        {
            var method = reader.GetMethodDefinition(methodHandle);
            var name = reader.GetString(method.Name);
            if (!typeMatches && !Matches(filters, name))
            {
                continue;
            }

            var signature = method.DecodeSignature(provider, null);
            var parameters = string.Join(", ", ReadParameters(reader, method, signature));
            lines.Add($"meth  {name}({parameters}) : {signature.ReturnType}  [{DescribeMethod(method.Attributes)}]");
        }

        return lines;
    }

    private static string ReadConstant(MetadataReader reader, FieldDefinition field)
    {
        var handle = field.GetDefaultValue();
        if (handle.IsNil)
        {
            return string.Empty;
        }

        var constant = reader.GetConstant(handle);
        var blob = reader.GetBlobReader(constant.Value);
        return constant.TypeCode switch
        {
            ConstantTypeCode.Int32 => $" = {blob.ReadInt32()}",
            ConstantTypeCode.Int64 => $" = {blob.ReadInt64()}",
            ConstantTypeCode.Byte => $" = {blob.ReadByte()}",
            ConstantTypeCode.Boolean => $" = {blob.ReadBoolean()}",
            _ => string.Empty
        };
    }

    private static IEnumerable<string> ReadParameters(MetadataReader reader, MethodDefinition method, MethodSignature<string> signature)
    {
        var names = new List<string>();
        foreach (var parameterHandle in method.GetParameters())
        {
            var parameter = reader.GetParameter(parameterHandle);
            if (parameter.SequenceNumber == 0)
            {
                continue;
            }

            names.Add(reader.GetString(parameter.Name));
        }

        for (var i = 0; i < signature.ParameterTypes.Length; i++)
        {
            var type = signature.ParameterTypes[i];
            var name = i < names.Count && !string.IsNullOrEmpty(names[i]) ? names[i] : $"arg{i}";
            yield return $"{type} {name}";
        }
    }

    private static string ReadTypeName(MetadataReader reader, TypeDefinition type)
    {
        var name = reader.GetString(type.Name);
        var ns = reader.GetString(type.Namespace);
        if (!type.IsNested)
        {
            return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
        }

        var declaring = reader.GetTypeDefinition(type.GetDeclaringType());
        return $"{ReadTypeName(reader, declaring)}+{name}";
    }

    private static string DescribeType(TypeAttributes attributes)
    {
        var parts = new List<string>();
        if ((attributes & TypeAttributes.Interface) != 0)
        {
            parts.Add("interface");
        }

        if ((attributes & TypeAttributes.Abstract) != 0)
        {
            parts.Add("abstract");
        }

        parts.Add(attributes.ToString().Split(',').Last().Trim());
        return string.Join(" ", parts);
    }

    private static string DescribeMethod(MethodAttributes attributes)
    {
        var parts = new List<string>();
        if ((attributes & MethodAttributes.Static) != 0)
        {
            parts.Add("static");
        }

        if ((attributes & MethodAttributes.Virtual) != 0)
        {
            parts.Add("virtual");
        }

        if ((attributes & MethodAttributes.Abstract) != 0)
        {
            parts.Add("abstract");
        }

        switch (attributes & MethodAttributes.MemberAccessMask)
        {
            case MethodAttributes.Public:
                parts.Add("public");
                break;
            case MethodAttributes.Assembly:
                parts.Add("internal");
                break;
            case MethodAttributes.Private:
                parts.Add("private");
                break;
        }

        return string.Join(" ", parts);
    }

    private static bool Matches(string[] filters, string name)
    {
        return filters.Any(filter => name.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }
}
