using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator
{
    internal static class Extensions
    {
        public static bool Any<T>(this SeparatedSyntaxList<T> list, Func<T, bool> predicate)
            where T : SyntaxNode
        {
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            if (list.Count == 0)
                return false;

            foreach (var item in list)
            {
                if (predicate(item))
                    return true;
            }

            return false;
        }

        public static void WriteWrapped<TState>(
            this IndentedTextWriter indentWriter,
            (char open, char close) wrapper,
            TState state,
            Action<IndentedTextWriter, TState> writeAction)
        {
            indentWriter.WriteLine(wrapper.open);
            writeAction(indentWriter, state);
            indentWriter.WriteLine(wrapper.close);
        }

        public static void WriteWrapped<TState>(
            this IndentedTextWriter indentWriter,
            (string open, string close) wrapper,
            TState state,
            Action<IndentedTextWriter, TState> writeAction)
        {
            indentWriter.WriteLine(wrapper.open);
            writeAction(indentWriter, state);
            indentWriter.WriteLine(wrapper.close);
        }

        public static void WriteIndented<TState>(
            this IndentedTextWriter indentWriter,
            TState state,
            Action<IndentedTextWriter, TState> writeAction)
        {
            indentWriter.Indent += 1;
            writeAction(indentWriter, state);
            indentWriter.Indent -= 1;
        }

        public static void WriteWrappedIndented<TState>(
            this IndentedTextWriter indentWriter,
            (char open, char close) wrapper,
            TState state,
            Action<IndentedTextWriter, TState> writeAction)
        {
            indentWriter.WriteWrapped(wrapper, (state, writeAction), (writer, tup) =>
            {
                writer.WriteIndented(tup.state, tup.writeAction);
            });
        }

        public static void WriteWrappedIndented<TState>(
            this IndentedTextWriter indentWriter,
            (string open, string close) wrapper,
            TState state,
            Action<IndentedTextWriter, TState> writeAction)
        {
            indentWriter.WriteWrapped(wrapper, (state, writeAction), (writer, tup) =>
            {
                writer.WriteIndented(tup.state, tup.writeAction);
            });
        }
    }
}
