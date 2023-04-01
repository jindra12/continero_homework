using Backend_Homework.Attributes;

namespace Backend_Homework.FileManagers
{
    [CommandLine("file")]
    public class FileManager : IFileManager
    {
        public async Task<Stream> Load(string path)
        {
            return new MemoryStream(await File.ReadAllBytesAsync(path));
        }

        public Task Save(string path, Stream file)
        {
            using (var fileStream = File.Create(path))
            {
                file.Seek(0, SeekOrigin.Begin);
                return file.CopyToAsync(fileStream);
            }
        }
    }
}