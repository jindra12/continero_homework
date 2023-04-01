using Backend_Homework.Attributes;

namespace Backend_Homework.FileManagers
{
    [CommandLine("file")]
    public class FileManager : IFileManager
    {
        /// <summary>
        /// Asynchronously loads from memory stream, should be used within using() context
        /// </summary>
        /// <param name="path">path to load file from</param>
        /// <returns>Stream with string file inside</returns>
        public async Task<Stream> Load(string path)
        {
            return new MemoryStream(await File.ReadAllBytesAsync(path));
        }

        /// <summary>
        /// Saves a stream as string file
        /// </summary>
        /// <param name="path">filepath to save to</param>
        /// <param name="file">stream of file to save</param>
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