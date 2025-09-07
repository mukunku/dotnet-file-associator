using System;
using System.Text.RegularExpressions;

namespace DotnetFileAssociator
{
    public struct FileExtensionDefinition
    {
        private const int MAX_ALLOWED_EXTENSION_LENGTH = 100; //Set some kind of reasonable upper limit
        private const int MAX_EXTENSION_LONG_NAME_LENGTH = 255; //Set some kind of reasonable upper limit

        private static readonly Regex WindowsFileExtensionValidationRegex
            = new(pattern: @"\.?[a-zA-Z0-9-_]+", RegexOptions.Compiled);

        /// <summary>
        /// File extension with the dot "." in front
        /// </summary>
        public string Extension { get; }

        /// <summary>
        /// Expanded name of the extension
        /// </summary>
        public string ExtensionLongName { get; }

        /// <summary>
        /// Defines a file extension.
        /// </summary>
        /// <param name="fileExtension">File extension with or without the dot "." in the beginning.</param>
        /// <param name="fileExtensionLongName">Optional expanded name of the extension.
        /// E.g. ".pdf" => "Portable Document Format file".</param>
        /// <exception cref="ArgumentNullException">Thrown if no file extension is provided.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the provided file extension is too long.</exception>
        /// <exception cref="ArgumentException">Thrown if the provided file extension contains illegal characters</exception>
        public FileExtensionDefinition(string fileExtension, string? fileExtensionLongName = null)
        {
            if (string.IsNullOrWhiteSpace(fileExtension))
                throw new ArgumentNullException(nameof(fileExtension), "Please provide a valid file extension.");

            if (fileExtension.Length > MAX_ALLOWED_EXTENSION_LENGTH)
                throw new ArgumentOutOfRangeException(nameof(fileExtension),
                    $"Provided file extension '{fileExtension}' is longer than the allowed maximum of {MAX_ALLOWED_EXTENSION_LENGTH} characters");

            if (!WindowsFileExtensionValidationRegex.IsMatch(fileExtension))
                throw new ArgumentException($"Invalid file extension: only letters, numbers, underscores '_', and dashes '-' are allowed.",
                    nameof(fileExtension));

            // We lowercase file extensions to standardize
            Extension = (!fileExtension.StartsWith(".", StringComparison.InvariantCultureIgnoreCase)
                ? string.Concat(".", fileExtension) : fileExtension).ToLowerInvariant();

            ExtensionLongName = (string.IsNullOrWhiteSpace(fileExtensionLongName) 
                ? $"{Extension.Replace(".", string.Empty).ToUpperInvariant()} File" : fileExtensionLongName!)
                .Left(MAX_EXTENSION_LONG_NAME_LENGTH);
        }
    }
}
