using Azure.Bicep.Types.Concrete;

namespace TucRail.Bicep.VsMarketplace.Shared.Models;

[BicepSerializableType]
[BicepParentType(typeof(VstsExtension))]
public record VstsPackage(
    [property: TypeAnnotation("The name of the publisher",
        ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    string PublisherName,
    [property: TypeAnnotation("The name of the extension",
        ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    string ExtensionName,
    [property: TypeAnnotation("The version of the extension",
        ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    string Version,
    [property: TypeAnnotation("Destination directory of the extension after the download",
        ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    string DestinationPath
    );