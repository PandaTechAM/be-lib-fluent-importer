using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using FluentImporter.Enums;
using FluentImporter.Exceptions;
using FluentImporter.Services.Interfaces;

namespace FluentImporter;

public class ImportRule<TModel> where TModel : class
{
   private readonly List<IPropertyRule> _rules = [];

   protected PropertyRule<TProperty> RuleFor<TProperty>(Expression<Func<TModel, TProperty>> navigationPropertyPath)
   {
      if (navigationPropertyPath.Body is not MemberExpression me)
      {
         throw new InvalidPropertyNameException("Invalid property name", string.Empty);
      }

      var rule = new PropertyRule<TProperty>(me);
      _rules.Add(rule);
      return rule;
   }

   public IEnumerable<TModel> GetRecords(IEnumerable<Dictionary<string, string>> data)
   {
      return data.Select(row => GetRecord(row));
   }

   public List<TModel> ReadCsv(Stream csvStream)
   {
      csvStream.Position = 0;
      using var reader = new StreamReader(csvStream);
      return ReadCsv(reader);
   }

   public List<TModel> ReadCsv(string csvFilePath)
   {
      using var reader = new StreamReader(csvFilePath);
      return ReadCsv(reader);
   }

   public List<TModel> ReadXlsx(string xlsxFilePath)
   {
      using var stream = File.Open(xlsxFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      return ReadXlsx(stream);
   }

   public List<TModel> ReadXlsx(Stream stream)
   {
      using var workbook = new XLWorkbook(stream);
      var worksheet = workbook.Worksheets.FirstOrDefault();
      if (worksheet is null)
      {
         throw new EmptyFileImportException("Imported file is empty");
      }

      var firstRow = worksheet.FirstRowUsed();
      var lastRow = worksheet.LastRowUsed();
      if (firstRow is null || lastRow is null)
      {
         throw new EmptyFileImportException("Imported file is empty");
      }

      var headers = firstRow
                    .CellsUsed()
                    .Select(c => NormalizeHeader(c.GetString()))
                    .ToArray();

      var models = new List<TModel>(Math.Max(0, lastRow.RowNumber() - firstRow.RowNumber()));
      for (var r = firstRow.RowNumber() + 1; r <= lastRow.RowNumber(); r++)
      {
         var row = worksheet.Row(r);
         var dict = new Dictionary<string, string>(capacity: headers.Length);

         for (var i = 0; i < headers.Length; i++)
         {
            var header = headers[i];
            if (string.IsNullOrEmpty(header))
            {
               continue;
            }

            var cell = row.Cell(i + 1);
            var value = cell.IsEmpty() ? null : cell.Value.ToString();
            dict.Add(header, value!);
         }

         models.Add(GetRecord(dict, r));
      }

      CheckForEmptyFile(models);
      return models;
   }

   private static string? NormalizeHeader(string? s)
   {
      if (string.IsNullOrWhiteSpace(s))
      {
         return null;
      }

      return s.Trim()
              .ToLowerInvariant();
   }

   private List<TModel> ReadCsv(StreamReader reader)
   {
      using var csv = new CsvReader(reader,
         new CsvConfiguration(CultureInfo.InvariantCulture)
         {
            HasHeaderRecord = true,
            PrepareHeaderForMatch = args => args.Header
                                                .Trim()
                                                .ToLowerInvariant()
         });

      var records = csv.GetRecords<object>()
                       .ToList();
      CheckForEmptyFile(records);

      var models = new List<TModel>(records.Count);

      for (var i = 0; i < records.Count; i++)
      {
         var record = records[i];
         var dict = (record as IDictionary<string, object>)!
                    .ToDictionary(kv => kv.Key, kv => kv.Value.ToString())
                    .AsReadOnly();

         // header is row 1, first data row is 2
         models.Add(GetRecord(dict!, i + 2));
      }

      return models;
   }

   private TModel GetRecord(IReadOnlyDictionary<string, string> dataRow, int? rowIndex = null)
   {
      var model = Activator.CreateInstance<TModel>();

      foreach (var rule in _rules)
      {
         var column = rule.ColumnName();
         var propertyName = rule.PropertyName();

         var prop = typeof(TModel).GetProperty(propertyName);
         if (prop is null)
         {
            throw new InvalidPropertyNameException("Invalid property name", column);
         }

         string? raw = null;

         try
         {
            if (!TryGetHeaderValue(dataRow, column, out raw))
            {
               throw new InvalidColumnValueException("Column not found", column);
            }

            var converted = InvokeGetValue(rule, raw, model);

            SetPropertySmart(prop, model, converted);
         }
         catch (InvalidColumnValueException ice)
         {
            var rowInfo = rowIndex.HasValue ? $"row {rowIndex.Value}" : "row ?";
            var msg =
               $"Invalid cell value at {rowInfo}, column '{column}', property '{propertyName}', raw '{Truncate(raw, 256)}', target '{prop.PropertyType.Name}'. Error: {GetInnermostMessage(ice)}";
            throw new InvalidCellValueException(msg, column);
         }
         catch (Exception ex)
         {
            var rowInfo = rowIndex.HasValue ? $"row {rowIndex.Value}" : "row ?";
            var msg =
               $"Invalid cell value at {rowInfo}, column '{column}', property '{propertyName}', raw '{Truncate(raw, 256)}', target '{prop.PropertyType.Name}'. Error: {GetInnermostMessage(ex)}";
            throw new InvalidCellValueException(msg, column);
         }
      }

      return model;
   }

   private static object? InvokeGetValue(IPropertyRule rule, string? raw, TModel model)
   {
      var method = rule.GetType()
                       .GetMethod("GetValue", [typeof(string), typeof(TModel)]);
      if (method is null)
      {
         throw new MissingMethodException(rule.GetType()
                                              .FullName,
            "GetValue(string, TModel)");
      }

      try
      {
         return method.Invoke(rule, [raw, model]);
      }
      catch (TargetInvocationException tie)
      {
         throw tie.InnerException ?? tie;
      }
   }

   private static bool TryGetHeaderValue(IReadOnlyDictionary<string, string> row,
      string columnName,
      out string? value)
   {
      if (row.TryGetValue(columnName, out value!))
      {
         return true;
      }

      var lower = columnName.ToLowerInvariant();
      if (row.TryGetValue(lower, out value!))
      {
         return true;
      }

      var norm = NormalizeHeader(columnName);
      if (norm is not null && row.TryGetValue(norm, out value!))
      {
         return true;
      }

      value = null;
      return false;
   }

   private static void SetPropertySmart(PropertyInfo prop, object target, object? value)
   {
      var setter = prop.SetMethod;

      if (setter is not null && !IsInitOnly(prop))
      {
         prop.SetValue(target, value);
         return;
      }

      var backing = prop.DeclaringType!.GetField($"<{prop.Name}>k__BackingField",
         BindingFlags.Instance | BindingFlags.NonPublic);

      if (backing is null)
      {
         throw new InvalidOperationException($"Property '{prop.Name}' is not settable.");
      }

      backing.SetValue(target, value);
   }

   private static bool IsInitOnly(PropertyInfo prop)
   {
      var setter = prop.SetMethod;
      if (setter is null)
      {
         return false;
      }

      var mods = setter.ReturnParameter.GetRequiredCustomModifiers();
      return mods.Any(static m => m == typeof(System.Runtime.CompilerServices.IsExternalInit));
   }

   private static string Truncate(string? s, int max)
   {
      if (s is null)
      {
         return "null";
      }

      return s.Length <= max ? s : s.Substring(0, max) + "…";
   }

   private static string GetInnermostMessage(Exception ex)
   {
      while (ex.InnerException is not null)
      {
         ex = ex.InnerException;
      }

      return ex.Message;
   }

   private static void CheckForEmptyFile<T>(IEnumerable<T>? records)
   {
      if (records is null || !records.Any())
      {
         throw new EmptyFileImportException("Imported file is empty");
      }
   }

   public class PropertyRule<TProperty> : IPropertyRule
   {
      private readonly string _propertyName;
      private string _columnName;

      private Func<string, TProperty> _converter =
         x => (TProperty)System.Convert.ChangeType(x, typeof(TProperty), CultureInfo.InvariantCulture);

      private ConverterType _converterType = ConverterType.None;

      private Func<string, TModel, TProperty> _converterWithInstance =
         (x, _) => (TProperty)System.Convert.ChangeType(x, typeof(TProperty), CultureInfo.InvariantCulture);

      private TProperty _defaultValue = default!;
      private bool _isValueRequired;
      private Func<TModel, TProperty>? _readFromModel;
      private ReadFromType _readFromType = ReadFromType.None;
      private TProperty _readValue = default!;
      private string _regexPattern = ".*";
      private Regex? _regexCompiled;

      public PropertyRule(MemberExpression navigationPropertyPath)
      {
         _propertyName = navigationPropertyPath.Member.Name;
         _columnName = _propertyName ?? throw new InvalidPropertyNameException("Invalid property name", string.Empty);
      }

      public string PropertyName()
      {
         return _propertyName;
      }

      public string ColumnName()
      {
         return _columnName;
      }

      public PropertyRule<TProperty> ReadFromColumn(string name)
      {
         _columnName = name;
         _readFromType = ReadFromType.Column;
         return this;
      }

      public PropertyRule<TProperty> Validate(string regex)
      {
         _regexPattern = regex;
         _regexCompiled = new Regex(
            _regexPattern,
            RegexOptions.ExplicitCapture | RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(250));
         return this;
      }

      public PropertyRule<TProperty> Convert(Func<string, TProperty> func)
      {
         _converter = func;
         _converterType = ConverterType.Converter;
         return this;
      }

      public PropertyRule<TProperty> Convert(Func<string, TModel, TProperty> func)
      {
         _converterWithInstance = func;
         _converterType = ConverterType.ConverterWithInstance;
         return this;
      }

      public TProperty GetValue(string? value, TModel model)
      {
         if (_isValueRequired && string.IsNullOrWhiteSpace(value))
         {
            throw new InvalidColumnValueException("Column value is required", $"{_columnName}: {value}");
         }

         var innerValue = _readFromType switch
         {
            ReadFromType.None or ReadFromType.Column => value ?? _defaultValue?.ToString(),
            ReadFromType.Value => null,
            ReadFromType.Function => null,
            _ => throw new ArgumentOutOfRangeException("", "Unknown read from type")
         };

         switch (_readFromType)
         {
            case ReadFromType.Value:
               return _readValue;
            case ReadFromType.Function:
               return _readFromModel is null ? _defaultValue : _readFromModel.Invoke(model) ?? _defaultValue;
            case ReadFromType.None:
            case ReadFromType.Column:
               break;
            default:
               throw new ArgumentOutOfRangeException();
         }

         _regexCompiled ??= new Regex(
            _regexPattern,
            RegexOptions.ExplicitCapture | RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(250));

         if (innerValue is not null && !_regexCompiled.IsMatch(innerValue))
         {
            throw new InvalidColumnValueException("Column value is not valid", $"{_columnName}: {value}");
         }

         var targetType = typeof(TProperty);
         if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
         {
            targetType = targetType.GenericTypeArguments.First();
         }

         return _converterType switch
         {
            ConverterType.None => innerValue == null
               ? _defaultValue
               : ChangeType(innerValue, targetType) ?? _defaultValue,
            ConverterType.Converter => _converter(innerValue!) ?? _defaultValue,
            ConverterType.ConverterWithInstance => _converterWithInstance(innerValue!, model) ?? _defaultValue,
            _ => throw new ArgumentOutOfRangeException("", "Unknown converter type")
         };
      }

      public void WriteValue(TProperty value)
      {
         _readFromType = ReadFromType.Value;
         _readValue = value;
      }

      public PropertyRule<TProperty> Default(TProperty value)
      {
         _defaultValue = value;
         return this;
      }

      public PropertyRule<TProperty> NotEmpty()
      {
         _isValueRequired = true;
         return this;
      }

      public void ReadFromModel(Func<TModel, TProperty> func)
      {
         _readFromType = ReadFromType.Function;
         _readFromModel = func;
      }

      private static TProperty? ChangeType(string innerValue, Type type)
      {
         if (type.IsEnum)
         {
            return (TProperty?)Enum.Parse(type, innerValue, ignoreCase: true);
         }

         if (type != typeof(bool))
         {
            return (TProperty?)System.Convert.ChangeType(innerValue, type, CultureInfo.InvariantCulture);
         }

         if (bool.TryParse(innerValue, out var b))
         {
            return (TProperty?)(object)b;
         }

         if (int.TryParse(innerValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
         {
            return (TProperty?)(object)(i != 0);
         }

         if (string.Equals(innerValue, "yes", StringComparison.OrdinalIgnoreCase))
         {
            return (TProperty?)(object)true;
         }

         if (string.Equals(innerValue, "no", StringComparison.OrdinalIgnoreCase))
         {
            return (TProperty?)(object)false;
         }

         return (TProperty?)System.Convert.ChangeType(innerValue, type, CultureInfo.InvariantCulture);
      }
   }
}