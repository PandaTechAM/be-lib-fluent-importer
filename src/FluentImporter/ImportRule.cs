using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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
      var rule = new PropertyRule<TProperty>(navigationPropertyPath.Body as MemberExpression ??
                                             throw new InvalidPropertyNameException("Invalid property name",
                                                string.Empty));

      _rules.Add(rule);

      return rule;
   }

   public IEnumerable<TModel> GetRecords(IEnumerable<Dictionary<string, string>> data)
   {
      foreach (var dataRow in data)
      {
         yield return GetRecord(dataRow);
      }
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
      using var stream = File.Open(xlsxFilePath, FileMode.Open);
      return ReadXlsx(stream);
   }

   public List<TModel> ReadXlsx(Stream stream)
   {
      var worksheet = new XLWorkbook(stream).Worksheets.First();
      var rowCount = worksheet.RowsUsed()
                              .Count();
      var columnCount = rowCount > 0
         ? worksheet.Rows()
                    .Max(row => row.CellsUsed()
                                   .Count())
         : 0;

      var data = Enumerable.Range(1, rowCount)
                           .Select(rowIndex =>
                           {
                              var row = worksheet.Row(rowIndex);
                              return Enumerable.Range(1, columnCount)
                                               .Select(colIndex =>
                                               {
                                                  var cell = row.Cell(colIndex);
                                                  return cell.IsEmpty() ? null : cell.Value.ToString();
                                               })
                                               .ToArray();
                           })
                           .ToList();

      CheckForEmptyFile(data);

      var models = new List<TModel>(data.Count);
      var headers = data[0]
                    .Select(x => x?.ToLower())
                    .ToArray();

      foreach (var dataRow in data.Skip(1))
      {
         var dict = new Dictionary<string, string>();
         for (var i = 0; i < headers.Length; i++)
         {
            dict.Add(headers[i]!, i < dataRow.Length ? dataRow[i]! : default!);
         }

         models.Add(GetRecord(dict));
      }

      return models;
   }

   private List<TModel> ReadCsv(StreamReader reader)
   {
      using var csv = new CsvReader(reader,
         new CsvConfiguration(CultureInfo.InvariantCulture)
         {
            HasHeaderRecord = true,
            PrepareHeaderForMatch = args => args.Header.ToLower()
         });

      var records = csv.GetRecords<object>()
                       .ToList();

      CheckForEmptyFile(records);

      var models = new List<TModel>();

      foreach (var record in records)
      {
         var recordMapped = (record as IDictionary<string, object>)!
                            .ToDictionary(x => x.Key,
                               x => (string)x.Value == string.Empty ? null : (string)x.Value)
                            .AsReadOnly();

         models.Add(GetRecord(recordMapped!));
      }

      return models;
   }

   private TModel GetRecord(IReadOnlyDictionary<string, string> dataRow)
   {
      var model = Activator.CreateInstance<TModel>();

      foreach (var rule in _rules)
      {
         var property = typeof(TModel).GetProperty(rule.PropertyName()) ??
                        throw new InvalidPropertyNameException("Invalid property name", rule.ColumnName());
         try
         {
            var value = dataRow[rule.ColumnName()
                                    .ToLower()];

            var convertMethod = rule.GetType()
                                    .GetMethod("GetValue");

            var convertedValue = convertMethod?.Invoke(rule, [value, model]);

            property.SetValue(model, convertedValue);
         }
         catch
         {
            throw new InvalidCellValueException("Invalid cell value", rule.ColumnName());
         }
      }

      return model;
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


      private Func<string, TProperty> _converter = x => (TProperty)System.Convert.ChangeType(x, typeof(TProperty));
      private ConverterType _converterType = ConverterType.None;

      private Func<string, TModel, TProperty> _converterWithInstance =
         (x, _) => (TProperty)System.Convert.ChangeType(x, typeof(TProperty));

      private TProperty _defaultValue = default!;
      private bool _isValueRequired;
      private Func<TModel, TProperty> _readFromModel = null!;
      private ReadFromType _readFromType = ReadFromType.None;
      private TProperty _readValue = default!;
      private string _regex = ".*";

      public PropertyRule(MemberExpression navigationPropertyPath)
      {
         _propertyName = navigationPropertyPath.Member.Name ??
                         throw new InvalidPropertyNameException("Invalid property name", _propertyName);
         _columnName = _propertyName;
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
         _regex = regex;
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

         string? innerValue;
         switch (_readFromType)
         {
            case ReadFromType.None:
            case ReadFromType.Column:
               innerValue = value ?? _defaultValue?.ToString();
               break;
            case ReadFromType.Value:
               return _readValue;
            case ReadFromType.Function:
               return _readFromModel.Invoke(model) ?? _defaultValue;
            default:
               throw new ArgumentOutOfRangeException("", "Unknown read from type");
         }

         var regex = new Regex(_regex);
         if (innerValue is not null && !regex.IsMatch(innerValue))
         {
            throw new InvalidColumnValueException("Column value is not valid", $"{_columnName}: {value}");
         }

         var type = typeof(TProperty);
         if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
         {
            type = type.GenericTypeArguments.First();
         }

         return _converterType switch
         {
            ConverterType.None => innerValue == default
               ? _defaultValue
               : ChangeType(innerValue, type) ??
                 _defaultValue,
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
         if (type == typeof(bool) && int.TryParse(innerValue, out var result))
         {
            return (TProperty?)System.Convert.ChangeType(result, type);
         }

         return (TProperty?)System.Convert.ChangeType(innerValue, type);
      }
   }
}