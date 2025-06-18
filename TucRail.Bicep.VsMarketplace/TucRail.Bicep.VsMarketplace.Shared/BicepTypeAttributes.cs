using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;

namespace TucRail.Bicep.VsMarketplace.Shared;

/// <summary>
/// When used on a property. Means that the property will be exposed as a property of the bicep resource.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class TypeAnnotationAttribute : Attribute
{
    /// <summary>
    /// Defines a property that will be added to the bicep type.
    /// </summary>
    /// <param name="description"></param>
    /// <param name="flags"></param>
    /// <param name="isSecure"></param>
    public TypeAnnotationAttribute(
        string? description,
        ObjectTypePropertyFlags flags = ObjectTypePropertyFlags.None,
        bool isSecure = false)
    {
        Description = description;
        Flags = flags;
        IsSecure = isSecure;
    }

    /// <summary>
    /// The description of the property
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Flags to be assigned to the property
    /// </summary>
    public ObjectTypePropertyFlags Flags { get; }

    /// <summary>
    /// If the property should be considered as a secret
    /// </summary>
    public bool IsSecure { get; }
}

/// <summary>
/// When used on a class. Means that it will be generated as a child resource of the parent type provided.
/// Example: If the class is "Issue" and the parent is "Repository". The type will be generated with the name "Repository/Issue"
/// </summary>
/// <example>
///<code>
/// 
///    [BicepSerializableType]
/// /* Serialized type will be Repository */
///   public record Repository(
///    [property: TypeAnnotation("The name of the repository", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
///    string Name);
///[BicepSerializableType]
///[BicepParentType(typeof(Repository))]
/// /* Serialized type will be Repository/Label */
/// public record Label([Property: TypeAnnotation("The name of the label", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)] string Name);
/// [BicepSerializableType]
/// [BicepParentType(typeof(Repository))]
/// /* Serialized type will be Repository/Issue */
/// public record Issue(
///    [property: TypeAnnotation("The title of the issue", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
///    string Title,
///    [property: TypeAnnotation("The body of the issue", ObjectTypePropertyFlags.Required)]
///    string Body
/// );
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class)]
public class BicepParentTypeAttribute : Attribute
{
    public BicepParentTypeAttribute(
        Type parentType)
    {
        ParentType = parentType;
    }

    /// <summary>
    /// The bicep serializable type that is the parent of this type
    /// </summary>
    public Type ParentType { get; }
}

/// <summary>
/// When used on a class. Means that it will be generated as a bicep resource with the same name as the class
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class BicepSerializableType : Attribute
{
    public BicepSerializableType()
    {
    }
}

/// <summary>
/// Marks the type to be added to the "types.json" when types are generated for the assembly
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class BicepSerializableProperty : Attribute
{
    public BicepSerializableProperty()
    {
    }
}

/// <summary>
/// 
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class BicepDiscriminatorType : Attribute
{
    /// <summary>
    /// The name of the bicep type that will be generated. Recommended to use nameof(T).
    /// </summary>
    public string DiscrminatorTypeName { get; }
    /// <summary>
    /// The list of types that will be part of the union. All of these types should include a property with the name specified by DiscriminatorPropertyName and implementing the attribute TypeAnnotationAttribute
    /// </summary>
    public Dictionary<string, Type> DiscriminatorTypes { get; }
    /// <summary>
    /// The name of the property shared by the discriminated types. Should use pascalCase.
    /// </summary>
    public string DiscriminatorPropertyName { get; }

    public BicepDiscriminatorType(string discrminatorTypeName, string discriminatorPropertyName, params Type[] discriminatorTypes)
    {
        DiscrminatorTypeName = discrminatorTypeName;
        DiscriminatorTypes = discriminatorTypes.ToDictionary(t => t.Name, t => t);
        DiscriminatorPropertyName = discriminatorPropertyName;
    }
}

/// <summary>
/// Tells to the type generator that the following property is a string literal with a hardcoded value. To be used on the property that
/// allows to differentiate discriminated types
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class BicepStringLiteralValue : Attribute
{
    public string Value { get; }
    
    public BicepStringLiteralValue(string value)
    {
        Value = value;
    }
}


/// <summary>
/// JSON converter to be used with nullable enum (Enum?) inside Bicep serializable types
/// </summary>
/// <typeparam name="T">The type of the enum to deserialize</typeparam>
public class NullableBicepEnumConverter<T> : JsonConverter<T?> where T : struct, Enum
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null; // Renvoie null si la valeur est null dans le JSON
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Unexpected token parsing enum. Expected String, got {reader.TokenType}.");
        }

        var enumText = reader.GetString();

        if (Enum.TryParse(enumText, ignoreCase: true, out T value))
        {
            return value; // Retourne l'énumération correspondante
        }

        throw new JsonException($"Unable to convert \"{enumText}\" to Enum \"{typeof(T)}\".");
    }

    public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue(); // Sérialise les valeurs nulles en JSON comme null
        }
        else
        {
            writer.WriteStringValue(value.Value.ToString()); // Sérialise le nom littéral de l'énum
        }
    }
}


/// <summary>
/// JSON converter to be used with enum inside Bicep serializable types
/// </summary>
/// <typeparam name="T">The type of the enum to deserialize</typeparam>
public class BicepEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Unexpected token parsing enum. Expected String, got {reader.TokenType}.");
        }

        var enumText = reader.GetString();

        if (Enum.TryParse(enumText, ignoreCase: true, out T value))
        {
            return value; // Retourne l'énumération correspondante
        }

        throw new JsonException($"Unable to convert \"{enumText}\" to Enum \"{typeof(T)}\".");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString()); // Sérialise le nom littéral de l'énum
    }
}
