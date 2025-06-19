using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bicep.Local.Extension.Protocol;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Gallery.WebApi;
using TucRail.Bicep.VsMarketplace.ExtensionHost.Helpers;
using TucRail.Bicep.VsMarketplace.Shared;
using TucRail.Bicep.VsMarketplace.Shared.Models;

namespace TucRail.Bicep.VsMarketplace.ExtensionHost.Handlers;

public class ExtensionHandler : IResourceHandler
{
    public string ResourceType => $"{nameof(VstsPublisher)}/{nameof(VstsExtension)}";

    private const string ExtensionAlreadyExistsMessage =
        "The extension already exists";

    private record Identifiers(
        string? Name,
        string? PublisherName);

    public Task<LocalExtensionOperationResponse> CreateOrUpdate(ResourceSpecification request,
        CancellationToken cancellationToken)
        => RequestHelper.HandleRequest(request.Config, async client =>
        {
            Console.WriteLine($"Starting create or update operation for {ResourceType}");
            Console.WriteLine(JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            var properties = RequestHelper.GetProperties<VstsExtension>(request.Properties);
            Console.WriteLine(properties);
            if (!File.Exists(properties.PackagePath))
            {
                Console.WriteLine("File not found: {0}", properties.PackagePath);
                return RequestHelper.CreateErrorResponse(ErrorCodes.FileNotFound,
                    ErrorCodes.GetFileNotFoundMessage(properties.PackagePath));
            }

            if (properties.Type is null)
            {
                Console.WriteLine("Type not found");
                return RequestHelper.CreateErrorResponse(ErrorCodes.PropertyNotFound,
                    ErrorCodes.GetPropertyNotFoundMessage(RequestHelper.ToCamelCase(nameof(VstsExtension.Type))));
            }

            if (properties.PackagePath is null)
            {
                Console.WriteLine("Package path not found");
                return RequestHelper.CreateErrorResponse(ErrorCodes.PropertyNotFound,
                    ErrorCodes.GetPropertyNotFoundMessage(
                        RequestHelper.ToCamelCase(nameof(VstsExtension.PackagePath))));
            }

            try
            {
                //Update resource
                Console.WriteLine("Creating filestream");
                await using var fileStream = new FileStream(properties.PackagePath, FileMode.Open);

                    Console.WriteLine("Updating extension");
                    await client.UpdateExtensionAsync(fileStream, properties.PublisherName, properties.Name,
                        cancellationToken: cancellationToken);

                var extension = await client.GetExtensionAsync(properties.PublisherName, properties.Name,
                    cancellationToken: cancellationToken);

                properties = properties with { Versions = extension is null ? [] : extension.Versions.Select(v => v.Version).ToArray() };
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected error during update: {0}", e.Message);
                Console.WriteLine("Creating filestream");
                await using var fileStream = new FileStream(properties.PackagePath!, FileMode.Open);
                //Create
                try
                {
                    Console.WriteLine("Creating extension");
                    await client.CreateExtensionAsync(fileStream, properties.GetExtensionType(),
                        cancellationToken: cancellationToken);

                    var extension = await client.GetExtensionAsync(properties.PublisherName, properties.Name,
                        cancellationToken: cancellationToken);

                    properties = properties with { Versions = extension is null ? [] : extension.Versions.Select(v => v.Version).ToArray() };
                }
                catch (VssException vssException)
                {
                    var (currentVersion, updatedVersion) = ExtractVersionsFromErrorMessage(vssException.Message);
                    if (properties.IgnoreVersionErrors &&
                         ((currentVersion is not null && updatedVersion is not null) ||
                          vssException.Message.Contains(ExtensionAlreadyExistsMessage,
                              StringComparison.OrdinalIgnoreCase)))
                    {
                        properties = properties with { ErrorMessage = vssException.Message };
                    }
                    else
                    {
                        return RequestHelper.CreateErrorResponse(ErrorCodes.VssException,
                            ErrorCodes.GetVssExceptionMessage(vssException.Message));
                    }
                }
            }

            return RequestHelper.CreateSuccessResponse(request, properties,
                new Identifiers(properties.Name, properties.PublisherName));
        });


    public Task<LocalExtensionOperationResponse> Preview(ResourceSpecification request,
        CancellationToken cancellationToken)
        => RequestHelper.HandleRequest(request.Config, async client =>
        {
            Console.WriteLine($"Starting preview operation for {ResourceType}");
            var properties = RequestHelper.GetProperties<VstsExtension>(request.Properties);

            await Task.Yield();

            // Remove any property that is not needed in the response

            return RequestHelper.CreateSuccessResponse(request, properties,
                new Identifiers(properties.Name, properties.PublisherName));
        });

    public Task<LocalExtensionOperationResponse> Get(ResourceReference request, CancellationToken cancellationToken)
        => RequestHelper.HandleRequest(request.Config, async client =>
        {
            Console.WriteLine($"Starting get operation for {ResourceType}");
            Console.WriteLine(JsonSerializer.Serialize(request,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            Console.WriteLine("Reading identifiers from request");
            var extensionName = RequestHelper.GetIdentifierData(request, nameof(VstsExtension.Name));
            var extensionPublisher = RequestHelper.GetIdentifierData(request, nameof(VstsExtension.PublisherName));
            if (extensionPublisher is null)
            {
                return RequestHelper.CreateErrorResponse(ErrorCodes.PropertyNotFound,
                    ErrorCodes.GetPropertyNotFoundMessage(
                        RequestHelper.ToCamelCase(nameof(VstsExtension.PublisherName))));
            }

            if (extensionName is null)
            {
                return RequestHelper.CreateErrorResponse(ErrorCodes.PropertyNotFound,
                    ErrorCodes.GetPropertyNotFoundMessage(RequestHelper.ToCamelCase(nameof(VstsExtension.Name))));
            }
            
            Console.WriteLine("Getting extension");
            Console.WriteLine($"Extension name: {extensionName}");
            Console.WriteLine($"Extension publisher: {extensionPublisher}");

            var extension = await client.GetExtensionAsync(extensionPublisher.ToString(), extensionName.ToString(),
                cancellationToken: cancellationToken);

            await Task.Yield();

            // Remove any property that is not needed in the response
            
            Console.WriteLine("Extension found");
            Console.WriteLine(JsonSerializer.Serialize(extension));

            return RequestHelper.CreateSuccessResponse(request, new VstsExtension(extension.ExtensionName,
                    extension.Publisher.PublisherName,
                    extension.LastUpdated.ToString(CultureInfo.InvariantCulture),
                    extension.Versions is null ? [] : extension.Versions.Select(v => v.Version).ToArray()),
                new Identifiers(extension.ExtensionName, extension.Publisher.PublisherName));
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