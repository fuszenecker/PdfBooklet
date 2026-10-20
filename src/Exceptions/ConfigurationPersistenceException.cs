using System;

namespace PdfBooklet.Exceptions;

public sealed class ConfigurationPersistenceException : Exception
{
    public ConfigurationPersistenceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
