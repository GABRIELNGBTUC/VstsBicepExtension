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
    public string ResourceType => "VstsPublisher/VstsExtension";

    private record Identifiers(
        string? Name,
        string? PublisherName);

    public Task<LocalExtensionOperationResponse> CreateOrUpdate(ResourceSpecification request,
        CancellationToken cancellationToken)
        => RequestHelper.HandleRequest(request.Config, async client =>
        {
            Console.WriteLine(JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            var properties = RequestHelper.GetProperties<VstsExtension>(request.Properties);
            Console.WriteLine(properties);
            if (!File.Exists(properties.PackagePath))
            {
                return RequestHelper.CreateErrorResponse(ErrorCodes.FileNotFound, 
                    ErrorCodes.GetFileNotFoundMessage(properties.PackagePath));
            }

            if (properties.Type is null)
            {
                return RequestHelper.CreateErrorResponse(ErrorCodes.PropertyNotFound, 
                    ErrorCodes.GetPropertyNotFoundMessage(RequestHelper.ToCamelCase(nameof(VstsExtension.Type))));
            }

            if (properties.PackagePath is null)
            {
                return RequestHelper.CreateErrorResponse(ErrorCodes.PropertyNotFound, 
                    ErrorCodes.GetPropertyNotFoundMessage(RequestHelper.ToCamelCase(nameof(VstsExtension.PackagePath))));
            }

            try
            {
                var extension = await client.GetExtensionAsync(properties.PublisherName, properties.Name,
                    cancellationToken: cancellationToken);
                //Update resource
                await using var fileStream = new FileStream(properties.PackagePath, FileMode.Open);
                try
                {
                    await client.UpdateExtensionAsync(fileStream, properties.PublisherName, properties.Name,
                        cancellationToken: cancellationToken);
                }
                catch (VssException e)
                {
                    var (currentVersion, updatedVersion) = ExtractVersionsFromErrorMessage(e.Message);
                    if (properties.IgnoreVersionErrors  && currentVersion is not null && updatedVersion is not null)
                    {
                        properties = properties with {ErrorMessage = e.Message};
                    }
                    else
                    {
                        return RequestHelper.CreateErrorResponse(ErrorCodes.VssException, ErrorCodes.GetVssExceptionMessage(e.Message));
                    }
                    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected error during update: {0}", e.Message);
                await using var fileStream = new FileStream(properties.PackagePath!, FileMode.Open);
                //Create
                try
                {
                    await client.CreateExtensionAsync(fileStream, properties.GetExtensionType(),
                        cancellationToken: cancellationToken);
                }
                catch (VssException vssException)
                {
                    var (currentVersion, updatedVersion) = ExtractVersionsFromErrorMessage(vssException.Message);
                    if (properties.IgnoreVersionErrors  && currentVersion is not null && updatedVersion is not null)
                    {
                        properties = properties with {ErrorMessage = e.Message};
                    }
                    else
                    {
                        return RequestHelper.CreateErrorResponse(ErrorCodes.VssException, ErrorCodes.GetVssExceptionMessage(vssException.Message));
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
            var properties = RequestHelper.GetProperties<VstsExtension>(request.Properties);

            await Task.Yield();

            // Remove any property that is not needed in the response

            return RequestHelper.CreateSuccessResponse(request, properties,
                new Identifiers(properties.Name, properties.PublisherName));
        });

    public Task<LocalExtensionOperationResponse> Get(ResourceReference request, CancellationToken cancellationToken)
        => RequestHelper.HandleRequest(request.Config, async client =>
        {
            Console.WriteLine(JsonSerializer.Serialize(request,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            var extensionName = RequestHelper.GetIdentifierData(request, nameof(VstsExtension.Name));
            var extensionPublisher = RequestHelper.GetIdentifierData(request, nameof(VstsExtension.PublisherName));
            if (extensionPublisher is null)
            {
                return RequestHelper.CreateErrorResponse(ErrorCodes.PropertyNotFound,
                    ErrorCodes.GetPropertyNotFoundMessage(RequestHelper.ToCamelCase(nameof(VstsExtension.PublisherName))));
            }

            if (extensionName is null)
            {
                return RequestHelper.CreateErrorResponse(ErrorCodes.PropertyNotFound,
                    ErrorCodes.GetPropertyNotFoundMessage(RequestHelper.ToCamelCase(nameof(VstsExtension.Name))));
            }
            var extension = await client.GetExtensionAsync(extensionPublisher.ToString(), extensionName.ToString(),
                cancellationToken: cancellationToken);

            await Task.Yield();

            // Remove any property that is not needed in the response

            return RequestHelper.CreateSuccessResponse(request, new VstsExtension(extension.ExtensionName,
                    extension.Publisher.PublisherName,
                    extension.LastUpdated.ToString(CultureInfo.InvariantCulture)),
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