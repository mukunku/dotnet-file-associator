# dotnet-file-associator
C# library and .NET tool for managing file extension associations on Windows 10/11.

Easily set or remove default applications for specific file extensions programmatically, with full registry integration.

# Example Usages

#### Set a file association:
```csharp
// Associates ".abc" files with MyApp.exe and sets a custom long name for the extension.
FileAssociator.SetFileAssociation(
    @"C:\Program Files\MyApp\MyApp.exe",
    ".abc",
    "My Custom File Type" //<-- Optional long name for the file type
);
```

#### Remove a file association:
```csharp
using DotnetFileAssociator;

// Removes the association between ".abc" files and MyApp.exe.
FileAssociator.RemoveFileAssociation(
    @"C:\Program Files\MyApp\MyApp.exe",
    ".abc"
);
```