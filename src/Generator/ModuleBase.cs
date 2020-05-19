using System;
using System.Collections.Generic;

namespace Generator
{
    public abstract class ModuleBase
    {
        protected internal virtual IEnumerable<CommandInfo> AutoRegister()
            => Array.Empty<CommandInfo>();
    }

    public sealed class CommandInfo { }

    public sealed class CommandInfoBuilder
    {
        public string? Name { get; set; }
        public IEnumerable<Type> ParameterTypes { get; set; } = Array.Empty<Type>();


        public CommandInfo Build() => new CommandInfo();
    }
}
