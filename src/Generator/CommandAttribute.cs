using System;

namespace Generator
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class CommandAttribute : Attribute
    {
        public string? Name { get; }

        public CommandAttribute() { }

        public CommandAttribute(string name)
        {
            Name = name;
        }
    }
}
