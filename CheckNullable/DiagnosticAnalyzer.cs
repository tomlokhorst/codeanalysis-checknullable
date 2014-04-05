using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CheckNullable
{
  [DiagnosticAnalyzer]
  [ExportDiagnosticAnalyzer(DiagnosticId, LanguageNames.CSharp)]
  public class DiagnosticAnalyzer : ISyntaxNodeAnalyzer<SyntaxKind>
  {
    internal const string DiagnosticId = "CheckNullable";
    internal const string Description = "Nullable is not checked before use";
    internal const string MessageFormat = "Nullable '{0}' must be checked for null values before accessing 'Value' property";
    internal const string Category = "NullCheck";

    internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning);

    public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
      get { return ImmutableArray.Create(Rule); }
    }

    public ImmutableArray<SyntaxKind> SyntaxKindsOfInterest
    {
      get { return ImmutableArray.Create(SyntaxKind.SimpleMemberAccessExpression); }
    }

    public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
    {
      var expr = node as MemberAccessExpressionSyntax;
      if (expr == null) return;

      var identifier = expr.Expression as IdentifierNameSyntax;
      if (identifier == null) return;

      // Only handle .Value
      if (expr.Name.Identifier.ValueText != "Value") return;

      var typeInfo = semanticModel.GetTypeInfo(identifier, cancellationToken);
      if (typeInfo.ConvertedType == null) return;

      // TODO: Is there a better way to test if this ia System.Nullable,
      // instead of some random Nullable type elsewhere in type hierarchie?
      if (typeInfo.ConvertedType.Name != "Nullable") return;

      // TODO: do actual control/data analysis
      if (uglyCheckHasValue(expr, identifier, semanticModel)) return;

      var diagnostic = Diagnostic.Create(Rule, node.GetLocation(), identifier.ToString());
      addDiagnostic(diagnostic);
    }

    // TODO: There has got to be a better way to do this
    public bool uglyCheckHasValue(SyntaxNode node, IdentifierNameSyntax identifier, SemanticModel semanticModel)
    {
      var ifStatement = node.FirstAncestorOrSelf<IfStatementSyntax>();
      if (ifStatement == null) return false;

      var hasValue = checksHasValue(ifStatement.Condition, identifier, semanticModel);
      if (hasValue) return true;

      return uglyCheckHasValue(ifStatement.Parent, identifier, semanticModel);
    }

    public bool checksHasValue(SyntaxNode node, IdentifierNameSyntax identifier, SemanticModel semanticModel)
    {
      if ((var memberAccess = node as MemberAccessExpressionSyntax) != null)
      {
        return memberAccess.Expression.IsEquivalentTo(identifier)
          && memberAccess.Name.Identifier.ValueText == "HasValue";
      }

      if ((var binaryExpression = node as BinaryExpressionSyntax) != null)
      {
        var leftIdentifier = binaryExpression.Left as IdentifierNameSyntax;

        return leftIdentifier != null
          && leftIdentifier.Identifier.Value == identifier.Identifier.Value
          && binaryExpression.OperatorToken.ValueText == "!="
          && binaryExpression.Right.IsKind(SyntaxKind.NullLiteralExpression);
      }

      return false;
    }
  }
}
