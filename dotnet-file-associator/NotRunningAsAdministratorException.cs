using System;

namespace DotnetFileAssociator
{
    public class NotRunningAsAdministratorException : Exception
    {
        public NotRunningAsAdministratorException(string? message = "This operation requires administrator privileges to run") : base(message)
        {
            
        }
    }
}
