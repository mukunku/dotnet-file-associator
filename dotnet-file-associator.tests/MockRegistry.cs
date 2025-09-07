using DotnetFileAssociator;
using System.Text.Json.Nodes;

namespace dotnet_file_associator.tests
{
    internal class MockRegistry : IRegistry
    {
        private JsonObject _rootStorage;

        private MockRegistry? _classesRootRegistry;
        public IRegistry GetClassesRootRegistry => _classesRootRegistry ??= new MockRegistry(new JsonObject());

        private MockRegistry? _currentUserRegistry;
        public IRegistry GetCurrentUserRegistry => _currentUserRegistry ??= new MockRegistry(new JsonObject());

        public bool RequiresAdministratorPrivileges => false;

        public MockRegistry() : this(new JsonObject())
        {
            
        }

        public MockRegistry(JsonObject rootStorage)
        {
            if (rootStorage is null)
                throw new ArgumentNullException(nameof(rootStorage));

            _rootStorage = rootStorage;
        }

        public IRegistry CreateSubKey(string key)
        {
            if (!_rootStorage.ContainsKey(key))
                _rootStorage.Add(key, new JsonObject());

            if (_rootStorage[key] is not JsonObject subKey)
                throw new InvalidDataException("Something went wrong while mocking the registry");

            return new MockRegistry(subKey);
        }

        public void DeleteSubKeyTree(string key)
        {
            _rootStorage.Remove(key);
        }

        public void Dispose()
        {
            //Nothing to dispose
        }

        public IEnumerable<string> GetValueNames()
        {
            foreach (var property in _rootStorage)
            {
                if (property.Value is not JsonObject)
                    yield return property.Key;
            }
        }

        public object? GetValue(string? name)
        {
            _rootStorage.TryGetPropertyValue(name ?? "null", out var jsonNode);
            return jsonNode?.AsValue().GetValue<object?>();
        }

        public void SetValue(string? name, object value)
        {
            name ??= "null";
            if (_rootStorage.ContainsKey(name))
                _rootStorage[name] = JsonValue.Create(value);
            else
                _rootStorage.Add(name, JsonValue.Create(value));
        }

        public void SetString(string? name, string value)
            => SetValue(name, value);

        public void DeleteValue(string name)
        {
            if (_rootStorage.TryGetPropertyValue(name, out var jsonNode))
            {
                if (jsonNode is JsonObject)
                    throw new InvalidOperationException($"{name} is not a value property");

                _rootStorage.Remove(name);
            }
        }

        public IRegistry? OpenSubKey(string key)
            => _rootStorage.TryGetPropertyValue(key, out var jsonNode)
            && jsonNode is JsonObject jsonObject ?
                new MockRegistry(jsonObject) : null;
    }
}
