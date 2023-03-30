namespace Backend_Homework.FileManagers
{
    public class FileManager : IFileManager<string>
    {
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