using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using Ganss.Excel;
using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace PandaFileImporter
{
    public class FileImporter
    {
        public FileBytesDto GetFileBytes(IFormFile file)
        {
            using (var memoryStream = new MemoryStream())
            {
                // Read the file data into the MemoryStream
                file.CopyTo(memoryStream);

                // Reset the position of the MemoryStream to the beginning
                memoryStream.Position = 0;

                return new FileBytesDto
                {
                    FileName = Path.GetFileNameWithoutExtension(file.FileName),
                    FileExtension = Path.GetExtension(file.FileName),
                    FileContent = memoryStream.ToArray()
                };
            }
        }

        public IEnumerable<T> GetData<T>(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName);

            using (var memoryStream = new MemoryStream())
            {
                switch (extension)
                {
                    case ".xlsx":
                        // Read the file data into the MemoryStream
                        file.CopyTo(memoryStream);
                        break;

                    case ".csv":
                        // Conver CSV into XLSX
                        var xlsxFile = SaveAsXlsx(file);
                        // Read the file data into the MemoryStream
                        xlsxFile.CopyTo(memoryStream);
                        break;

                    default:
                        throw new NotSupportedException("File format not supported!");
                }

                // Reset the position of the MemoryStream to the beginning
                memoryStream.Position = 0;

                // Perform any processing on the MemoryStream
                using (var reader = new StreamReader(memoryStream))
                {
                    var fileContent = reader.ReadToEnd();

                    var data = new ExcelMapper(memoryStream).Fetch<T>();
                    return data;
                }
            }
        }

        public IFormFile SaveAsXlsx(IFormFile file)
        {
            using (var memoryStream = new MemoryStream())
            {
                // Copy the CSV file content to a memory stream
                //await file.CopyToAsync(memoryStream);
                file.CopyTo(memoryStream);
                memoryStream.Position = 0;

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Sheet1");

                    // Read the CSV data from the memory stream
                    using (var reader = new StreamReader(memoryStream))
                    {
                        int row = 1;
                        while (!reader.EndOfStream)
                        {
                            //string line = (await reader.ReadLineAsync())!;
                            string line = reader.ReadLine()!;
                            string[] values = line!.Split(',');

                            for (int col = 1; col <= values.Length; col++)
                            {
                                worksheet.Cell(row, col).Value = values[col - 1];
                            }

                            row++;
                        }
                    }

                    // Save the Excel workbook to a memory stream
                    var excelStream = new MemoryStream();
                    workbook.SaveAs(excelStream);
                    excelStream.Position = 0;

                    // Return new IFormFile in XLSX format as the response
                    return new PandaFormFile(excelStream, "import.xlsx", MimeTypes.XLSX.ToString());
                }
            }
        }

        public string GetExtensionMimeType(string fileExtension)
        {
            if (fileExtension.Substring(0, 1) == ".")
                fileExtension = fileExtension.ToUpper().Substring(1, fileExtension.Length - 1);

            Type constantsType = typeof(MimeTypes);
            FieldInfo[] fields = constantsType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            foreach (FieldInfo field in fields)
            {
                if (field.IsLiteral && !field.IsInitOnly)
                {
                    string constantValue = (string)field.GetRawConstantValue()!;

                    if (field.Name == fileExtension)
                    {
                        return constantValue;
                    }
                }
            }

            throw new Exception("No extension found for selected file!");
        }
    }
}