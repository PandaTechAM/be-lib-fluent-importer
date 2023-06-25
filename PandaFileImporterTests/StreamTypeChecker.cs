namespace PandaFileImporterTests
{
    public static class StreamTypeChecker
    {
        // Method to check if the stream is in OLE2 format
        public static bool IsStreamOLE2(Stream stream)
        {
            byte[] signature = new byte[8] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };

            byte[] buffer = new byte[8];
            stream.Read(buffer, 0, 8);

            for (int i = 0; i < 8; i++)
            {
                if (buffer[i] != signature[i])
                    return false;
            }

            return true;
        }

        // Method to check if the stream is in OOXML format
        public static bool IsStreamOOXML(Stream stream)
        {
            byte[] signature = new byte[4] { 0x50, 0x4B, 0x03, 0x04 };

            byte[] buffer = new byte[4];
            stream.Read(buffer, 0, 4);

            for (int i = 0; i < 4; i++)
            {
                if (buffer[i] != signature[i])
                    return false;
            }

            return true;
        }
    }
}
