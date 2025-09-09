using DotnetFileAssociator;

namespace dotnet_file_associator.tests
{
    [TestClass]
    public sealed class SanityTests
    {
        private const string DUMMY_PROGRAM_ID = "DotnetFileAssociator.Tests.DummyApp";
        private IRegistry? _registry;
        private FileAssociator? _testFileAssociator;
        private FileExtensionDefinition _testFileExtension = new FileExtensionDefinition(".test", "Test File");

        private FileExplorerOpenWithMRUList GetMRUListInstance() => new FileExplorerOpenWithMRUList(_testFileExtension, _registry!);

        [TestInitialize]
        public void InitializeTest()
        {
            _registry = new MockRegistry();
            _testFileAssociator = new FileAssociator(Path.GetTempFileName(), _registry);
            _testFileAssociator.OverideProgramId(DUMMY_PROGRAM_ID);
        }

        [TestCleanup]
        public void CleanupTest()
        {
            _registry?.Dispose();
            _testFileAssociator?.Dispose();
        }

        [TestMethod]
        public void FileAssociationTest()
        {
            Assert.IsFalse(_testFileAssociator!.IsFileAssociationSet(_testFileExtension), "File association shouldn't be set already");
            _testFileAssociator.SetFileAssociation(_testFileExtension);
            Assert.IsTrue(_testFileAssociator.IsFileAssociationSet(_testFileExtension), "File association wasn't set correctly");
            _testFileAssociator.RemoveFileAssociation(_testFileExtension);
            Assert.IsFalse(_testFileAssociator.IsFileAssociationSet(_testFileExtension), "File association shouldn't be set still");
        }

        [TestMethod]
        public void MRUListProgramIdTest()
        {
            using var mruList = GetMRUListInstance();
            Assert.IsFalse(mruList!.DisassociateProgramId(DUMMY_PROGRAM_ID), "There shouldn't be an associated program id yet");

            Assert.Throws<InvalidOperationException>(() => { mruList.AssociateProgramId(DUMMY_PROGRAM_ID); },
                "The program id shouldn't be defined yet which should have caused an exception to be thrown");

            //Actually define the program id now
            _testFileAssociator!.DefineProgramId(_testFileExtension, "\"{0}\" \"%1\"");

            //Now it should work since the program id is defined
            mruList.AssociateProgramId(DUMMY_PROGRAM_ID);

            //Confirm it was associated as expected
            Assert.IsTrue(mruList.DisassociateProgramId(DUMMY_PROGRAM_ID), "The program id wasn't associated with the extension when it should have been");

            //Associate again
            mruList.AssociateProgramId(DUMMY_PROGRAM_ID);

            //Reload state to confirm nothing was persisted to disk
            mruList.ReloadAllEntries();
            Assert.IsFalse(mruList.DisassociateProgramId(DUMMY_PROGRAM_ID), "The association shouldn't have existed because we didn't call SaveChanges() yet");

            //Associate again and this time call SaveChanges()
            mruList.AssociateProgramId(DUMMY_PROGRAM_ID);
            mruList.SaveChanges();

            //Reload state to confirm it was persisted to disk
            mruList.ReloadAllEntries();
            Assert.IsTrue(mruList.DisassociateProgramId(DUMMY_PROGRAM_ID), "The program id wasn't associated with the extension when it should have been");
        }

        [TestMethod]
        public void MRUListLettersTest()
        {
            const string executable1 = "MyExecutable1.exe";
            const string executable2 = "MyExecutable2.exe";
            const string executable3 = "MyExecutable3.exe";
            const string executable4 = "MyExecutable4.exe";

            using var mruList = GetMRUListInstance();
            Assert.IsEmpty(mruList!.ExecutablesInMRUOrder, $"Found: {mruList.ExecutablesInMRUOrder.First()}");

            mruList.MakeExecutableMostRecentlyUsed(executable1);
            Assert.ContainsSingle(mruList.ExecutablesInMRUOrder);
            Assert.AreEqual(executable1, mruList.ExecutablesInMRUOrder.First());

            mruList.MakeExecutableMostRecentlyUsed(executable2);
            Assert.HasCount(2, mruList.ExecutablesInMRUOrder);
            Assert.AreEqual(executable2, mruList.ExecutablesInMRUOrder.First());

            //Confirm data wasn't persisted because we didn't call SaveChanges()
            mruList.ReloadAllEntries();
            Assert.IsEmpty(mruList.ExecutablesInMRUOrder);

            //Re-add first couple executables and persist it this time
            mruList.MakeExecutableMostRecentlyUsed(executable1);
            mruList.MakeExecutableMostRecentlyUsed(executable2);
            mruList.SaveChanges();
            mruList.ReloadAllEntries();
            Assert.HasCount(2, mruList.ExecutablesInMRUOrder);
            Assert.AreEqual(executable1, mruList.ExecutablesInMRUOrder.Last());

            //Test removal without persisting to disk
            mruList.MakeExecutableMostRecentlyUsed(executable3);
            Assert.IsTrue(mruList.RemoveExecutableFromRecentlyUsedList(executable2), "Executable should have been in the list already");
            Assert.IsTrue(mruList.RemoveExecutableFromRecentlyUsedList(executable3), "Executable should have been in the list already");

            //Now test with disk persistance
            mruList.ReloadAllEntries();
            mruList.MakeExecutableMostRecentlyUsed(executable3);
            Assert.IsTrue(mruList.RemoveExecutableFromRecentlyUsedList(executable2), "Executable should have been in the list already");
            Assert.IsTrue(mruList.RemoveExecutableFromRecentlyUsedList(executable3), "Executable should have been in the list already");
            mruList.MakeExecutableMostRecentlyUsed(executable4);
            mruList.SaveChanges();
            mruList.ReloadAllEntries();
            Assert.HasCount(2, mruList.ExecutablesInMRUOrder);
            Assert.AreEqual(executable4, mruList.ExecutablesInMRUOrder.First());
            Assert.AreEqual(executable1, mruList.ExecutablesInMRUOrder.Last());
        }

        [TestMethod]
        public void AdministratorRightsTest()
        {
            Assert.IsInstanceOfType(_registry, typeof(MockRegistry));
            var mockRegistry = (MockRegistry)_registry;

            //Simulate Windows registry by requiring admin rights and not running as admin
            mockRegistry.RequireAdministratorRights(true);
            mockRegistry.SetCurrentUserAsAdmin(false);

            //Make sure we can access properties and call methods without admin rights
            using var mruList = GetMRUListInstance();
            mruList.AssociateProgramId(DUMMY_PROGRAM_ID, false);
            Assert.IsNotEmpty(mruList.ExecutableProgramIds);
            mruList.MakeExecutableMostRecentlyUsed(Path.GetFileName(_testFileAssociator.PathToExecutable));
            Assert.IsNotEmpty(mruList.ExecutablesInMRUOrder);

            //Saving should require admin rights
            Assert.Throws<NotRunningAsAdministratorException>(() => { mruList.SaveChanges(); });

            //Creating a program id requires admin rights
            Assert.Throws<NotRunningAsAdministratorException>(() => { _testFileAssociator.DefineProgramId(_testFileExtension, "dummy command"); });

            //This shouldn't require admin rights
            Assert.IsFalse(_testFileAssociator.IsFileAssociationSet(_testFileExtension), "File association shouldn't exist");

            //Setting and Removing should also require admin rights
            Assert.Throws<NotRunningAsAdministratorException>(() => { _testFileAssociator.SetFileAssociation(_testFileExtension); });
            Assert.Throws<NotRunningAsAdministratorException>(() => { _testFileAssociator.RemoveFileAssociation(_testFileExtension); });
        }
    }
}
