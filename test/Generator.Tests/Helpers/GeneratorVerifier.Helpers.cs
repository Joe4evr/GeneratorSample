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
        private static readonly MetadataReference CorlibReference = CreateReferenceFromFile(typeof(object).Assembly.Location);
        private static readonly MetadataReference SystemCoreReference = CreateReferenceFromFile(typeof(Enumerable).Assembly.Location);
        private static readonly MetadataReference CSharpSymbolsReference = CreateReferenceFromFile(typeof(CSharpCompilation).Assembly.Location);
        private static readonly MetadataReference CodeAnalysisReference = CreateReferenceFromFile(typeof(Compilation).Assembly.Location);
        private static readonly MetadataReference GeneratorReference = CreateReferenceFromFile(typeof(ModuleGenerator).Assembly.Location);

        internal static string DefaultFilePathPrefix = "Test";
        internal static string CSharpDefaultFileExt = "cs";
        internal static string VisualBasicDefaultExt = "vb";
        internal static string TestProjectName = "TestProject";

        private static MetadataReference CreateReferenceFromFile(string path)
            => MetadataReference.CreateFromFile(path);

        #region Get Generated Sources
        private static Task<IReadOnlyCollection<SyntaxTree>> GetGeneratedSourcesAsync(
            string[] sources, string language, GeneratorDriver driver)
        {
            return GetGeneratedSourcesFromDocumentsAsync(driver, GetDocuments(sources, language));
        }

        private static async Task<IReadOnlyCollection<SyntaxTree>> GetGeneratedSourcesFromDocumentsAsync(
            GeneratorDriver driver, Document[] documents)
        {
            var projects = new HashSet<Project>();
            foreach (var document in documents)
            {
                projects.Add(document.Project);
            }

            var generated = ImmutableArray.CreateBuilder<SyntaxTree>();
            foreach (var project in projects)
            {
                var inputCompilation = await project.GetCompilationAsync();
                if (inputCompilation is null)
                    throw new InvalidOperationException("Could not create input compilation.");

                _ = driver.RunFullGeneration(inputCompilation, out var outputCompilation, out var diagnostics);

                Assert.Empty(diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning));

                generated.AddRange(outputCompilation.SyntaxTrees.Except(inputCompilation.SyntaxTrees));
            }

            return generated.ToImmutable();
        }
        #endregion

        #region Set up compilation and documents
        /// <summary>
        /// Given an array of strings as sources and a language, turn them into a project and return the documents and spans of it.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="language">The language the source code is in</param>
        /// <returns>A Tuple containing the Documents produced from the sources and their TextSpans if relevant</returns>
        private static Document[] GetDocuments(string[] sources, string language)
        {
            if (language != LanguageNames.CSharp && language != LanguageNames.VisualBasic)
            {
                throw new ArgumentException("Unsupported Language");
            }

            var project = CreateProject(sources, language);
            var documents = project.Documents.ToArray();

            if (sources.Length != documents.Length)
            {
                throw new InvalidOperationException("Amount of sources did not match amount of Documents created");
            }

            return documents;
        }

        /// <summary>
        /// Create a Document from a string through creating a project that contains it.
        /// </summary>
        /// <param name="source">Classes in the form of a string</param>
        /// <param name="language">The language the source code is in</param>
        /// <returns>A Document created from the source string</returns>
        protected static Document CreateDocument(string source, string language = LanguageNames.CSharp)
        {
            return CreateProject(new[] { source }, language).Documents.First();
        }

        /// <summary>
        /// Create a project using the inputted strings as sources.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="language">The language the source code is in</param>
        /// <returns>A Project created out of the Documents created from the source strings</returns>
        private static Project CreateProject(string[] sources, string language = LanguageNames.CSharp)
        {
            string fileNamePrefix = DefaultFilePathPrefix;
            string fileExt = language == LanguageNames.CSharp ? CSharpDefaultFileExt : VisualBasicDefaultExt;

            var projectId = ProjectId.CreateNewId(debugName: TestProjectName);

            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, TestProjectName, TestProjectName, language)
                .AddMetadataReference(projectId, CorlibReference)
                .AddMetadataReference(projectId, SystemCoreReference)
                .AddMetadataReference(projectId, CSharpSymbolsReference)
                .AddMetadataReference(projectId, CodeAnalysisReference)
                .AddMetadataReference(projectId, GeneratorReference);

            int count = 0;
            foreach (var source in sources)
            {
                var newFileName = fileNamePrefix + count + "." + fileExt;
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                solution = solution.AddDocument(documentId, newFileName, SourceText.From(source));
                count++;
            }
            return solution.GetProject(projectId)!;
        }
        #endregion
    }
}
