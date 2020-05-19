using System;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Generator.Tests
{
    public class GeneratorTests : GeneratorVerifier
    {
        [Fact]
        public async Task Test1()
        {
            var test = @"";
            await VerifyCSharpGeneratorAsync(test, Array.Empty<SyntaxTree>());
        }

        [Fact]
        public async Task Test2()
        {
            var test = @"
using System.Threading.Tasks;
using Generator;

namespace N
{
    public partial class TestModule : ModuleBase
    {
        [Command(""test"")]
        public Task TestCmd() => Task.CompletedTask;
    }
}
";

            var expected = CSharpSyntaxTree.ParseText(SourceText.From(
@"public partial class TestModule
{
    protected override IEnumerable<CommandInfo> AutoRegister()
    {
        return new CommandInfo[]
        {
            new CommandInfoBuilder()
            {
                Name = ""test"",
            }.Build(),
        };
    }
}
", Encoding.UTF8), CSharpParseOptions.Default);

            await VerifyCSharpGeneratorAsync(test, expected);
        }

        [Fact]
        public async Task Test3()
        {
            var test = @"
using System.Threading.Tasks;
using Generator;

namespace N
{
    public partial class TestModule : ModuleBase
    {
        [Command(""echo"")]
        public Task EchoCmd(string input) => Task.CompletedTask;
    }
}
";

            var expected = CSharpSyntaxTree.ParseText(SourceText.From(
@"public partial class TestModule
{
    protected override IEnumerable<CommandInfo> AutoRegister()
    {
        return new CommandInfo[]
        {
            new CommandInfoBuilder()
            {
                Name = ""echo"",
                ParameterTypes = new Type[]
                {
                    typeof(string),
                },
            }.Build(),
        };
    }
}
", Encoding.UTF8), CSharpParseOptions.Default);

            await VerifyCSharpGeneratorAsync(test, expected);
        }

        protected override ISourceGenerator GetCSharpSourceGenerator()
            => new ModuleGenerator();
    }
}
