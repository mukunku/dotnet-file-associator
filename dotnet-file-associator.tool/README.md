# dotnet-file-associator [![NuGet version](https://badge.fury.io/nu/dotnet-file-associator.tool.svg?icon=si%3Anuget)](https://www.nuget.org/packages/dotnet-file-associator.tool)

C# library and .NET tool for managing file extension associations on Windows 10/11.

Easily set or remove default applications for specific file extensions programmatically, with full registry integration.

# How to Install

Install the .NET tool (optionally globally) using the following command:
```powershell
dotnet tool install dotnet-file-associator.tool -g
```
<hr>

It can then be invoked such as:
```powershell
dotnet dotnet-file-associator --version
```
or the long version:
```powershell
dotnet tool run dotnet-file-associator --help
```

# Example Usages

#### Set file association:
```powershell
dotnet dotnet-file-associator set -p "C:\Program Files\MyApp\MyApp.exe" -e ".abc"
```

#### Remove file association:
```csharp
dotnet dotnet-file-associator remove -p "C:\Program Files\MyApp\MyApp.exe" -e ".abc"
```

#### Check file association:

```csharp
dotnet dotnet-file-associator check -p "C:\Program Files\MyApp\MyApp.exe" -e ".abc"
```
<sub>Status code 0 is returned if the association exists, otherwise 1 is returned.</sub>