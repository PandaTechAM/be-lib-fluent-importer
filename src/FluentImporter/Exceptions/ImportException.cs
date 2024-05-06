using System;

namespace FluentImporter.Exceptions;

public abstract class ImportException(string message, string? value = null)
    : Exception(message)
{
    public string? Value { get; private set; } = value;
}