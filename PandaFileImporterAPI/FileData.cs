using System.ComponentModel;

namespace PandaFileImporterAPI
{
    public class FileData
    {
        [DisplayName("ID")]
        public int Id { get; set; }
        [DisplayName("NAME test")]
        public string Name { get; set; } = null!;
        [DisplayName("DESCRIPTION test")]
        public string Description { get; set; } = null!;
        [DisplayName("DATE")]
        public DateTime Date { get; set; }
        [DisplayName("COMMENT test")]
        public string? Comment { get; set; }
    }
}
