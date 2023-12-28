using System.Linq.Expressions;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.EntityFrameworkCore;

namespace Pandatech.FileImporter;

public class ImportRule<TModel> where TModel : class
{
    public class PropertyRule<TProperty> : IInterface
    {
        private enum ConverterType
        {
            None,
            Converter,
            ConverterWithInstance
        }

        private enum ReadFromType
        {
            None,
            Column,
            Value,
            Function
        }

        readonly string _propertyName;
        private string _columnName;
        Func<string, TProperty> _converter = x => (TProperty)System.Convert.ChangeType(x, typeof(TProperty));

        Func<string, TModel, TProperty> _converterWithInstance =
            (x, y) => (TProperty)System.Convert.ChangeType(x, typeof(TProperty));


        private ConverterType _converterType = ConverterType.None;
        private ReadFromType _readFromType = ReadFromType.None;

        public PropertyRule(MemberExpression navigationPropertyPath)
        {
            _propertyName = navigationPropertyPath.Member.Name ?? throw new ArgumentException("Invalid property name");
            _columnName = _propertyName;
        }

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
                    return readValue;
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

        private TProperty readValue;

        public void ReadFromValue(TProperty value)
        {
            _readFromType = ReadFromType.Value;
            readValue = value;
        }

        Func<TModel, TProperty> _readFromModel;

        public void ReadFromModel(Func<TModel, TProperty> func)
        {
            _readFromType = ReadFromType.Function;
            _readFromModel = func;
        }
    }

    public PropertyRule<TProperty> RuleFor<TProperty>(
        Expression<Func<TModel, TProperty>> navigationPropertyPath)
    {
        var rule = new PropertyRule<TProperty>(navigationPropertyPath.Body as MemberExpression ??
                                               throw new ArgumentException("Invalid property name"));

        _rules.Add(rule);

        return rule;
    }

    private List<IInterface> _rules = new();

    private interface IInterface
    {
        public string PropertyName();
        string ColumnName();
    };

    public async Task ImportAsync(DbContext context, List<Dictionary<string, string>> data)
    {
        var dbSet = context.Set<TModel>();

        foreach (var dataRow in data)
        {
            await ImportLine(dataRow, dbSet);
        }

        await context.SaveChangesAsync();
    }

    public async Task ImportFromCsvAsync(DbContext context, string csv)
    {
        var dbSet = context.Set<TModel>();

        var data = csv.Split("\n").Select(x => x.Split(",")).ToList();

        var headers = data[0];

        foreach (var dataRow in data.Skip(1))
        {
            var dict = new Dictionary<string, string>();
            for (var i = 0; i < headers.Length; i++)
            {
                dict.Add(headers[i], Unescape(dataRow[i]));
            }

            await ImportLine(dict, dbSet);
        }

        await context.SaveChangesAsync();
    }

    public async Task ImportFromExcelAsync(DbContext context, byte[] excel)
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

    private static string Unescape(string value)
    {
        if (value.StartsWith("\"") && value.EndsWith("\""))
        {
            value = value.Substring(1, value.Length - 2); // Remove the surrounding double quotes
            value = value.Replace("\"\"", "\""); // Replace double double quotes with single quotes
            value = value.Replace("\\n", "\n"); // Replace escaped newlines with actual newlines
        }

        return value;
    }


    private async Task ImportLine(Dictionary<string, string> dataRow, DbSet<TModel> dbSet)
    {
        var model = Activator.CreateInstance<TModel>();

        foreach (var rule in _rules)
        {
            var property = typeof(TModel).GetProperty(rule.PropertyName());
            if (property == null)
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