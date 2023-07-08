namespace PandaFileImporter
{
    public class FileBytesDto
    {
        public string FileName { get; set; } = null!;
        public string FileExtension { get; set; } = null!;
        public byte[] FileContent { get; set; } = null!;
    }
}
