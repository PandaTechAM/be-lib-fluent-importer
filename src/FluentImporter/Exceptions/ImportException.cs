using System;

namespace FluentImporter.Exceptions;

public abstract class ImportException(string message, string? messageDetails = null)
   : Exception(message)
{
   public string? MessageDetails { get; private set; } = messageDetails;
}