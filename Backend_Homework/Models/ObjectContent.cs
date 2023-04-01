namespace Backend_Homework.Models
{
    /// <summary>
    /// Keeps the object structure between various formats
    /// </summary>
    public class ObjectContent : IContent
    {
        /// <summary>
        /// Object children of the object
        /// </summary>
        public IDictionary<string, IContent> Children { get; set; } = new Dictionary<string, IContent>();
    }
}
