using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using NPOI.SS.Formula.Functions;
using PandaFileImporter;
using System.Text;

namespace PandaFileImporterTests
{
    public class ServiceTests
    {
        private readonly FileImporter _fileImporter;

        public ServiceTests()
        {
            _fileImporter = new FileImporter();
        }

        private List<FileData> GetDataList()
        {
            var list = new List<FileData>();

            for (int i = 0; i < 10; i++)
            {
                list.Add(new FileData
                {
                    Id = i,
                    Name = $"Test {i}",
                    Date = DateTime.Now,
                    Description = $"Description {i}",
                });
            }

            return list;
        }

        /// <summary>
        /// Generate IFormFile and use it for test cases.
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        /// <param name="mimeType">MimeTypes static class members CSV/XLSX.</param>
        /// <returns></returns>
        private IFormFile GenerateFormFile(string filePath, string mimeType = "application/octet-stream")
        {
            //var extension = Path.GetExtension(filePath);

            // Read the local file into a byte array
            byte[] fileBytes = File.ReadAllBytes(filePath);

            // Create a stream from the byte array
            MemoryStream stream = new MemoryStream(fileBytes);

            // Create an instance of FormFile using the stream
            var formFile = new FormFile(stream, 0, stream.Length, null, Path.GetFileName(filePath))
            {
                Headers = new HeaderDictionary(),
                ContentType = mimeType // Set the content type as needed
            };
            return formFile;
        }

        [Fact]
        public void Get_File_Bytes()
        {
            byte[] filebytes = Encoding.UTF8.GetBytes("dummy data");
            IFormFile file = new FormFile(new MemoryStream(filebytes), 0, filebytes.Length, "Data", "import.xlsx");

            var bytesDto = _fileImporter.GetFileBytes(file);

            Assert.Equal(filebytes.Length, bytesDto.FileContent.Length);
            Assert.True(filebytes.SequenceEqual(bytesDto.FileContent));
        }

        [Fact]
        public void Import_File()
        {
            var xlsxFile = GenerateFormFile("FileData.xlsx");

            var xlsxResult = _fileImporter.GetData<FileData>(xlsxFile);

            Assert.True(xlsxResult.Count() != 0);
            Assert.Equal(typeof(FileData), xlsxResult.First().GetType());



            // todo: add csv support ???

            //var csvFile = GenerateFormFile("FileData.csv");

            //var csvResult = _fileImporter.GetData<FileData>(csvFile);

            //Assert.True(csvResult.Count() != 0);
            //Assert.Equal(typeof(FileData), csvResult.First().GetType());
        }
    }
}