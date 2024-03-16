using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ClosedXML.Excel;
using CsvHelper;
using FileImporter.Enums;
using FileImporter.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FileImporter;

public class ImportRule<TModel> where TModel : class
{
    public class PropertyRule<TProperty> : IPropertyRule
    {
        private readonly string _propertyName;
        private string _columnName;
        private ConverterType _converterType = ConverterType.None;
        private ReadFromType _readFromType = ReadFromType.None;
        private TProperty _readValue = default!;
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

        public PropertyRule<TProperty> Custom(Func<string, TProperty> func)
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

        public TProperty GetValue(string value, TModel model)
        {
            string innerValue;
            switch (_readFromType)
            {
                case ReadFromType.None:
                case ReadFromType.Column:
                    innerValue = value;
                    break;
                case ReadFromType.Value:
                    return _readValue;
                case ReadFromType.Function:
                    return _readFromModel.Invoke(model);
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return _converterType switch
            {
                ConverterType.None => (TProperty)System.Convert.ChangeType(innerValue, typeof(TProperty)),
                ConverterType.Converter => _converter(innerValue),
                ConverterType.ConverterWithInstance => _converterWithInstance(innerValue, model),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public string PropertyName()
        {
            return _propertyName;
        }

        public string ColumnName()
        {
            return _columnName;
        }


        public void WriteValue(TProperty value)
        {
            _readFromType = ReadFromType.Value;
            _readValue = value;
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

    public async Task ImportAsync(DbContext context, List<Dictionary<string, string>> data)
    {
        var dbSet = context.Set<TModel>();

        foreach (var dataRow in data)
        {
            await ImportLine(dataRow, dbSet);
        }

        await context.SaveChangesAsync();
    }

    public async Task ImportCsvAsync(DbContext context, string csvFilePath)
    {
        var dbSet = context.Set<TModel>();

        using (var reader = new StreamReader(csvFilePath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<Dictionary<string, string>>();
            foreach (var record in records)
            {
                await ImportLine(record, dbSet);
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task ImportExcelAsync(DbContext context, byte[] excel)
    {
        var dbSet = context.Set<TModel>();

        var stream = new MemoryStream(excel);

        var data = new XLWorkbook(stream).Worksheets.First().Rows()
            .Select(x => x.Cells().Select(y => y.Value.ToString()).ToArray()).ToList();

        stream.Close();

        var headers = data[0];

        foreach (var dataRow in data.Skip(1))
        {
            var dict = new Dictionary<string, string>();
            for (var i = 0; i < headers.Length; i++)
            {
                dict.Add(headers[i], dataRow[i]);
            }

            await ImportLine(dict, dbSet);
        }

        await context.SaveChangesAsync();
    }


    private async Task ImportLine(IReadOnlyDictionary<string, string> dataRow, DbSet<TModel> dbSet)
    {
        var model = Activator.CreateInstance<TModel>();

        foreach (var rule in _rules)
        {
            var property = typeof(TModel).GetProperty(rule.PropertyName());

            if (property is null)
            {
                throw new ArgumentException("Invalid property name");
            }

            var value = dataRow[rule.ColumnName()];

            var convertMethod = rule.GetType().GetMethod("GetValue");

            var convertedValue = convertMethod?.Invoke(rule, [value, model]);

            property.SetValue(model, convertedValue);
        }

        await dbSet.AddAsync(model);
    }
}