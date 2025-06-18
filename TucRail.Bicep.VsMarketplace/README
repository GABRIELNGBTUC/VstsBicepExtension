# Bicep local extension type generator

Based on the Github local extension sample provided by the Bicep team.

The main goal of this project is to have a template that can be reused across projects and do not require modifications of the `TypeGenerator` class to generate types.

## Components

### Projects

1. CLI project

A basic CLI to generate types

2. Shared project

Contains the model that is serialized and Bicep serialization attributes

3. Generator project

Contains the type generator used by the CLI

4. ExtensionHost project

Is the project that contains the handler delegated to run the local extension

### Extension configuration

You can pass a configuration object to the type generator to add any required data that is necessary for the extension to work properly.

Example: A PAT for an azure devops extension that does not use Entra ID authentication

### Bicep resources

You can define Bicep resources for your local extension by annotating a class in your types library with `BicepSerializableType`.

It is also possible to create a hierarchy by making use of the annotation `BicepParentType`. When used, the provided type will be appended similarly to Bicep native types.

Example:

```csharp

[BicepSerializableType]
public class A {}
[BicepParentType(typeof(A))]
[BicepSerializableType]
public class B{}
[BicepParentType(typeof(B))]
[BicepSerializableType]
public class C{}
```

Generates the following three resources:
- A
- A/B
- A/B/C

However, do note that while nesting resources is possible with local extension, the `parent` property is not natively supported and must be implemented by yourself.

## Usage

1. Generate the content of index.json and types.json and return it

```csharp
class Program
    {
        static void Main(string[] args)
        {
            var config = new ExtensionConfiguration();
            var kvps = TypeGenerator.GenerateTypes("test", "1.0.0", config, typeof(Program));
        }
    }
```

2. Generate the content of index.json and types.json and copy the output to a directory

```csharp
class Program
    {
        static void Main(string[] args)
        {
            var config = new ExtensionConfiguration();
            config.Add(ObjectTypePropertyFlags.None, new ExtensionConfigurationStringProperty("token", ObjectTypePropertyFlags.None, "Access token for the api", IsSecure: true));
            TypeGenerator.CopyTypesToDirectory("test", "1.0.0", config,  
                "mydirectorypath", typeof(Program));
        }
    }
```

3. Generate types using the pre-generated CLI project

```bash
./scripts/generate_types.sh
```

```ps
./scripts/Generate-Types.ps1
```

4. Publish the extension using the pre-generated CLI project

```bash
./scripts/publish.sh 'extension-destination-folder' 'extension-name'
```

```ps
./scripts/Generate-Types.ps1 'extension-destination-folder' 'extension-name'
```
