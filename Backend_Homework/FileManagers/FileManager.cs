using Backend_Homework.Attributes;

namespace Backend_Homework.FileManagers
{
    [CommandLine("file")]
    public class FileManager : IFileManager<string>
    {
        public string ParseConfigFromCommandLine(string input)
        {
            return input;
        }

        async Task<Stream> IFileManager<string>.Load(string path)
        {
            return new MemoryStream(await File.ReadAllBytesAsync(path));
        }

        Task IFileManager<string>.Save(string path, Stream file)
        {
            using (var fileStream = File.Create(path))
            {
                file.Seek(0, SeekOrigin.Begin);
                return file.CopyToAsync(fileStream);
            }
        }
    }
}