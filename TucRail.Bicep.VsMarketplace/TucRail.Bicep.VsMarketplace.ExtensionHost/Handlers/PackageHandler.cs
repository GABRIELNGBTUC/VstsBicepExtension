using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bicep.Local.Extension.Protocol;
using Microsoft.VisualStudio.Services.Common;
using TucRail.Bicep.VsMarketplace.ExtensionHost.Helpers;
using TucRail.Bicep.VsMarketplace.Shared;
using TucRail.Bicep.VsMarketplace.Shared.Models;

namespace TucRail.Bicep.VsMarketplace.ExtensionHost.Handlers;

public class PackageHandler : IResourceHandler
{
    public string ResourceType => $"{nameof(VstsPublisher)}/{nameof(VstsExtension)}/{nameof(VstsPackage)}";

    private record Identifiers(
        string? Version,
        string? ExtensionName,
        string? PublisherName,
        string? DownloadPath = null);

    public Task<LocalExtensionOperationResponse> CreateOrUpdate(ResourceSpecification request,
        CancellationToken cancellationToken)
        => RequestHelper.HandleRequest(request.Config, client =>
        {
            return Task.FromResult(RequestHelper.CreateErrorResponse(ErrorCodes.OperationNotSupported,
                ErrorCodes.GetOperationNotSupportedMessage(
                    "Packages can only be retrieved. Use the 'existing' resource statement to download the package.")));
        });


    public Task<LocalExtensionOperationResponse> Preview(ResourceSpecification request,
        CancellationToken cancellationToken)
        => RequestHelper.HandleRequest(request.Config, async client =>
        {
            var properties = RequestHelper.GetProperties<VstsPackage>(request.Properties);

            await Task.Yield();

            // Remove any property that is not needed in the response

            return RequestHelper.CreateSuccessResponse(request, properties,
                new Identifiers(properties.Version, properties.ExtensionName, properties.PublisherName));
        });

    public Task<LocalExtensionOperationResponse> Get(ResourceReference request, CancellationToken cancellationToken)
        => RequestHelper.HandleRequest(request.Config, async client =>
        {
            Console.WriteLine(JsonSerializer.Serialize(request,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            var extensionVersion = RequestHelper.GetIdentifierData(request, nameof(VstsPackage.Version));
            var extensionName = RequestHelper.GetIdentifierData(request, nameof(VstsPackage.ExtensionName));
            var extensionPublisher = RequestHelper.GetIdentifierData(request, nameof(VstsPackage.PublisherName));
            var packageDownloadPath = RequestHelper.GetIdentifierData(request, nameof(VstsPackage.DestinationPath));
            if (extensionPublisher is null)
            {
                return RequestHelper.CreateErrorResponse(ErrorCodes.PropertyNotFound,
                    ErrorCodes.GetPropertyNotFoundMessage(RequestHelper.ToCamelCase(nameof(VstsPackage.PublisherName))));
            }

            if (extensionName is null)
            {
                return RequestHelper.CreateErrorResponse(ErrorCodes.PropertyNotFound,
                    ErrorCodes.GetPropertyNotFoundMessage(RequestHelper.ToCamelCase(nameof(VstsPackage.ExtensionName))));
            }

            if (extensionVersion is null)
            {
                return RequestHelper.CreateErrorResponse(ErrorCodes.PropertyNotFound,
                    ErrorCodes.GetPropertyNotFoundMessage(RequestHelper.ToCamelCase(nameof(VstsPackage.Version))));
            }

            if (packageDownloadPath is null)
            {
                return RequestHelper.CreateErrorResponse(ErrorCodes.PropertyNotFound,
                    ErrorCodes.GetPropertyNotFoundMessage(RequestHelper.ToCamelCase(nameof(VstsPackage.DestinationPath))));
            }

            if (!Directory.Exists(packageDownloadPath.ToString()))
            {
                return RequestHelper.CreateErrorResponse(ErrorCodes.DirectoryNotFound,
                    ErrorCodes.GetDirectoryNotFoundMessage(packageDownloadPath.ToString()));
            }

            try
            {
                var extension = await client.GetExtensionAsync(extensionPublisher.ToString(), extensionName.ToString(),
                    cancellationToken: cancellationToken);
                await using var fileStream = new FileStream(Path.Join(packageDownloadPath.ToString(), $"{extensionName.ToString()}-{extensionVersion.ToString()}.vsix"), FileMode.OpenOrCreate);
                await using var downloadStream = await client.GetPackageAsync(extensionPublisher.ToString(), extensionName.ToString(),
                    extensionVersion.ToString(), cancellationToken: cancellationToken);
                await Task.Yield();
                
                await downloadStream.CopyToAsync(fileStream, cancellationToken);

                return RequestHelper.CreateSuccessResponse(request, new VstsPackage(extensionPublisher.ToString(),
                        extensionName.ToString(),
                        extensionVersion.ToString(),
                        packageDownloadPath.ToString()),
                    new Identifiers(extensionVersion.ToString(), extensionName.ToString(),
                        extensionPublisher.ToString(), packageDownloadPath.ToString()));
            }
            catch (VssException e)
            {
                return RequestHelper.CreateErrorResponse(ErrorCodes.VssException, ErrorCodes.GetVssExceptionMessage(e.Message));
            }
        });

    public Task<LocalExtensionOperationResponse> Delete(ResourceReference request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
    
    private static (string? CurrentVersion, string? UpdatedVersion) ExtractVersionsFromErrorMessage(string message)
    {
        // Regex pour rechercher les versions actuelles et mises à jour
        var versionPattern = @"Current version: (?<current>\d+\.\d+\.\d+)\s+Updated version: (?<updated>\d+\.\d+\.\d+)";
        var match = Regex.Match(message, versionPattern);

        if (match.Success)
        {
            // Extraction des versions à l'aide des groupes nommés
            string currentVersion = match.Groups["current"].Value;
            string updatedVersion = match.Groups["updated"].Value;

            return (CurrentVersion: currentVersion, UpdatedVersion: updatedVersion);
        }

        return (CurrentVersion: null, UpdatedVersion: null);
    }

}