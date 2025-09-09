# dotnet-file-associator 

C# library and .NET tool for managing file extension associations on Windows 10/11.

Easily set or remove default applications for specific file extensions programmatically, with full registry integration.

# Description

This repo contains both a C# library and a .NET tool for managing file extension associations on Windows 10/11. 

The library allows developers to programmatically set, remove, and check file associations, while the tool provides a command-line interface for performing these actions.

Both packages can be found on [NuGet.org](https://www.nuget.org/profiles/mukunku) and [GitHub Packages](https://github.com/mukunku?tab=packages&repo_name=dotnet-file-associator)

## C# Library [![NuGet version](https://badge.fury.io/nu/dotnet-file-associator.svg?icon=si%3Anuget)](https://www.nuget.org/packages/dotnet-file-associator)

### How to Install

```shell
dotnet add package dotnet-file-associator
```

### Example Usages

#### Set file association:

```csharp
using DotnetFileAssociator;

// Associates ".abc" files with MyApp.exe.
FileAssociator.SetFileAssociation(
    @"C:\Program Files\MyApp\MyApp.exe",
    ".abc"
);
```

#### Remove file association:

```csharp
using DotnetFileAssociator;

// Removes the association between ".abc" files and MyApp.exe.
FileAssociator.RemoveFileAssociation(
    @"C:\Program Files\MyApp\MyApp.exe",
    ".abc"
);
```

#### Check file association:
```csharp
using DotnetFileAssociator;

// Checks if MyApp.exe has been previously associated with ".abc" files.
FileAssociator.IsFileAssociationSet(
    @"C:\Program Files\MyApp\MyApp.exe",
    ".abc"
);
```

## .NET Tool [![NuGet version](https://badge.fury.io/nu/dotnet-file-associator.tool.svg?icon=si%3Anuget)](https://www.nuget.org/packages/dotnet-file-associator.tool)

### How to Install

Install the .NET tool (optionally globally) using the following command:
```shell
dotnet tool install dotnet-file-associator.tool -g
```

It can then be invoked such as:
```shell
dotnet dotnet-file-associator --version
```
or the long version:
```shell
dotnet tool run dotnet-file-associator --help
```

### Example Usages

#### Set file association:
```shell
dotnet dotnet-file-associator set -p "C:\Program Files\MyApp\MyApp.exe" -e ".abc"
```

#### Remove file association:
```shell
dotnet dotnet-file-associator remove -p "C:\Program Files\MyApp\MyApp.exe" -e ".abc"
```

#### Check file association:
```shell
dotnet dotnet-file-associator check -p "C:\Program Files\MyApp\MyApp.exe" -e ".abc"
```
<sub>Status code 0 is returned if the association exists, otherwise 1 is returned.</sub>