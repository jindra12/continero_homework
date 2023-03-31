using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backend_Homework.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class CommandLineAttribute : Attribute
    {
        public string Command { get; internal set; } = default!;

        public CommandLineAttribute(string command)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }
    }
}