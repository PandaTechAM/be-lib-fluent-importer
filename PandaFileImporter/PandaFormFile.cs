using Microsoft.AspNetCore.Http;

namespace PandaFileImporter
{
    internal class PandaFormFile : IFormFile
    {
        private readonly Stream _stream;

        public PandaFormFile(Stream stream, string fileName, string contentType)
        {
            _stream = stream;
            FileName = fileName;
            ContentType = contentType;
        }

        public string ContentType { get; }

        public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{FileName}\"";

        public IHeaderDictionary Headers => new PandaHeaderDictionary();

        public long Length => _stream.Length;

        public string Name => "file";

        public string FileName { get; }

    public void CopyTo(Stream target) => _stream.CopyTo(target);

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
            => _stream.CopyToAsync(target, 81920, cancellationToken);

        public Stream OpenReadStream() => _stream;

        public Task<byte[]> ReadAllBytesAsync(CancellationToken cancellationToken = default)
        {
            using (var memoryStream = new MemoryStream())
            {
                _stream.CopyTo(memoryStream);
                return Task.FromResult(memoryStream.ToArray());
            }
        }
    }
}
