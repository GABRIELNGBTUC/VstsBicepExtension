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
    [property: TypeAnnotation("Test property", ObjectTypePropertyFlags.None)]
    string SomeRandomProperty);

public record ConfigurationEntra(
    string SomeRandomProperty,
    [property: TypeAnnotation("Authentication mode to login to the marketplace", ObjectTypePropertyFlags.Required), BicepStringLiteralValue(nameof(AuthenticationMode.Entra))]
    AuthenticationMode AuthenticationMode) : ConfigurationBase(SomeRandomProperty);

public record ConfigurationPat(
    string SomeRandomProperty,
    [property: TypeAnnotation("PAT token to use", ObjectTypePropertyFlags.Required, true)]
    string Token,
    [property: TypeAnnotation("Authentication mode to login to the marketplace", ObjectTypePropertyFlags.Required), BicepStringLiteralValue(nameof(AuthenticationMode.PAT))]
    AuthenticationMode AuthenticationMode
) : ConfigurationBase(SomeRandomProperty);

public enum AuthenticationMode
{
    PAT, Entra
}