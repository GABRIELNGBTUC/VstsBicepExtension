using Azure.Bicep.Types.Concrete;

namespace TucRail.Bicep.VsMarketplace.Generator;


public abstract record ExtensionConfigurationBaseProperty
{
    public ObjectTypePropertyFlags Flags;
    public string? Description;
    public string Name;

    public ExtensionConfigurationBaseProperty()
    {
    }

    public ExtensionConfigurationBaseProperty(string name, ObjectTypePropertyFlags flags, string? description)
    {
        Flags = flags;
        Description = description;
        Name = name;
    }

    public virtual ITypeReference GetBicepTypeReference(ref TypeFactory factory) => throw new NotImplementedException();
}

public record ExtensionConfigurationIntegerProperty(
    string Name,
    ObjectTypePropertyFlags Flags,
    string? Description = null,
    int? MinimumValue = null,
    int? MaximumValue = null) : ExtensionConfigurationBaseProperty(Name, Flags, Description)
{
    public override ITypeReference GetBicepTypeReference(ref TypeFactory factory) =>
        TypeGenerator.GetReferenceFromType(factory, new IntegerType(MinimumValue, MaximumValue));
}

public record ExtensionConfigurationStringProperty(
    string Name,
    ObjectTypePropertyFlags Flags,
    string? Description = null,
    bool IsSecure = false,
    int? MinimumLength = null,
    int? MaximumLength = null,
    string? pattern = null) : ExtensionConfigurationBaseProperty(Name, Flags, Description)
{
    public override ITypeReference GetBicepTypeReference(ref TypeFactory factory) =>
        TypeGenerator.GetReferenceFromType(factory, new StringType(IsSecure, MinimumLength, MaximumLength, pattern));
}

public record ExtensionConfigurationBooleanProperty(
    string Name,
    ObjectTypePropertyFlags Flags,
    string? Description = null) : ExtensionConfigurationBaseProperty(Name, Flags, Description)
{
    public override ITypeReference GetBicepTypeReference(ref TypeFactory factory) =>
        TypeGenerator.GetReferenceFromType(factory, new BooleanType());
}

public enum ArrayTypeKind
{
    String,
    Integer,
    Object
}

public record ExtensionConfigurationArrayProperty(
    string Name,
    ObjectTypePropertyFlags Flags,
    ArrayTypeKind ArrayKind,
    string? Description = null,
    int? MinimumItems = null,
    int? MaximumItems = null) : ExtensionConfigurationBaseProperty(Name, Flags, Description)
{
    public override ITypeReference GetBicepTypeReference(ref TypeFactory factory) =>
        TypeGenerator.GetReferenceFromType(factory,
            new ArrayType(GetArrayElementType(ref factory), MinimumItems, MaximumItems));

    private ITypeReference GetArrayElementType(ref TypeFactory factory)
    {
        switch (ArrayKind)
        {
            case ArrayTypeKind.Integer: return TypeGenerator.GetReferenceFromType(factory, new IntegerType());
            case ArrayTypeKind.String: return TypeGenerator.GetReferenceFromType(factory, new StringType());
            default: throw new NotImplementedException();
        }
    }
}

public record ExtensionConfigurationObjectProperty(
    string Name,
    ObjectTypePropertyFlags Flags,
    ICollection<ExtensionConfigurationBaseProperty> properties,
    string? Description = null) : ExtensionConfigurationBaseProperty(Name, Flags, Description)
{
    public override ITypeReference GetBicepTypeReference(ref TypeFactory factory)
    {
        var typeFactory = factory;
        return TypeGenerator.GetReferenceFromType(typeFactory,
            new ObjectType(Name,
                properties.ToDictionary(p => p.Name,
                    p => new ObjectTypeProperty(p.GetBicepTypeReference(ref typeFactory), p.Flags, p.Description)),
                null));
    }
}