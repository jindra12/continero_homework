namespace Backend_Homework.Models
{
    /// <summary>
    /// Keeps a primitive value
    /// </summary>
    public class PrimitiveContent : IContent
    {
        /// <summary>
        /// Primitive value, e.g. number, string, boolean etc.
        /// </summary>
        public object? Value { get; set; }
    }
}
