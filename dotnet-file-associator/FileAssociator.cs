using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace DotnetFileAssociator
{
    public class FileAssociator : IDisposable
    {
        private const int MAX_WINDOWS_REGISTRY_KEY_LENGTH = 255; //https://learn.microsoft.com/en-us/windows/win32/sysinfo/registry-element-size-limits

        private readonly string _exeName;
        private readonly IRegistry _registry;
        private string _programId;

        public string PathToExecutable { get; }

        /// <summary>
        /// Create a new file extension associator for a given executable.
        /// </summary>
        /// <param name="pathToExecutable">Path to executable to be launched when a file is double-clicked.</param>
        /// <exception cref="FileNotFoundException">Thrown if the provided executable does not exist within the file system.</exception>
        /// <remarks>Keep in mind setting and removing file associations requires administrator access.
        /// Checking if a file association was previously set does not require administrator access.</remarks>
        public FileAssociator(string pathToExecutable, IRegistry? registry = null)
        {
            _registry = registry ?? new WindowsRegistry();

            if (!File.Exists(pathToExecutable))
                throw new FileNotFoundException("Executable not found.", pathToExecutable);

            //We don't validate executable extensions as it can be many things: .exe, .bat, .ps1, etc
            PathToExecutable = pathToExecutable;
            _exeName = Path.GetFileName(pathToExecutable);

            //We need a unique-ish app id to save to the registry. We append a suffix so we don't accidentally overwrite an existing ProgId entry.
            _programId = _exeName.Replace(" ", string.Empty) + ".dotnet-file-associator";

            if (_programId.Length > MAX_WINDOWS_REGISTRY_KEY_LENGTH)
                throw new ArgumentOutOfRangeException(nameof(pathToExecutable), "Executable name is too long.");
        }

        /// <summary>
        /// Overrides the default, internally generated, program id that is used to create registry entries.
        /// Useful when you already have a ProgId entry in the registry you want to reuse.
        /// </summary>
        /// <param name="newProgramId">Make sure the provided id is unique and does not conflict with existing entries.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="newProgramId"/> is longer than <see cref="MAX_WINDOWS_REGISTRY_KEY_LENGTH"/> characters</exception>
        /// <remarks>CAUTION: It's possible to significantly mess up the system's file associations if you are not careful: 
        /// make sure you provide a unique <paramref name="newProgramId"/> value</remarks>
        public void OverideProgramId(string newProgramId)
        {
            if (newProgramId.Length > MAX_WINDOWS_REGISTRY_KEY_LENGTH)
                throw new ArgumentOutOfRangeException(nameof(newProgramId), "Program Id is too long.");

            _programId = newProgramId;
        }

        /// <summary>
        /// Associates <see cref="PathToExecutable"/> with <paramref name="fileExtension"/> files.
        /// </summary>
        /// <param name="pathToExecutable">Path to executable that should be used to open <paramref name="extension"/> files.</param>
        /// <param name="extension">File extension with or without the dot "." in the beginning.</param>
        /// <param name="extensionLongName">Optional expanded name of the extension.
        /// E.g. ".pdf" => "Portable Document Format File", ".png" => "Portable Network Graphics Image"</param>
        /// <exception cref="ArgumentNullException">Thrown if no file extension is provided.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the provided file extension is too long.</exception>
        /// <exception cref="ArgumentException">Thrown if the provided file extension contains illegal characters.</exception>
        /// <exception cref="NotRunningAsAdministratorException">This operation requires administrator privileges.</exception>
        /// <exception cref="FileNotFoundException"> Thrown if the provided executable does not exist within the file system.</exception>
        public static void SetFileAssociation(string pathToExecutable, string extension, string? extensionLongName = null)
        {
            using var fileAssociator = new FileAssociator(pathToExecutable);
            fileAssociator.SetFileAssociation(new FileExtensionDefinition(extension, extensionLongName));
        }

        /// <summary>
        /// Associates <see cref="PathToExecutable"/> with <paramref name="fileExtension"/> files.
        /// </summary>
        /// <param name="fileExtension">File extension definition to associate the executable with</param>
        /// <exception cref="NotRunningAsAdministratorException">This operation requires administrator privileges.</exception>
        /// <remarks>The executable will become the default app for double-clicking files of this type. It will also get added to the OpenWith list in Windows.</remarks>
        public void SetFileAssociation(FileExtensionDefinition fileExtension)
        {
            if (_registry.RequiresAdministratorPrivileges && !_registry.IsCurrentUserAdministrator)
                throw new NotRunningAsAdministratorException();

            SetAsDefaultApp(fileExtension);
            AddToOpenWithListForCurrentUser(fileExtension);
            NotifyWindowsFileExplorer();
        }

        /// <summary>
        /// Deasssociates <see cref="PathToExecutable"/> with <paramref name="fileExtension"/> files.
        /// </summary>
        /// <param name="pathToExecutable">Path to executable that should no longer be used to open <paramref name="extension"/> files.</param>
        /// <param name="extension">File extension with or without the dot "." in the beginning.</param>
        /// <exception cref="ArgumentNullException">Thrown if no file extension is provided.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the provided file extension is too long.</exception>
        /// <exception cref="ArgumentException">Thrown if the provided file extension contains illegal characters.</exception>
        /// <exception cref="NotRunningAsAdministratorException">This operation requires administrator privileges.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the provided executable does not exist within the file system.</exception>
        public static void RemoveFileAssociation(string pathToExecutable, string extension)
        {
            using var fileAssociator = new FileAssociator(pathToExecutable);
            fileAssociator.RemoveFileAssociation(new FileExtensionDefinition(extension));
        }

        /// <summary>
        /// Deasssociates <see cref="PathToExecutable"/> with <paramref name="fileExtension"/> files.
        /// </summary>
        /// <param name="fileExtension">File extension definition to associate the executable with</param>
        /// <exception cref="NotRunningAsAdministratorException">This operation requires administrator privileges.</exception>
        public void RemoveFileAssociation(FileExtensionDefinition fileExtension)
        {
            if (_registry.RequiresAdministratorPrivileges && !_registry.IsCurrentUserAdministrator)
                throw new NotRunningAsAdministratorException();

            var wasUnset = UnsetAsDefaultApp(fileExtension);
            var wasRemoved = RemoveFromOpenWithListForCurrentUser(fileExtension);
            if (wasUnset || wasRemoved)
                NotifyWindowsFileExplorer();
        }

        /// <summary>
        /// Makes <see cref="PathToExecutable"/> the default app for the file extension for all users
        /// </summary>
        /// <param name="fileExtension">File extension definition to associate with <see cref="PathToExecutable"/></param>
        /// <param name="command">The command to be executed when a file with the given extension is double-clicked.
        /// Here '{0}' will be the path to your executable and %1 will be the absolute path to the file that was double clicked</param>
        /// <exception cref="NotRunningAsAdministratorException">Thrown when called while not running as administrator.</exception>
        /// <exception cref="ArgumentException">Thrown when an invalid command is passed.</exception>
        private void SetAsDefaultApp(FileExtensionDefinition fileExtension, string command = "\"{0}\" \"%1\"")
        {
            //Create a program id entry for our executable
            DefineProgramId(fileExtension, command);

            //Finally, set our program id entry as the default for the extension
            using var extensionKey = _registry.GetClassesRootRegistry.CreateSubKey(fileExtension.Extension);
            extensionKey.SetValue(null, _programId);
        }

        /// <summary>
        /// Creates an entry under \HKEY_CLASSES_ROOT\<program-id> that can be referenced as
        /// the extension's default program and be added to the MRU List for that extension.
        /// </summary>
        /// <exception cref="NotRunningAsAdministratorException">Thrown when called while not running as administrator.</exception>
        /// <exception cref="ArgumentException">Thrown when an invalid command is passed.</exception>
        internal void DefineProgramId(FileExtensionDefinition fileExtension, string command)
        {
            if (_registry.RequiresAdministratorPrivileges && !_registry.IsCurrentUserAdministrator)
                throw new NotRunningAsAdministratorException();

            if (!command.Contains("{0}") || !command.Contains("%1"))
                throw new ArgumentException("The command must contain both '{0}' and '%1' placeholders.", nameof(command));

            //Create a program id entry for our executable
            using var programIdKey = _registry.GetClassesRootRegistry.CreateSubKey(_programId);

            //Set the long name of the extension the executable supports
            programIdKey.SetValue(null, fileExtension.ExtensionLongName);

            using var openKey = programIdKey.CreateSubKey("shell\\open");
            openKey.SetValue("icon", $"{PathToExecutable},0"); //Use the executable's icon

            //Set the command to be run when a user opens a file with the given extension
            using var commandKey = openKey.CreateSubKey("command");
            command = string.Format(CultureInfo.InvariantCulture, command, PathToExecutable);
            commandKey.SetValue(null, command);
        }

        /// <summary>
        /// Removes <see cref="PathToExecutable"/> as the default app for the file extension for all users
        /// </summary>
        /// <param name="fileExtension">File extension to disassociate with <see cref="PathToExecutable"/></param>
        /// <returns>False if the current executable isn't the default app for <paramref name="fileExtension"/> in the first place. True, otherwise.</returns>
        private bool UnsetAsDefaultApp(FileExtensionDefinition fileExtension)
        {
            using var extensionKey = _registry.GetClassesRootRegistry.CreateSubKey(fileExtension.Extension);
            var defaultProgramId = extensionKey.GetValue(null);
            if (_programId.Equals(defaultProgramId))
            {
                //We don't know what the new default executable should be so the only solution is to have no default
                _registry.GetClassesRootRegistry.DeleteSubKeyTree(fileExtension.Extension);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Makes <see cref="PathToExecutable"/> the top pick in the 'Open With' dialog in Windows for <paramref name="fileExtension"/>
        /// </summary>
        /// <param name="fileExtension">File extension to associate with <see cref="PathToExecutable"/></param>
        private void AddToOpenWithListForCurrentUser(FileExtensionDefinition fileExtension)
        {
            using var mruList = new FileExplorerOpenWithMRUList(fileExtension, _registry);
            mruList.MakeExecutableMostRecentlyUsed(_exeName);
            mruList.AssociateProgramId(_programId);
            mruList.SaveChanges();
        }

        /// <summary>
        /// Removes <see cref="PathToExecutable"/> from the 'Open With' dialog options in Windows for <paramref name="fileExtension"/>.
        /// </summary>
        /// <param name="fileExtension">File extension to disassociate with <see cref="PathToExecutable"/></param>
        /// <returns>True if anything was actually removed. False, otherwise.</returns>
        private bool RemoveFromOpenWithListForCurrentUser(FileExtensionDefinition fileExtension)
        {
            using var mruList = new FileExplorerOpenWithMRUList(fileExtension, _registry);
            var wasRemoved = mruList.RemoveExecutableFromRecentlyUsedList(_exeName);
            var wasDisassociated = mruList.DisassociateProgramId(_programId);
            if (wasRemoved || wasDisassociated)
            {
                mruList.SaveChanges();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if <see cref="PathToExecutable"/> has been previously associated with <paramref name="extension"/> files.
        /// </summary>
        /// <param name="pathToExecutable">Path to executable that should no longer be used to open <paramref name="extension"/> files.</param>
        /// <param name="extension">File extension with or without the dot "." in the beginning.</param>
        /// <remarks>Administrator access is NOT required for this method.</remarks>
        /// <exception cref="FileNotFoundException">Thrown if the provided executable does not exist within the file system.</exception>
        public static bool IsFileAssociationSet(string pathToExecutable, string extension)
        {
            using var fileAssociator = new FileAssociator(pathToExecutable);
            return fileAssociator.IsFileAssociationSet(new FileExtensionDefinition(extension));
        }

        /// <summary>
        /// Checks if <see cref="PathToExecutable"/> has been previously associated with <paramref name="fileExtension"/> files.
        /// </summary>
        /// <param name="fileExtension">File extension definition to check association with.</param>
        /// <remarks>Administrator access is NOT required for this method.</remarks>
        public bool IsFileAssociationSet(FileExtensionDefinition fileExtension)
            => IsProgramIdDefined() && IsSetAsDefaultApp(fileExtension) && IsFirstInOpenWithListForCurrentUser(fileExtension);

        private bool IsSetAsDefaultApp(FileExtensionDefinition fileExtension)
        {
            using var extensionKey = _registry.GetClassesRootRegistry.OpenSubKey(fileExtension.Extension);
            if (extensionKey is null)
                return false;

            var defaultProgramId = extensionKey.GetValue(null);
            return _programId.Equals(defaultProgramId);
        }

        private bool IsProgramIdDefined()
        {
            using var programIdKey = _registry.GetClassesRootRegistry.OpenSubKey(_programId);
            if (programIdKey is null)
                return false;

            using var openKey = programIdKey.OpenSubKey("shell\\open");
            if (openKey is null)
                return false;

            using var commandKey = openKey.OpenSubKey("command");
            if (commandKey is null)
                return false;

            return true;
        }

        private bool IsFirstInOpenWithListForCurrentUser(FileExtensionDefinition fileExtension)
        {
            using var mruList = new FileExplorerOpenWithMRUList(fileExtension, _registry);
            if (mruList.ExecutablesInMRUOrder.Count == 0)
                return false;

            return mruList.ExecutablesInMRUOrder[0].Equals(this._exeName, StringComparison.Ordinal);
        }

        private static void NotifyWindowsFileExplorer()
            => SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        public void Dispose()
        {
            try
            {
                this._registry.Dispose();
            }
            catch { /*swallow*/ }
        }
    }
}
