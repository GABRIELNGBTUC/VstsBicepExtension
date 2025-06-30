using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Azure.Bicep.Types;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;
using Azure.Bicep.Types.Serialization;
using TucRail.Bicep.VsMarketplace.Generator;
using TucRail.Bicep.VsMarketplace.Shared;
using TucRail.Bicep.VsMarketplace.Shared.Models;

namespace TucRail.Bicep.VsMarketplace.Generator;


public static class TypeFactoryExtensions {
    public static ITypeReference GetReferenceFromType(this TypeFactory factory, TypeBase type)
    {
        try
        {
            var typeReference = factory.Create(() => type);
            return factory.GetReference(typeReference);
        }
        catch (ArgumentException ex)
        {
        }

        return factory.GetReference(type);
    }
}

public static class TypeGenerator
{
    internal static string CamelCase(string input)
        => $"{input[..1].ToLowerInvariant()}{input[1..]}";

    internal static TypeBase GenerateForRecord(TypeFactory factory, ConcurrentDictionary<Type, TypeBase> typeCache,
        Type type, bool ignoreDiscriminatorAttribute = false)
    {
        var typeProperties = new Dictionary<string, ObjectTypeProperty>();
        if (ignoreDiscriminatorAttribute == false && type.GetCustomAttribute<BicepDiscriminatorType>() is { } discriminatorTypeAttribute)
        {
            TypeBase typeReference;
            var baseProperties = (ObjectType)GenerateForRecord(factory, typeCache, type, true);
            var childTypesDictionary = new Dictionary<string, ITypeReference>();
            foreach (var kvp in discriminatorTypeAttribute.DiscriminatorTypes)
            {
                var discriminatedTypeProperties = (ObjectType)GenerateForRecord(factory, typeCache, kvp.Value, true);
                childTypesDictionary.Add(kvp.Key, factory.GetReferenceFromType(discriminatedTypeProperties));
            }
            typeReference = factory.Create(() => new DiscriminatedObjectType(discriminatorTypeAttribute.DiscrminatorTypeName,
                discriminatorTypeAttribute.DiscriminatorPropertyName, baseProperties.Properties
               , childTypesDictionary));
            
            return typeReference;
        }
        else
        {
            foreach (var property in type
                         .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .Where(p => !type.BaseType.GetProperties().Select(prop => prop.Name).Contains(p.Name)))
            {
                var annotation = property.GetCustomAttributes<TypeAnnotationAttribute>(false).FirstOrDefault();
                var propertyType = property.PropertyType;
                TypeBase typeReference;
                var originalPropertyType = propertyType;

                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    propertyType = propertyType.GetGenericArguments()[0];
                }
                
                if (property.GetCustomAttribute<BicepStringLiteralValue>(true) is { } discriminatorTypeValue)
                {
                    typeReference = factory.Create(() => new StringLiteralType(discriminatorTypeValue.Value));
                }
                else if (propertyType == typeof(string) && annotation?.IsSecure == true)
                {
                    typeReference = factory.Create(() => new StringType(sensitive: true));
                }
                else if (propertyType == typeof(string))
                {
                    typeReference = typeCache.GetOrAdd(propertyType, _ => factory.Create(() => new StringType()));
                }
                else if (propertyType == typeof(bool))
                {
                    typeReference = typeCache.GetOrAdd(propertyType, _ => factory.Create(() => new BooleanType()));
                }
                else if (propertyType == typeof(int))
                {
                    typeReference = typeCache.GetOrAdd(propertyType, _ => factory.Create(() => new IntegerType()));
                }
                else if (propertyType.IsClass)
                {
                    typeReference = typeCache.GetOrAdd(propertyType,
                        _ => factory.Create(() => GenerateForRecord(factory, typeCache, propertyType)));
                }
                else if (originalPropertyType.IsGenericType &&
                         originalPropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                         originalPropertyType.GetGenericArguments()[0] is { IsEnum: true } enumType)
                {
                    var enumMembers = enumType.GetEnumNames()
                        .Select(x => factory.Create(() => new StringLiteralType(x)))
                        .Select(x => factory.GetReference(x))
                        .ToImmutableArray();

                    typeReference = typeCache.GetOrAdd(propertyType,
                        _ => factory.Create(() => new UnionType(enumMembers)));
                }
                else if (originalPropertyType is { IsEnum: true } originalEnumType)
                {
                    var enumMembers = originalEnumType.GetEnumNames()
                        .Select(x => factory.Create(() => new StringLiteralType(x)))
                        .Select(x => factory.GetReference(x))
                        .ToImmutableArray();

                    typeReference = typeCache.GetOrAdd(propertyType,
                        _ => factory.Create(() => new UnionType(enumMembers)));
                }
                else
                {
                    throw new NotImplementedException($"Unsupported property type {propertyType}");
                }

                if (IsNullableReferenceType(property)
                    || (originalPropertyType.IsGenericType &&
                        originalPropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                {
                    var unionType = factory.GetReferenceFromType(new UnionType([
                        factory.GetReference(typeReference),
                        factory.GetReferenceFromType(new NullType())
                    ]));
                    typeReference = unionType.Type;
                }

                typeProperties[CamelCase(property.Name)] = new ObjectTypeProperty(
                    factory.GetReference(typeReference),
                    annotation?.Flags ?? ObjectTypePropertyFlags.None,
                    annotation?.Description);
            }
        }


        return new ObjectType(
            type.Name,
            typeProperties,
            null);
    }

    internal static ResourceType GenerateResource(TypeFactory factory, ConcurrentDictionary<Type, TypeBase> typeCache,
        Type type)
    {
        var realName = type.Name;
        Type? parentType = type;
        do
        {
            parentType = parentType.GetCustomAttribute<BicepParentTypeAttribute>(true)?.ParentType;
            if (parentType is not null)
            {
                realName = $"{parentType!.Name}/{realName}";
            }
        } while (type.GetCustomAttribute<BicepParentTypeAttribute>(true)?.ParentType is not null &&
                 parentType != null);

        return factory.Create(() => new ResourceType(
            realName,
            ScopeType.Unknown,
            null,
                  
            factory.GetReferenceFromType(GenerateForRecord(factory, typeCache, type)),
            ResourceFlags.None,
            null));
    }

    internal static string GetString(Action<Stream> streamWriteFunc)
    {
        using var memoryStream = new MemoryStream();
        streamWriteFunc(memoryStream);

        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    /// <summary>
    /// Generates bicep types and index for the provided assembly
    /// </summary>
    /// <param name="extensionName">Name of the extension</param>
    /// <param name="version">Version of the extension in SemVersion format</param>
    /// <param name="configurationType">Type used to generate the configuration. Pass <see langword="null"/> if no configuration is required.</param>
    /// <param name="sourceAssemblyType">A type present in the assembly where your serializable types are contained.</param>
    /// <returns></returns>
    [RequiresUnreferencedCode("Retrieves the valid bicep types from the current assembly")]
    public static Dictionary<string, string> GenerateTypes(string extensionName, string version,
        Type? configurationType, Type? sourceAssemblyType = null)
    {
        var factory = new TypeFactory([]);

        var typeCache = new ConcurrentDictionary<Type, TypeBase>();

        

        TypeBase? configurationTypeReference;

        if (configurationType is null)
        {
            configurationTypeReference = null;
        }

        else
        {
            var configurationTypeBase = GenerateForRecord(factory, typeCache, configurationType);

            if (configurationTypeBase is ObjectType configurationTypeBaseObject)
            {
                var configuration =
                    factory.Create(() => new ObjectType("configuration", configurationTypeBaseObject.Properties, null));
                configurationTypeReference = configuration;
            }
            else
            {
                configurationTypeReference = configurationTypeBase;
            }
        }

        var settings = new TypeSettings(
            name: extensionName,
            version: version,
            isSingleton: true,
            configurationType: new CrossFileTypeReference("types.json", factory.GetIndex(configurationTypeReference ?? 
                factory.GetReferenceFromType(new ObjectType("configuration", new Dictionary<string, ObjectTypeProperty>(), null)).Type 
                )));

        var serializableTypes =
            sourceAssemblyType is null
                ? Assembly.GetExecutingAssembly().GetTypes()
                    .Where(t => t.GetCustomAttribute<BicepSerializableType>() != null)
                : Assembly.GetAssembly(sourceAssemblyType).GetTypes()
                    .Where(t => t.GetCustomAttribute<BicepSerializableType>() != null);
        var resourceTypes = serializableTypes.Select(type => GenerateResource(factory, typeCache, type));

        var index = new TypeIndex(
            resourceTypes.ToDictionary(x => x.Name, x => new CrossFileTypeReference("types.json", factory.GetIndex(x))),
            new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<CrossFileTypeReference>>>(),
            settings,
            null);

        return new Dictionary<string, string>
        {
            ["index.json"] = GetString(stream => TypeSerializer.SerializeIndex(stream, index)),
            ["types.json"] = GetString(stream => TypeSerializer.Serialize(stream, factory.GetTypes())),
        };
    }

    private static bool IsNullableReferenceType(PropertyInfo property)
    {
        // Vérification de l'attribut NullableAttribute pour la propriété
        var nullableAttribute = property.CustomAttributes
            .FirstOrDefault(a => a.AttributeType.Name == "NullableAttribute" || a.AttributeType.Name == "Nullable");

        if (nullableAttribute != null)
        {
            // Les arguments de NullableAttribute indiquent :
            // 1 --> Nullable (ex.: string?)
            // 2 --> Non-nullable (ex.: string)
            // var flag = (byte)nullableAttribute.ConstructorArguments[0].Value;
            // return flag == 1;
            return true;
        }

        return false; // Non-nullable ou nullabilité inconnue
    }
}