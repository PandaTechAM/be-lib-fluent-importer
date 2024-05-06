using System;

namespace FluentImporter.Exceptions;

public abstract class ImportException : Exception
{
    protected ImportException(string message, string? value = null) : base(message)
    {
    }
}