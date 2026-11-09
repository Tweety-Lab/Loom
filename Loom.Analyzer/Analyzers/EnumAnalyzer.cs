using static Loom.Common.Diagnostics.DiagnosticDescription;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks for invaid enum members.
/// </summary>
[LoomAnalyzer]
public class EnumAnalyzer : Analyzer
{
    public static DiagnosticDescription EnumValueOutOfRange = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Enum value exceeds the signed 32-bit range.");
    public static DiagnosticDescription DuplicateEnumMember = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Duplicate enum member '{0}'.");

    [Visitor]
    public void Visit(EnumDeclarationNode node)
    {
        var names = new HashSet<string>();
        long next = 0;
        foreach (var member in node.Body.Contents.OfType<EnumMemberNode>())
        {
            long value = member.Value ?? next;
            if (value > int.MaxValue)
            {
                Context.DiagnosticContext?.Report(EnumValueOutOfRange, member.Name.Location);
                value = 0;
            }

            if (!names.Add(member.Name.Text))
                Context.DiagnosticContext?.Report(DuplicateEnumMember, member.Name.Location, member.Name.Text);

            next = value + 1;
        }
    }
}