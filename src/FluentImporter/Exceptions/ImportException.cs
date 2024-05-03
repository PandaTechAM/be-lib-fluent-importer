using System;

namespace FluentImporter.Exceptions;

public abstract class ImportException : ArgumentException
{
    protected ImportException(string message, string value) : base(message, value)
    {
    }

    protected ImportException()
    {
    }
}