// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Azure.Core;
using Azure.Identity;
using Bicep.Local.Extension.Protocol;
using Bicep.Local.Extension.Rpc;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Gallery.WebApi;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;
using TucRail.Bicep.VsMarketplace.Shared;
using TucRail.Bicep.VsMarketplace.Shared.Models;
using ErrorDetail = Bicep.Local.Extension.Protocol.ErrorDetail;
using ResourceReference = Bicep.Local.Extension.Protocol.ResourceReference;
using ResourceSpecification = Bicep.Local.Extension.Protocol.ResourceSpecification;

namespace TucRail.Bicep.VsMarketplace.ExtensionHost.Helpers;

public static class RequestHelper
{
    public static async Task<LocalExtensionOperationResponse> HandleRequest(JsonObject? config,
        Func<GalleryHttpClient, Task<LocalExtensionOperationResponse>> onExecuteFunc)
    {
        //For some godforsaken reason the marketplace is an ADO organization?
        var organizationUrl = "https://marketplace.visualstudio.com/";//config!["organizationUrl"]!.GetValue<string>();
        var configuration = JsonSerializer.Deserialize<Configuration>(config, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (configuration is null)
        {
            Console.WriteLine($"Failed to read configuration from object: {config?.ToJsonString()}");
            return CreateErrorResponse(ErrorCodes.ConfigurationDeserializationFailure, 
                ErrorCodes.GetConfigurationDeserializationFailureMessage());
        }
        VssCredentials? credentials = null;
        switch (configuration.AuthenticationMode)
        {
            case AuthenticationMode.PAT:
            {
                var accessToken = configuration.PatToken;
                credentials = new VssBasicCredential("", accessToken);
                break;
            }
            case AuthenticationMode.Entra:
            {
                var azureCredentials = new DefaultAzureCredential();
                var accessToken = await azureCredentials.GetTokenAsync(new TokenRequestContext(new[] {"499b84ac-1321-427f-aa17-267ca6975798/.default"}));
                credentials = new VssAadCredential(new VssAadToken("Bearer", accessToken.Token));
                break;
            }
        }
        var client = new GalleryHttpClient(new Uri(organizationUrl), credentials);

        try
        {
            return await onExecuteFunc(client);
        }
        catch (Exception exception)
        {
            // Github example on how to return multiple errors
            // if (exception is ApiException apiException &&
            //     apiException.ApiError?.Errors is {} apiErrors)
            // {
            //     var errorDetails = apiErrors
            //         .Select(error => new ErrorDetail(error.Code, error.Field ?? "", error.Message)).ToArray();
            //
            //     return CreateErrorResponse("ApiError", apiException.ApiError.Message, errorDetails);
            // }

            return CreateErrorResponse(ErrorCodes.UnhandledException, ErrorCodes.GetUnhandledExceptionMessage(exception.ToString()));
        }
    }

    public static TProperties GetProperties<TProperties>(JsonObject properties)
        => properties.Deserialize<TProperties>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    public static LocalExtensionOperationResponse CreateSuccessResponse<TProperties, TIdentifiers>(
        ResourceReference request, TProperties properties, TIdentifiers identifiers)
    {
        return new(
            new(
                request.Type,
                request.ApiVersion,
                "Succeeded",
                (JsonNode.Parse(JsonSerializer.Serialize(identifiers,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web))) as JsonObject)!,
                request.Config,
                (JsonNode.Parse(JsonSerializer.Serialize(properties,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web))) as JsonObject)!),
            null);
    }

    public static LocalExtensionOperationResponse CreateSuccessResponse<TProperties, TIdentifiers>(
        ResourceSpecification request, TProperties properties, TIdentifiers identifiers)
    {
        return new(
            new(
                request.Type,
                request.ApiVersion,
                "Succeeded",
                (JsonNode.Parse(JsonSerializer.Serialize(identifiers,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web))) as JsonObject)!,
                request.Config,
                (JsonNode.Parse(JsonSerializer.Serialize(properties,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web))) as JsonObject)!),
            null);
    }

    public static LocalExtensionOperationResponse CreateErrorResponse(string code, string message,
        ErrorDetail[]? details = null, string? target = null)
    {
        return new LocalExtensionOperationResponse(
            null,
            new(new(code, target ?? "", message, details ?? [], [])));
    }
    
    public static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input) || char.IsLower(input[0]))
        {
            return input; // Déjà en camelCase ou vide
        }

        // Convertir la première lettre en minuscule
        return char.ToLower(input[0]) + input.Substring(1);
    }
    

    public static JsonNode? GetIdentifierData(ResourceReference reference, string input) => reference.Identifiers[ToCamelCase(input)];
    
}