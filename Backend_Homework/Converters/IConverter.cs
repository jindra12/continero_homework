using Backend_Homework.Models;

namespace Backend_Homework.Converters
{
    /// <summary>
    /// Handles conversion between a byte[] array and Content for a specific format
    /// </summary>
    public interface IConverter
    {
        /// <summary>
        /// Loads content from bytes
        /// </summary>
        public Task<IContent> FromStream(Stream file);

        /// <summary>
        /// Outputs stream from content
        /// </summary>
        public Task<Stream> FromContent(IContent content);
    }
}
