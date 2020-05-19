using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Generator.Tests
{
    public abstract partial class GeneratorVerifier
    {
        protected virtual ISourceGenerator? GetCSharpSourceGenerator()
        {
            return null;
        }

        protected Task VerifyCSharpGeneratorAsync(string source, params SyntaxTree[] expected)
            => VerifyCSharpGeneratorAsync(new[] { source }, expected);

        protected Task VerifyCSharpGeneratorAsync(string[] sources, params SyntaxTree[] expected)
        {
            var generator = GetCSharpSourceGenerator();
            if (generator is null)
                throw new InvalidOperationException("There must be an implementation of " + nameof(GetCSharpSourceGenerator));

            var driver = new CSharpGeneratorDriver(CSharpParseOptions.Default, ImmutableArray.Create(generator), ImmutableArray.Create<AdditionalText>());
            return VerifyGeneratorAsync(sources, LanguageNames.CSharp, driver, expected);
        }

        private async Task VerifyGeneratorAsync(string[] sources, string language, GeneratorDriver driver, params SyntaxTree[] expected)
        {
            var generationResults = await GetGeneratedSourcesAsync(sources, language, driver);
            VerifyResults(generationResults, expected);
        }

        private static void VerifyResults(IReadOnlyCollection<SyntaxTree> actualResults, IReadOnlyCollection<SyntaxTree> expectedResults)
        {
            Assert.Equal(actual: actualResults.Count, expected: expectedResults.Count);

            foreach (var (actual, expected) in actualResults.Zip(expectedResults))
            {
                Assert.True(actual.IsEquivalentTo(expected));
            }
        }
    }
}
