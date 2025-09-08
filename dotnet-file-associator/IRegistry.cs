using System;
using System.Collections.Generic;

namespace DotnetFileAssociator
{
    public interface IRegistry : IDisposable
    {
        public bool RequiresAdministratorPrivileges { get; }
        public bool IsCurrentUserAdministrator { get; }
        public IRegistry GetClassesRootRegistry { get; }
        public IRegistry GetCurrentUserRegistry { get; }

        public IRegistry CreateSubKey(string key);

        public void DeleteSubKeyTree(string key);

        public IRegistry? OpenSubKey(string key);

        public IEnumerable<string> GetValueNames();

        public void SetValue(string? name, object value);
        public void SetString(string? name, string value);
        public void DeleteValue(string name);
        public object? GetValue(string? name);
    }
}
