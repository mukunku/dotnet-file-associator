using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

namespace DotnetFileAssociator
{
    internal class FileExplorerOpenWithMRUList : IDisposable
    {
        private IRegistry _registry;
        private OrderedDictionary _executablesInMRUOrder = new();
        private Dictionary<string, bool> _executableProgramIds = new();

        private string _fileExtensionRegistryKeyName
            => $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{FileExtension.Extension}";

        public FileExtensionDefinition FileExtension { get; }

        public IReadOnlyList<string> ExecutablesInMRUOrder
        {
            get
            {
                var list = new List<string>(_executablesInMRUOrder.Count);
                for (var i = 0; i < _executablesInMRUOrder.Count; i++)
                {
                    list.Add((string)_executablesInMRUOrder[i]);
                }
                return list.AsReadOnly();
            }
        }

        public IReadOnlyList<string> ExecutableProgramIds => _executableProgramIds.Keys.ToList().AsReadOnly();

        /// <summary>
        /// Allows reading and modifying the Open With list of a specific extension
        /// </summary>
        /// <param name="fileExtensionDefinition"></param>
        /// <remarks>This type doesn't always provide everything shown in the Windows
        /// File Explorer Open With dialog. However adding new entries here will
        /// allow the exe to show up in the list.</remarks>
        internal FileExplorerOpenWithMRUList(FileExtensionDefinition fileExtension, IRegistry registry)
        {
            if (registry is null)
                throw new ArgumentNullException(nameof(registry));

            _registry = registry;
            FileExtension = fileExtension;
            ReloadAllEntries();
        }

        internal void ReloadAllEntries()
        {
            //TODO: Make these work with read-only access
            ReloadOpenWithListEntries();
            ReloadOpenWithProgidsEntries();
        }

        private void ReloadOpenWithListEntries()
        {
            _executablesInMRUOrder.Clear();
            using var extensionFileExplorerKey = _registry.GetCurrentUserRegistry.OpenSubKey(_fileExtensionRegistryKeyName);
            if (extensionFileExplorerKey is null)
            {
                return;
            }

            using var openWithListKey = extensionFileExplorerKey.OpenSubKey("OpenWithList");
            if (openWithListKey is null)
            {
                return;
            }

            //Get all the previous executables used to open this extension
            Dictionary<string, string> previousExecutables = new();
            foreach (var letter in GetAlphabet())
            {
                var executableName = openWithListKey.GetValue(letter) as string;
                if (string.IsNullOrWhiteSpace(executableName))
                    continue;

                previousExecutables.Add(letter, executableName!);
            }

            //Now order the previous executables based on the Most Recently Used (MRU) order
            var mostRecentlyUsedList = openWithListKey.GetValue("MRUList") as string;
            if (!string.IsNullOrWhiteSpace(mostRecentlyUsedList))
            {
                foreach (char c in mostRecentlyUsedList!)
                {
                    string letter = c.ToString();
                    if (previousExecutables.TryGetValue(letter, out var executableName))
                    {
                        _executablesInMRUOrder.Add(letter, executableName);
                        previousExecutables.Remove(letter);
                    }
                }
            }

            //Add any leftover executables, if any. (we don't care about order if they weren't in the MRU list to begin with)
            foreach (var executable in previousExecutables)
            {
                _executablesInMRUOrder.Add(executable.Key, executable.Value);
            }
        }

        private void ReloadOpenWithProgidsEntries()
        {
            _executableProgramIds.Clear();
            using var extensionFileExplorerKey = _registry.GetCurrentUserRegistry.OpenSubKey(_fileExtensionRegistryKeyName);
            if (extensionFileExplorerKey is null)
            {
                return;
            }

            using var openWithProgidsKey = extensionFileExplorerKey.OpenSubKey("OpenWithProgids");
            if (openWithProgidsKey is null)
            {
                return;
            }

            foreach (var valueName in openWithProgidsKey.GetValueNames())
            {
                if (string.IsNullOrWhiteSpace(valueName))
                    continue;

                var toBeDeleted = false;
                _executableProgramIds.Add(valueName, toBeDeleted);
            }
        }

        /// <summary>
        /// Marks the provided exe to be saved as the most recently used exe in the MRU list.
        /// Utilize <see cref="SaveChanges"/> to persist the changes to disk.
        /// If <paramref name="exeName"/>already exists at a different index it will be moved to the front of the list.
        /// </summary>
        /// <param name="exeName">Executable name (not file path) to insert. E.g. "My-App.exe"</param>
        public void MakeExecutableMostRecentlyUsed(string exeName)
        {
            if (string.IsNullOrWhiteSpace(exeName))
                throw new ArgumentNullException(nameof(exeName));

            //Double check this is not a file path and just the executable name
            exeName = Path.GetFileName(exeName);

            string? letter = null;
            //Remove the executable if it's already in the list
            for (var i = 0; i < _executablesInMRUOrder.Count; i++)
            {
                if (_executablesInMRUOrder[i].Equals(exeName)) //This performs a case-sensitive string comparison. Would it make more sense for it to be case-insensitive?
                {
                    letter = _executablesInMRUOrder.Keys.Get<string>(i); //re-use the existing letter
                    _executablesInMRUOrder.RemoveAt(i);
                    break;
                }
            }

            if (letter is null)
            {
                //Determine which letter of the alphabet we can use
                if (_executablesInMRUOrder.Count > 0)
                {
                    foreach (var _letter in GetAlphabet())
                    {
                        if (!_executablesInMRUOrder.Contains(_letter))
                        {
                            letter = _letter;
                            break;
                        }
                    }

                    if (letter is null)
                    {
                        //If the entire alphabet was somehow exhausted, hijack the least recently used letter
                        letter = _executablesInMRUOrder.Keys.Last<string>();
                        _executablesInMRUOrder.RemoveAt(_executablesInMRUOrder.Count - 1);
                    }
                }
                else
                {
                    letter = "a";
                }
            }

            _executablesInMRUOrder.Insert(0, letter, exeName);
        }

        /// <summary>
        /// Marks the provided exe to be removed from the MRU list.
        /// Utilize <see cref="SaveChanges"/> to persist the changes to disk.
        /// If <paramref name="exeName"/>already exists at a different index it will be moved to the front of the list.
        /// </summary>
        /// <param name="exeName">Executable name (not file path) to insert. E.g. "My-App.exe"</param>
        /// <returns>True if the exe was in the list in the first place, false if not.</returns>
        /// <remarks>We don't delete the letter, only remove it from MRUList, hopefully that's okay.</remarks>
        public bool RemoveExecutableFromRecentlyUsedList(string exeName)
        {
            foreach ((string Letter, string ExeName) pair in _executablesInMRUOrder.AsEnumerable<string, string>())
            {
                if (pair.ExeName.Equals(exeName, StringComparison.Ordinal))
                {
                    _executablesInMRUOrder.Remove(pair.Letter);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Saves the current MRU exe order and program ids into the registry, creating new entries for any new MRU list letters
        /// </summary>
        /// <exception cref="NotRunningAsAdministratorException">This operation requires Administrator privileges.</exception>
        public void SaveChanges()
        {
            if (_registry.RequiresAdministratorPrivileges && !_registry.IsCurrentUserAdministrator)
                throw new NotRunningAsAdministratorException();

            using var extensionFileExplorerKey = _registry.GetCurrentUserRegistry.CreateSubKey(_fileExtensionRegistryKeyName);
            using var openWithListKey = extensionFileExplorerKey.CreateSubKey("OpenWithList");

            #region Save OpenWithList SubKey
            //Write all letter/exe pairs
            List<string> mruLetterList = new(_executablesInMRUOrder.Count);
            foreach (var pair in _executablesInMRUOrder.AsEnumerable<string, string>())
            {
                openWithListKey.SetString(pair.Key, pair.Value);
                mruLetterList.Add(pair.Key);
            }

            //Write the MRU list
            string mruList = string.Join(string.Empty, mruLetterList);
            openWithListKey.SetString("MRUList", mruList);
            #endregion

            #region Save OpenWithProgids SubKey
            using var openWithProgidsKey = extensionFileExplorerKey.CreateSubKey("OpenWithProgids");
            //Save program id's not marked for deletion
            foreach (var programId in _executableProgramIds.Where(programId => !programId.Value))
            {
                openWithProgidsKey.SetValue(programId.Key, 0);
            }
            //Delete program id's that were marked for deletion
            foreach (var programId in _executableProgramIds.Where(programId => programId.Value))
            {
                openWithProgidsKey.DeleteValue(programId.Key);
            }
            //Cleanup deleted program ids
            var programIdsToDelete = _executableProgramIds.Where(programId => programId.Value).Select(programId => programId.Key).ToList();
            foreach (var programIdToDelete in programIdsToDelete)
            {
                _executableProgramIds.Remove(programIdToDelete);
            }
            #endregion
        }

        /// <summary>
        /// Marks the provided program id (Defined under \HKEY_CLASSES_ROOT\<program-id>) for association with the current file extension.
        /// Utilize <see cref="SaveChanges"/> to persist the changes to disk.
        /// </summary>
        /// <param name="programId">The program id to link to the file extension.</param>
        /// <param name="throwIfProgramIdIsNotRegistered">Whether to validate <paramref name="programId"/>exists under HKEY_CLASSES_ROOT.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the provided <paramref name="programId"/> does not exist in the registry and <paramref name="throwIfProgramIdIsNotRegistered"/> is true.
        /// </exception>
        public void AssociateProgramId(string programId, bool throwIfProgramIdIsNotRegistered = true)
        {
            if (_executableProgramIds.ContainsKey(programId))
            {
                _executableProgramIds[programId] = false; //Mark it as not to be deleted, just in-case
                return;
            }

            if (throwIfProgramIdIsNotRegistered && _registry.GetClassesRootRegistry.OpenSubKey(programId) is null)
            {
                throw new InvalidOperationException($"Cannot associate program id '{programId}' with extension '{FileExtension.Extension}' " +
                    @$"as it does not exist at 'HKEY_CLASSES_ROOT\{programId}'");
            }

            _executableProgramIds.Add(programId, false);
        }

        /// <summary>
        /// Marks the provided program id for deletion. 
        /// Utilize <see cref="SaveChanges"/> to persist the changes to disk.
        /// </summary>
        /// <param name="programId">The program id to unlink from the file extension.</param>
        /// <returns>True if an association existed in the first place, false if not.</returns>
        public bool DisassociateProgramId(string programId)
        {
            if (_executableProgramIds.ContainsKey(programId))
            {
                //Mark for deletion
                _executableProgramIds[programId] = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns every character in English alphabet in order from 'a' to 'z'
        /// </summary>
        /// <returns>Lowercase letters as strings</returns>
        /// <remarks>Now we know our ABC's</remarks>
        private static IEnumerable<string> GetAlphabet()
        {
            for (char c = 'a'; c <= 'z'; c++)
            {
                yield return c.ToString();
            }
        }

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
