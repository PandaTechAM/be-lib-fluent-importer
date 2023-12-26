using System.Linq.Expressions;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace PandaFileImporter;

public class ImportRule<TModel> where TModel : class
{
    public class PropertyRule<TModel, TProperty> : IInterface
    {
        string _propertyName;
        private string _columnName = "";
        Func<string, TProperty> _converter = x => (TProperty)System.Convert.ChangeType(x, typeof(TProperty));

        public PropertyRule(MemberExpression navigationPropertyPath)
        {
            _propertyName = navigationPropertyPath.Member.Name ?? throw new ArgumentException("Invalid property name");
        }

        public PropertyRule<TModel, TProperty> ReadFromColumn(string name)
        {
            _columnName = name;
            return this;
        }

        public PropertyRule<TModel, TProperty> Convert(Func<string, TProperty> func)
        {
            _converter = func;
            return this;
        }
        
        public TProperty GetValue(string value)
        {
            return _converter(value);
        }

        public string PropertyName()
        {
            return _propertyName;
        }
        
        public string ColumnName()
        {
            return _columnName;
        }
    }

    public PropertyRule<TModel, TProperty> RuleFor<TProperty>(
        Expression<Func<TModel, TProperty>> navigationPropertyPath)
    {
        var rule = new PropertyRule<TModel, TProperty>(navigationPropertyPath.Body as MemberExpression ?? throw new ArgumentException("Invalid property name"));

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
        
        var data = new XLWorkbook(stream).Worksheets.First().Rows().Select(x => x.Cells().Select(y => y.Value.ToString()).ToArray()).ToList();

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
                
            var convertedValue = convertMethod?.Invoke(rule, new object[] {value});
                
            property.SetValue(model, convertedValue);
        }

        await dbSet.AddAsync(model);
    }
}