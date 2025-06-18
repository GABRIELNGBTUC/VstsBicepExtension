using System.Text.Json;
using Bicep.Local.Extension.Protocol;
using Microsoft.VisualStudio.Services.Gallery.WebApi;
using TucRail.Bicep.VsMarketplace.ExtensionHost.Helpers;
using TucRail.Bicep.VsMarketplace.Shared.Models;

namespace TucRail.Bicep.VsMarketplace.ExtensionHost.Handlers;

public class PublisherHandler : IResourceHandler
{
    public string ResourceType => "VstsPublisher";

    private record Identifiers(
        string? Name);

    public Task<LocalExtensionOperationResponse> CreateOrUpdate(ResourceSpecification request, CancellationToken cancellationToken)
        => RequestHelper.HandleRequest(request.Config, async client =>
        {
            return RequestHelper.CreateErrorResponse("operation-not-supported", 
                "Updating the publisher outside of the portal is not supported. Please apply your updates through the UI.");
            Console.WriteLine(JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            var properties = RequestHelper.GetProperties<VstsPublisher>(request.Properties);

            try
            {
                var publisher = await client.GetPublisherAsync(properties.Name, cancellationToken: cancellationToken);
                var oldName = publisher.DisplayName;
                publisher.DisplayName = properties.Name;
                publisher.Domain = properties.PublisherDomain;
                //Update resource
                Console.WriteLine("Updating the publisher");
                await client.UpdatePublisherAsync(publisher, oldName, cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to update the publisher");
                //Create
                var publisher = new Publisher();
                publisher.DisplayName = properties.Name;
                publisher.Domain = properties.PublisherDomain;
                await client.CreatePublisherAsync(publisher, cancellationToken: cancellationToken);
            }
            
            return RequestHelper.CreateSuccessResponse(request, properties, new Identifiers(properties.Name));
        });
    

    public Task<LocalExtensionOperationResponse> Preview(ResourceSpecification request, CancellationToken cancellationToken)
    => RequestHelper.HandleRequest(request.Config, async client =>
    {
        var properties = RequestHelper.GetProperties<VstsPublisher>(request.Properties);

        await Task.Yield();

        // Remove any property that is not needed in the response

        return RequestHelper.CreateSuccessResponse(request, properties,
            new Identifiers(properties.Name));
    });

    public Task<LocalExtensionOperationResponse> Get(ResourceReference request, CancellationToken cancellationToken)
        => RequestHelper.HandleRequest(request.Config, async client =>
        {
            Console.WriteLine(JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            var publisherName = request.Identifiers[nameof(VstsPublisher.Name).ToLower()]!.ToString();
            var publisher = await client.GetPublisherAsync(publisherName, cancellationToken: cancellationToken);

            await Task.Yield();

            // Remove any property that is not needed in the response

            return RequestHelper.CreateSuccessResponse(request, new VstsPublisher(publisher.DisplayName, publisher.PublisherId.ToString(), publisher.Domain, publisher.IsDomainVerified),
                new Identifiers(publisher.DisplayName));
        });

    public Task<LocalExtensionOperationResponse> Delete(ResourceReference request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}