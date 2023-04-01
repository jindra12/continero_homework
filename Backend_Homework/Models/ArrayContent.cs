namespace Backend_Homework.Models
{
    /// <summary>
    /// Keeps the array structure between various formats
    /// </summary>
    public class ArrayContent : IContent
    {
        /// <summary>
        /// Object children as array
        /// </summary>
        public IList<IContent> Children { get; set; } = new List<IContent>();
    }
}
