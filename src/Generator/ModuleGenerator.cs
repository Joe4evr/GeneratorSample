using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Generator
{
    [Generator]
    public sealed class ModuleGenerator : ISourceGenerator
    {
        private static readonly Type _moduleBaseType = typeof(ModuleBase);
        //private static readonly Type _cmdAttrType = typeof(CommandAttribute);

        public void Execute(SourceGeneratorContext context)
        {
            if (!((context.SyntaxReceiver is ModuleSyntaxReceiver msr)
                && msr.TryGetModuleClass(
                    context.Compilation, out var semanticModel,
                    out var moduleDecl, out var moduleSymbol)))
                return;

            var methods = moduleDecl.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Any(at => at.Name.ToString() == "Command"))
                .ToArray();
            if (methods.Length == 0) return;

            using var stream = new MemoryStream();
            using var indentWriter = new IndentedTextWriter(new StreamWriter(stream, Encoding.UTF8));

            indentWriter.WriteLine($"{moduleDecl.Modifiers} {moduleDecl.Keyword} {moduleDecl.Identifier}");
            indentWriter.WriteWrappedIndented(('{', '}'), methods, (writer, methods) =>
            {
                writer.WriteLine("protected override IEnumerable<CommandInfo> AutoRegister()");
                writer.WriteWrappedIndented(('{', '}'), methods, (writer, methods) =>
                {
                    writer.WriteLine($"return new CommandInfo[]");
                    writer.WriteWrappedIndented(("{", "};"), methods, (writer, methods) =>
                    {
                        WriteCommandRegistrations(methods, writer);
                    });
                });
            });

            indentWriter.Flush();
            stream.Position = 0;
            var source = SourceText.From(stream, Encoding.UTF8);
            context.AddSource($@"{moduleDecl.Identifier}.Generated.cs", source);
        }

        public void Initialize(InitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new ModuleSyntaxReceiver());
        }

        private static void WriteCommandRegistrations(
            IEnumerable<MethodDeclarationSyntax> methods,
            IndentedTextWriter indentWriter)
        {
            foreach (var method in methods)
            {
                indentWriter.WriteLine("new CommandInfoBuilder()");
                indentWriter.WriteWrappedIndented(("{", "}.Build(),"), method, (writer, m) =>
                {
                    var name = m.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .Single(at => at.Name.ToString() == "Command")
                        .ArgumentList?.Arguments
                        .FirstOrDefault()?.Expression.ToString();
                    if (name != null)
                        writer.WriteLine($"Name = {name},");

                    var parameters = m.ParameterList.Parameters
                        //.Where(p => p.Type != null)
                        .Select(p => $"typeof({p.Type!})");
                    if (parameters.Any())
                    {
                        writer.WriteLine($"ParameterTypes = new Type[]");
                        writer.WriteWrappedIndented(("{", "},"), parameters, (writer, ps) =>
                        {
                            foreach (var param in ps)
                                writer.WriteLine($"{param},");
                        });
                    }
                });
            }
        }

        private sealed class ModuleSyntaxReceiver : ISyntaxReceiver
        {
            private ClassDeclarationSyntax? ClassDecl { get; set; }

            public bool TryGetModuleClass(
                Compilation compilation,
                [NotNullWhen(true)] out SemanticModel? semanticModel,
                [NotNullWhen(true)] out ClassDeclarationSyntax? classDecl,
                [NotNullWhen(true)] out ITypeSymbol? classSymbol)
            {
                semanticModel = null;
                classSymbol = null;

                classDecl = ClassDecl;
                if (classDecl is null) return false;

                semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
                classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as ITypeSymbol;
                if (classSymbol is null || classSymbol.IsAbstract
                    || classSymbol.DeclaredAccessibility != Accessibility.Public) return false;

                return InheritsFromModuleBase(classSymbol);

                static bool InheritsFromModuleBase(ITypeSymbol? typeSymbol)
                {
                    return (typeSymbol?.BaseType?.Name == _moduleBaseType.Name)
                        || InheritsFromModuleBase(typeSymbol?.BaseType);
                }
            }

            void ISyntaxReceiver.OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is ClassDeclarationSyntax classDeclaration)
                {
                    ClassDecl = classDeclaration;
                }
            }
        }
    }
}
