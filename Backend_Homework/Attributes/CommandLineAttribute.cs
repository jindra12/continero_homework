using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backend_Homework.Attributes
{
    /// <summary>
    /// Manages connections between IConverter and IFileManager instances and the string representation from console line args
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class CommandLineAttribute : Attribute
    {
        /// <summary>
        /// Name of console argument to be matched with class type
        /// </summary>
        public string Argument { get; internal set; } = default!;

        public CommandLineAttribute(string command)
        {
            Argument = command ?? throw new ArgumentNullException(nameof(command));
        }
    }
}