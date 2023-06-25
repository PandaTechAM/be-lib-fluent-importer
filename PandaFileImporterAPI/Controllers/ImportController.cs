using Microsoft.AspNetCore.Mvc;
using NPOI.SS.Formula.Functions;
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
        public ServiceResponse<byte[]> GetFileBytes(IFormFile file)
        {
            var response = new ServiceResponse<byte[]>();
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
                if(Path.GetExtension(file.FileName) != ".xlsx")
                {
                    response.Message = "Not supported file extenstion!";
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
    }
}
