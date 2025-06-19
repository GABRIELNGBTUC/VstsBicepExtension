using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;

namespace TucRail.Bicep.VsMarketplace.Shared.Models;

[BicepSerializableType]
[BicepParentType(typeof(VstsPublisher))]
public record VstsExtension(
    [property:
        TypeAnnotation("The name of the extension",
            ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    string Name,
    [property:
        TypeAnnotation("The extension publisher",
            ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    string PublisherName,
    [property: TypeAnnotation("The extension last update date", ObjectTypePropertyFlags.ReadOnly)]
    string LastUpdatedDate,
    [property: TypeAnnotation("The extension versions", ObjectTypePropertyFlags.ReadOnly)]
    string[]? Versions,
    [property: TypeAnnotation("The type of the extension", ObjectTypePropertyFlags.WriteOnly),
               JsonConverter(typeof(NullableBicepEnumConverter<ExtensionType>))
    ]
    ExtensionType? Type = null,
    [property:
        TypeAnnotation("Absolute path to the extension .vsix file. Only used when creating or updating an extension",
            ObjectTypePropertyFlags.WriteOnly)]
    string? PackagePath = null,
    [property: TypeAnnotation("Ignore errors if the current version is lower or equal than the currently published one. This does not update the extension if an error is raised and this property is set to true.", ObjectTypePropertyFlags.WriteOnly)]
    bool IgnoreVersionErrors = false,
    [property: TypeAnnotation("The triggered error when update or creating an extension", ObjectTypePropertyFlags.ReadOnly)]
    string? ErrorMessage = null
)
{
    public string? GetExtensionType() => EnumHelper.GetDisplayName(Type);
}

public enum ExtensionType
{
    [Display(Name = "Azure%20DevOps")]
    AzureDevOps,
    [Display(Name = "Visual%20Studio")]
    VisualStudio,
    [Display(Name = "Visual%20Studio%20Code")]
    VisualStudioCode
}