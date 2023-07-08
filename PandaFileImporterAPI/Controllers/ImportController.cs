using Microsoft.AspNetCore.Mvc;
using PandaFileImporter;
using PandaTech.ServiceResponse;

namespace PandaFileImporterAPI.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class ImportController : ExtendedController
    {
        public readonly FileImporter _fileImporter;

        public ImportController(FileImporter fileImporeter, IExceptionHandler exceptionHandler) : base(exceptionHandler)
        {
            _fileImporter = fileImporeter;
        }

        [HttpPost("get-bytes")]
        public ServiceResponse<FileBytesDto> GetFileBytes(IFormFile file)
        {
            var response = new ServiceResponse<FileBytesDto>();
            try
            {
                response.ResponseData.Data = _fileImporter.GetFileBytes(file);
            }
            catch (Exception e)
            {
                response = ExceptionHandler.Handle(response, e);
            }

            return SetResponse(response);
        }

        [HttpPost("import-file")]
        public ServiceResponse<IEnumerable<FileData>> ImportFile(IFormFile file)
        {
            var response = new ServiceResponse<IEnumerable<FileData>>();
            try
            {
                if (Path.GetExtension(file.FileName) != ".xlsx")
                {
                    response.Message = "Not supported file extension!";
                    response.ResponseStatus = ServiceResponseStatus.BadRequest;
                }

                response.ResponseData.Data = _fileImporter.GetData<FileData>(file);
            }
            catch (Exception e)
            {
                response = ExceptionHandler.Handle(response, e);
            }

            return SetResponse(response);
        }

        [HttpPost("download-file-from-bytes")]
        public IActionResult DownloadFile([FromBody] FileBytesDto file)
        {
            return File(file.FileContent, _fileImporter.GetExtensionMimeType(file.FileExtension), file.FileName + file.FileExtension);
        }
    }
}
