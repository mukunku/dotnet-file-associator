# dotnet-file-associator [![NuGet version](https://badge.fury.io/nu/dotnet-file-associator.svg)](https://www.nuget.org/packages/dotnet-file-associator)

C# library and .NET tool for managing file extension associations on Windows 10/11.

Easily set or remove default applications for specific file extensions programmatically, with full registry integration.


# Example Usages

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
