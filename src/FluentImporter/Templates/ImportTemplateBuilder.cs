using System.Globalization;
using ClosedXML.Excel;
using FluentImporter.Metadata;

namespace FluentImporter.Templates;

/// <summary>
///     Renders a two-sheet xlsx template from column metadata: a fillable data sheet first, a legend second.
/// </summary>
internal static class ImportTemplateBuilder
{
    private const int LegendColumnTableColumns = 5;

    // Sits clear of the A-E column-reference table so hiding it cannot hide part of the legend.
    private const int DropdownSourceColumn = 7;

    private static readonly XLColor HeaderFill = XLColor.FromArgb(0x1F, 0x3B, 0x57);
    private static readonly XLColor RequiredHeaderFill = XLColor.FromArgb(0x2E, 0x5C, 0x8A);
    private static readonly XLColor SectionFill = XLColor.FromArgb(0xE8, 0xED, 0xF2);
    private static readonly char[] InvalidSheetNameChars = [':', '\\', '/', '?', '*', '[', ']'];

    internal static byte[] Build(
        IReadOnlyList<IImportColumn> columns,
        IReadOnlyList<object?[]> exampleRows,
        ImportTemplateOptions options)
    {
        var valueSets = ResolveValueSets(columns, options);

        using var workbook = new XLWorkbook();

        // Order matters: the data sheet is written first so it is sheet 1, which is both the tab Excel opens on and
        // the sheet the reader picks when no name is given.
        var dataSheet = workbook.Worksheets.Add(SanitizeSheetName(options.DataSheetName, "Data"));
        var legendSheet = workbook.Worksheets.Add(SanitizeSheetName(options.LegendSheetName, "Legend"));

        var dropdownRanges = WriteLegendSheet(legendSheet, columns, valueSets, options);
        WriteDataSheet(dataSheet, columns, exampleRows, valueSets, options);
        ApplyDropdowns(dataSheet, columns, dropdownRanges, exampleRows.Count, options);

        legendSheet.TabSelected = false;
        dataSheet.SetTabActive();
        dataSheet.SetTabSelected();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static Dictionary<string, List<AllowedValue>> ResolveValueSets(
        IReadOnlyList<IImportColumn> columns,
        ImportTemplateOptions options)
    {
        var valueSetByProperty = new Dictionary<string, List<AllowedValue>>(StringComparer.Ordinal);

        foreach (var column in columns)
        {
            if (column.AllowedValues.Count > 0)
            {
                valueSetByProperty[column.PropertyName] = [..column.AllowedValues];
                continue;
            }

            if (column.EnumSource is null)
            {
                continue;
            }

            var values = new List<AllowedValue>();
            foreach (var member in Enum.GetValues(column.EnumSource))
            {
                var name = Enum.GetName(column.EnumSource, member) ?? member.ToString() ?? string.Empty;
                var numeric = Convert.ToInt64(member, CultureInfo.InvariantCulture);
                var label = options.EnumLabelResolver?.Invoke(column.EnumSource, name, numeric);

                values.Add(new AllowedValue(
                    numeric.ToString(CultureInfo.InvariantCulture),
                    string.IsNullOrWhiteSpace(label) ? name : label));
            }

            valueSetByProperty[column.PropertyName] = values;
        }

        return valueSetByProperty;
    }

    private static void WriteDataSheet(
        IXLWorksheet sheet,
        IReadOnlyList<IImportColumn> columns,
        IReadOnlyList<object?[]> exampleRows,
        Dictionary<string, List<AllowedValue>> valueSets,
        ImportTemplateOptions options)
    {
        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            var cell = sheet.Cell(1, i + 1);

            // The header text is the parsing contract, so it is written verbatim and never localized.
            cell.Value = column.ColumnName;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = column.IsRequired ? RequiredHeaderFill : HeaderFill;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var note = BuildHeaderNote(column, options);
            if (note.Length > 0)
            {
                cell.CreateComment().AddText(note);
            }

            var format = ResolveNumberFormat(column, valueSets);
            if (format is not null)
            {
                sheet.Column(i + 1).Style.NumberFormat.Format = format;
            }
        }

        for (var r = 0; r < exampleRows.Count; r++)
        {
            var values = exampleRows[r];
            for (var c = 0; c < columns.Count && c < values.Length; c++)
            {
                WriteExampleCell(sheet.Cell(r + 2, c + 1), values[c], columns[c], valueSets);
            }
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Row(1).Height = 22;

        var usedRows = Math.Max(exampleRows.Count, 1);
        sheet.Range(1, 1, usedRows + 1, columns.Count).SetAutoFilter();

        for (var i = 1; i <= columns.Count; i++)
        {
            sheet.Column(i).AdjustToContents(1, usedRows + 1, 12d, 40d);
        }
    }

    private static Dictionary<string, IXLRange> WriteLegendSheet(
        IXLWorksheet sheet,
        IReadOnlyList<IImportColumn> columns,
        Dictionary<string, List<AllowedValue>> valueSets,
        ImportTemplateOptions options)
    {
        var texts = options.Texts;

        sheet.Cell(1, 1).Value = texts.LegendTitle;
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Range(1, 1, 1, LegendColumnTableColumns).Merge();

        var row = 3;
        sheet.Cell(row, 1).Value = texts.ColumnsTitle;
        sheet.Cell(row, 1).Style.Font.Bold = true;
        row++;

        string[] headers =
        [
            texts.ColumnHeader, texts.RequiredHeader, texts.TypeHeader, texts.FormatHeader, texts.DescriptionHeader
        ];

        for (var i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = SectionFill;
        }

        row++;

        foreach (var column in columns)
        {
            sheet.Cell(row, 1).Value = column.ColumnName;
            sheet.Cell(row, 2).Value = column.IsRequired ? texts.RequiredYes : texts.RequiredNo;
            sheet.Cell(row, 3).Value = ResolveTypeName(column, valueSets, texts);
            sheet.Cell(row, 4).Value = column.ExpectedFormat ?? string.Empty;
            sheet.Cell(row, 5).Value = ResolveDescription(column, options) ?? string.Empty;
            sheet.Cell(row, 5).Style.Alignment.WrapText = true;
            row++;
        }

        var dropdownRangeByProperty = new Dictionary<string, IXLRange>(StringComparer.Ordinal);
        var columnsWithValues = columns.Where(c => valueSets.ContainsKey(c.PropertyName)).ToList();

        if (columnsWithValues.Count > 0)
        {
            row += 2;
            sheet.Cell(row, 1).Value = texts.AllowedValuesTitle;
            sheet.Cell(row, 1).Style.Font.Bold = true;
            row += 2;

            foreach (var column in columnsWithValues)
            {
                var values = valueSets[column.PropertyName];

                sheet.Cell(row, 1).Value = column.ColumnName;
                sheet.Cell(row, 1).Style.Font.Bold = true;
                sheet.Cell(row, 1).Style.Fill.BackgroundColor = SectionFill;
                sheet.Cell(row, 2).Style.Fill.BackgroundColor = SectionFill;
                row++;

                sheet.Cell(row, 1).Value = texts.ValueHeader;
                sheet.Cell(row, 2).Value = texts.LabelHeader;
                sheet.Cell(row, 1).Style.Font.Italic = true;
                sheet.Cell(row, 2).Style.Font.Italic = true;
                row++;

                var firstValueRow = row;
                foreach (var value in values)
                {
                    sheet.Cell(row, 1).Value = value.Value;
                    sheet.Cell(row, 1).Style.NumberFormat.Format = "@";
                    sheet.Cell(row, 2).Value = value.Label;

                    // Hidden helper column: the dropdown source, so the visible legend stays a clean two-column table
                    // while the operator still picks "0 - Label".
                    sheet.Cell(row, DropdownSourceColumn).Value = value.DropdownText;
                    row++;
                }

                if (values.Count > 0)
                {
                    dropdownRangeByProperty[column.PropertyName] =
                        sheet.Range(firstValueRow, DropdownSourceColumn, row - 1, DropdownSourceColumn);
                }

                row++;
            }
        }

        sheet.Column(1).AdjustToContents(10d, 32d);
        sheet.Column(2).AdjustToContents(10d, 45d);
        sheet.Column(3).AdjustToContents(10d, 20d);
        sheet.Column(4).Width = 18;
        sheet.Column(5).Width = 60;

        if (dropdownRangeByProperty.Count > 0)
        {
            sheet.Column(DropdownSourceColumn).Hide();
        }

        return dropdownRangeByProperty;
    }

    private static void ApplyDropdowns(
        IXLWorksheet dataSheet,
        IReadOnlyList<IImportColumn> columns,
        Dictionary<string, IXLRange> dropdownRanges,
        int exampleRowCount,
        ImportTemplateOptions options)
    {
        if (!options.AddDropdowns || dropdownRanges.Count == 0)
        {
            return;
        }

        var lastRow = Math.Max(exampleRowCount, 1) + 1 + Math.Max(options.BlankRowsForInput, 0);

        for (var i = 0; i < columns.Count; i++)
        {
            if (!dropdownRanges.TryGetValue(columns[i].PropertyName, out var source))
            {
                continue;
            }

            var validation = dataSheet.Range(2, i + 1, lastRow, i + 1).CreateDataValidation();
            validation.List(source, true);
            validation.IgnoreBlanks = true;
            validation.InCellDropdown = true;

            // Warning rather than Stop: a hard stop would fight bulk paste, which is how large files get filled.
            validation.ErrorStyle = XLErrorStyle.Warning;
            validation.ShowErrorMessage = true;
            validation.ErrorTitle = options.Texts.InvalidValueTitle;
            validation.ErrorMessage = options.Texts.InvalidValueMessage;
        }
    }

    private static void WriteExampleCell(
        IXLCell cell,
        object? value,
        IImportColumn column,
        Dictionary<string, List<AllowedValue>> valueSets)
    {
        if (value is null)
        {
            return;
        }

        if (valueSets.TryGetValue(column.PropertyName, out var values))
        {
            var raw = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            var match = values.FirstOrDefault(v => string.Equals(v.Value, raw, StringComparison.OrdinalIgnoreCase));
            cell.Value = match?.DropdownText ?? raw;
            return;
        }

        if (column.ExpectedFormat is { Length: > 0 } format && value is DateTime formattable)
        {
            cell.Value = formattable.ToString(format, CultureInfo.InvariantCulture);
            return;
        }

        cell.Value = value switch
        {
            string s => s,
            bool b => b,
            DateTime d => d,
            DateOnly d => d.ToDateTime(TimeOnly.MinValue),
            TimeSpan t => t,
            sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal =>
                Convert.ToDouble(value, CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    private static string BuildHeaderNote(IImportColumn column, ImportTemplateOptions options)
    {
        var parts = new List<string>(3);

        if (column.IsRequired)
        {
            parts.Add(options.Texts.RequiredNote);
        }

        var description = ResolveDescription(column, options);
        if (!string.IsNullOrWhiteSpace(description))
        {
            parts.Add(description);
        }

        if (!string.IsNullOrWhiteSpace(column.ExpectedFormat))
        {
            parts.Add(column.ExpectedFormat);
        }

        return string.Join(Environment.NewLine, parts);
    }

    private static string? ResolveDescription(IImportColumn column, ImportTemplateOptions options)
    {
        var resolved = options.ColumnDescriptionResolver?.Invoke(column);
        return string.IsNullOrWhiteSpace(resolved) ? column.Description : resolved;
    }

    private static string ResolveTypeName(
        IImportColumn column,
        Dictionary<string, List<AllowedValue>> valueSets,
        ImportTemplateTexts texts)
    {
        if (valueSets.ContainsKey(column.PropertyName))
        {
            return texts.ListType;
        }

        if (column.ExpectedFormat is { Length: > 0 })
        {
            return texts.TextType;
        }

        var type = column.PropertyType;

        if (type == typeof(bool))
        {
            return texts.BooleanType;
        }

        if (type == typeof(DateTime) || type == typeof(DateOnly) || type == typeof(DateTimeOffset))
        {
            return texts.DateType;
        }

        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
        {
            return texts.DecimalNumberType;
        }

        if (type == typeof(sbyte) || type == typeof(byte) || type == typeof(short) || type == typeof(ushort)
            || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong))
        {
            return texts.WholeNumberType;
        }

        return type.IsEnum ? texts.ListType : texts.TextType;
    }

    private static string? ResolveNumberFormat(
        IImportColumn column,
        Dictionary<string, List<AllowedValue>> valueSets)
    {
        // A value-set column carries "0 - Label" text, and a column with a declared format carries text in that format.
        // Both must be Text so Excel does not coerce them.
        if (valueSets.ContainsKey(column.PropertyName) || column.ExpectedFormat is { Length: > 0 })
        {
            return "@";
        }

        var type = column.PropertyType;

        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
        {
            return "0.00";
        }

        if (type == typeof(sbyte) || type == typeof(byte) || type == typeof(short) || type == typeof(ushort)
            || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong))
        {
            return "0";
        }

        if (type == typeof(DateTime) || type == typeof(DateOnly) || type == typeof(DateTimeOffset))
        {
            return "yyyy-mm-dd";
        }

        // Text columns are pinned to Text so ids and account numbers keep leading zeros and never go scientific.
        return type == typeof(string) ? "@" : null;
    }

    private static string SanitizeSheetName(string? name, string fallback)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return fallback;
        }

        var cleaned = name.Trim();
        foreach (var invalid in InvalidSheetNameChars)
        {
            cleaned = cleaned.Replace(invalid, ' ');
        }

        cleaned = cleaned.Trim('\'', ' ');

        if (cleaned.Length == 0)
        {
            return fallback;
        }

        return cleaned.Length <= 31 ? cleaned : cleaned[..31];
    }
}
