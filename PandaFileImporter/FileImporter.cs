using DocumentFormat.OpenXml.Wordprocessing;
using Ganss.Excel;
using Microsoft.AspNetCore.Http;

namespace PandaFileImporter
{
    public class FileImporter
    {
        public byte[] GetFileBytes(IFormFile file)
        {
            using (var memoryStream = new MemoryStream())
            {
                // Read the file data into the MemoryStream
                file.CopyTo(memoryStream);

                // Reset the position of the MemoryStream to the beginning
                memoryStream.Position = 0;

                return memoryStream.ToArray();
            }
        }

        public IEnumerable<T> GetData<T>(IFormFile file)
        {
            using (var memoryStream = new MemoryStream())
            {
                // Read the file data into the MemoryStream
                file.CopyTo(memoryStream);

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
            return file;
        }
    }
}