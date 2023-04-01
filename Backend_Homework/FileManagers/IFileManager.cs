namespace Backend_Homework.FileManagers
{
    /// <summary>
    /// Manages loading and saving files
    /// </summary>
    public interface IFileManager
    {
        /// <summary>
        /// Loads a file from T config
        /// </summary>
        public Task<Stream> Load(string config);

        /// <summary>
        /// Saves a file based on configuration
        /// </summary>
        public Task Save(string config, Stream file);
    }
}
