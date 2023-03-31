namespace Backend_Homework.FileManagers
{
    /// <summary>
    /// Manages loading and saving files
    /// </summary>
    public interface IFileManager<T>
    {
        /// <summary>
        /// Loads a file from T config
        /// </summary>
        public Task<Stream> Load(T config);

        /// <summary>
        /// Saves a file based on configuration
        /// </summary>
        public Task Save(T config, Stream file);

        /// <summary>
        /// Process command line input, output configuration
        /// </summary>
        public T ParseConfigFromCommandLine(string input);
    }
}
