namespace TucRail.Bicep.VsMarketplace.Shared;

public static class ErrorCodes
{
    public const string FileNotFound = "file-not-found";
    public static string GetFileNotFoundMessage(string? filePath) => $"The file with the path '{filePath ?? "null filePath"}' was not found.";
    
    public const string DirectoryNotFound = "directory-not-found";
    public static string GetDirectoryNotFoundMessage(string? directoryPath) => $"The directory with the path '{directoryPath ?? "null directoryPath"}' was not found.";
    
    public const string PropertyNotFound = "property-not-found";
    public static string GetPropertyNotFoundMessage(string propertyName) => $"The property with the name '{propertyName}' was not found.";
    
    public const string VssException = "vss-exception";
    public static string GetVssExceptionMessage(string message) => $"An exception was thrown by the VSS API: {message}";
    
    public const string OperationNotSupported = "operation-not-supported";
    public static string GetOperationNotSupportedMessage(string reason) => $"The attempted operation is not supported. \n Reason: {reason}";
    
    public const string UnhandledException = "unhandled-exception";
    public static string GetUnhandledExceptionMessage(string message) => $"An unhandled exception was thrown: {message}";
    
    public const string ConfigurationDeserializationFailure = "configuration-deserialization-failure";
    public static string GetConfigurationDeserializationFailureMessage() => $"Could not deserialize the configuration data";
}