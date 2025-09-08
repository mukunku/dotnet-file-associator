using Microsoft.Win32;
using System.Collections.Generic;
using System.Security.Principal;

namespace DotnetFileAssociator
{
    public class WindowsRegistry : IRegistry
    {
        private RegistryKey _registryKey;

        /// <summary>
        /// Creates a Windows Registry interface
        /// </summary>
        /// <param name="baseRegistry">E.g. Registry.ClassesRoot</param>
        public WindowsRegistry(RegistryKey? baseRegistry = null)
        {
            _registryKey = baseRegistry ?? Registry.LocalMachine;
        }

        public IRegistry GetClassesRootRegistry => new WindowsRegistry(Registry.ClassesRoot);

        public IRegistry GetCurrentUserRegistry => new WindowsRegistry(Registry.CurrentUser);

        public bool RequiresAdministratorPrivileges => true;

        /// <summary>
        /// Returns true if the current process is running with administrator privileges.
        /// </summary>
        public bool IsCurrentUserAdministrator => 
            new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        
        public IRegistry CreateSubKey(string key)
            => new WindowsRegistry(_registryKey.CreateSubKey(key));

        public void DeleteSubKeyTree(string key)
        {
            if (_registryKey.OpenSubKey(key) is not null)
                _registryKey.DeleteSubKeyTree(key);
        }

        public IEnumerable<string> GetValueNames()
            => _registryKey.GetValueNames();

        public void SetValue(string? name, object value)
            => _registryKey.SetValue(name, value);

        public void SetString(string? name, string value)
            => _registryKey.SetValue(name, value, RegistryValueKind.String);

        public object? GetValue(string? name)
            => _registryKey.GetValue(name, null);

        public void DeleteValue(string name)
            => _registryKey.DeleteValue(name);

        public IRegistry? OpenSubKey(string key)
            => _registryKey.OpenSubKey(key) is not null ? 
                new WindowsRegistry(_registryKey.OpenSubKey(key)) : null;

        public void Dispose()
        {
            try
            {
                _registryKey.Dispose();
            }
            catch { /*swallow*/ }
        }
    }
}
