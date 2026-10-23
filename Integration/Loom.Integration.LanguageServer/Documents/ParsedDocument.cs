using System.Collections.Immutable;
using Loom.Common.Diagnostics;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Integration.LanguageServer.Documents;

internal sealed record ParsedDocument(TextDocumentSnapshot Document, ProgramNode? Root, ImmutableArray<Diagnostic> Diagnostics);