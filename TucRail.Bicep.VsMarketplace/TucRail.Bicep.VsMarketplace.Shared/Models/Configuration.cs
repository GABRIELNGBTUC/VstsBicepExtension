using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;

namespace TucRail.Bicep.VsMarketplace.Shared.Models;

public record Configuration(
    [property:
        TypeAnnotation(
            "Authentication mode used. Either PAT or Entra. If entra is used, the credential chain used can be found here https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/credential-chains?tabs=dac#defaultazurecredential-overview",
            ObjectTypePropertyFlags.Required), 
        JsonConverter(typeof(BicepEnumConverter<AuthenticationMode>))]
    AuthenticationMode AuthenticationMode,
    [property: TypeAnnotation("PAT", ObjectTypePropertyFlags.None, true)]
    string? PatToken
);

[BicepDiscriminatorType(nameof(ConfigurationBase), "authenticationMode", typeof(ConfigurationEntra), typeof(ConfigurationPat))]
public record ConfigurationBase(
    [property: TypeAnnotation("", ObjectTypePropertyFlags.None)]
    string SomeRandomProperty);

public record ConfigurationEntra(
    [property: TypeAnnotation("", ObjectTypePropertyFlags.Required), BicepStringLiteralValue("Entra")]
    string AuthenticationMode = "Entra");

public record ConfigurationPat(
    [property: TypeAnnotation("", ObjectTypePropertyFlags.Required, true)]
    string Token,
    [property: TypeAnnotation("", ObjectTypePropertyFlags.Required), BicepStringLiteralValue("PAT")]
    string AuthenticationMode
);

public enum AuthenticationMode
{
    PAT, Entra
}