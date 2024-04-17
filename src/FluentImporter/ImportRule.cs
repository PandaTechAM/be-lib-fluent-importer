using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using ClosedXML.Excel;
using CsvHelper;
using FluentImporter.Enums;
using FluentImporter.Services.Interfaces;

namespace FluentImporter;

public class ImportRule<TModel> where TModel : class
{
    public class PropertyRule<TProperty> : IPropertyRule
    {
        private readonly string _propertyName;
        private string _columnName;
        private ConverterType _converterType = ConverterType.None;
        private ReadFromType _readFromType = ReadFromType.None;
        private TProperty _readValue = default!;
        private TProperty _defaultValue = default!;
        private bool _isValueRequired;
        private Func<TModel, TProperty> _readFromModel = null!;

        public PropertyRule(MemberExpression navigationPropertyPath)
        {
            _propertyName = navigationPropertyPath.Member.Name ?? throw new ArgumentException("Invalid property name");
            _columnName = _propertyName;
        }

        private Func<string, TProperty> _converter = x => (TProperty)System.Convert.ChangeType(x, typeof(TProperty));

        private Func<string, TModel, TProperty> _converterWithInstance =
            (x, _) => (TProperty)System.Convert.ChangeType(x, typeof(TProperty));


        public PropertyRule<TProperty> ReadFromColumn(string name)
        {
            _columnName = name;
            _readFromType = ReadFromType.Column;
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
                throw new ArgumentException($"Value for column '{_columnName}' is required", value);
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
                    throw new ArgumentOutOfRangeException();
            }

            return _converterType switch
            {
                ConverterType.None => (TProperty?)System.Convert.ChangeType(innerValue, typeof(TProperty)) ??
                                      _defaultValue,
                ConverterType.Converter => _converter(innerValue!) ?? _defaultValue,
                ConverterType.ConverterWithInstance => _converterWithInstance(innerValue!, model) ?? _defaultValue,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public string PropertyName() => _propertyName;

        public string ColumnName() => _columnName;

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
    }

    private readonly List<IPropertyRule> _rules = [];

    protected PropertyRule<TProperty> RuleFor<TProperty>(Expression<Func<TModel, TProperty>> navigationPropertyPath)
    {
        var rule = new PropertyRule<TProperty>(navigationPropertyPath.Body as MemberExpression ??
                                               throw new ArgumentException("Invalid property name"));

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
        var data = new XLWorkbook(stream).Worksheets
            .First()
            .Rows()
            .Select(x => x.Cells()
                .Select(y => y.Value.ToString())
                .ToArray())
            .ToList();

        var models = new List<TModel>(data.Count);
        var headers = data[0];

        foreach (var dataRow in data.Skip(1))
        {
            var dict = new Dictionary<string, string>();
            for (var i = 0; i < headers.Length; i++)
            {
                dict.Add(headers[i], i < dataRow.Length ? dataRow[i] : default!);
            }

            models.Add(GetRecord(dict));
        }

        return models;
    }

    private List<TModel> ReadCsv(StreamReader reader)
    {
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<object>();
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
                           throw new ArgumentException("Invalid property name");

            var value = dataRow[rule.ColumnName()];

            var convertMethod = rule.GetType().GetMethod("GetValue");

            var convertedValue = convertMethod?.Invoke(rule, [value, model]);

            property.SetValue(model, convertedValue);
        }

        return model;
    }
}