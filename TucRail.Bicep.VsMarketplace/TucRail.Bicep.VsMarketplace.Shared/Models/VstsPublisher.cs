using Azure.Bicep.Types.Concrete;

namespace TucRail.Bicep.VsMarketplace.Shared.Models;

[BicepSerializableType]
public record VstsPublisher(
    [property: TypeAnnotation("The name of the publisher", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    string Name,
    [property: TypeAnnotation("The publisher identifier", ObjectTypePropertyFlags.ReadOnly)]
    string PublisherId,
    [property: TypeAnnotation("The publisher domain", ObjectTypePropertyFlags.None)]
    string? PublisherDomain,
    [property: TypeAnnotation("If the publisher domain has been verified", ObjectTypePropertyFlags.ReadOnly)]
    bool IsVerified
    );