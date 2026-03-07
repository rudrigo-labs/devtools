using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using DevTools.Rename.Models;

namespace DevTools.Rename.Engine;

public static class RoslynRenamer
{
    public static string Rename(string content, string oldText, string newText, RenameMode mode)
    {
        if (string.IsNullOrEmpty(oldText) || string.IsNullOrEmpty(newText) ||
            string.Equals(oldText, newText, StringComparison.Ordinal))
            return content;

        var tree = CSharpSyntaxTree.ParseText(content);
        var root = tree.GetRoot();

        SyntaxNode newRoot;

        if (mode == RenameMode.NamespaceOnly || oldText.Contains('.'))
        {
            newRoot = new NamespaceRewriter(oldText, newText).Visit(root);
        }
        else
        {
            newRoot = new IdentifierRewriter(oldText, newText).Visit(root);
        }

        return newRoot.ToFullString();
    }

    private sealed class IdentifierRewriter : CSharpSyntaxRewriter
    {
        private readonly string _oldText;
        private readonly string _newText;

        public IdentifierRewriter(string oldText, string newText)
        {
            _oldText = oldText;
            _newText = NormalizeIdentifier(newText);
        }

        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            if (token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == _oldText)
            {
                return SyntaxFactory.Identifier(token.LeadingTrivia, SyntaxKind.IdentifierToken, _newText, _newText, token.TrailingTrivia);
            }

            return base.VisitToken(token);
        }

        private static string NormalizeIdentifier(string value)
        {
            if (SyntaxFacts.IsValidIdentifier(value))
                return value;

            var keywordKind = SyntaxFacts.GetKeywordKind(value);
            if (keywordKind != SyntaxKind.None)
                return "@" + value;

            return value;
        }
    }

    private sealed class NamespaceRewriter : CSharpSyntaxRewriter
    {
        private readonly string _oldNamespace;
        private readonly string _newNamespace;

        public NamespaceRewriter(string oldNamespace, string newNamespace)
        {
            _oldNamespace = oldNamespace;
            _newNamespace = newNamespace;
        }

        public override SyntaxNode? VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            var updated = TryReplaceName(node.Name, out var name)
                ? node.WithName(name!)
                : node;

            return base.VisitNamespaceDeclaration(updated);
        }

        public override SyntaxNode? VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
        {
            var updated = TryReplaceName(node.Name, out var name)
                ? node.WithName(name!)
                : node;

            return base.VisitFileScopedNamespaceDeclaration(updated);
        }

        public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (node.Name is null)
                return base.VisitUsingDirective(node);

            var updated = TryReplaceName(node.Name, out var name)
                ? node.WithName(name!)
                : node;

            return base.VisitUsingDirective(updated);
        }

        public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node)
        {
            if (TryReplaceName(node, out var name))
                return name;

            return base.VisitQualifiedName(node);
        }

        public override SyntaxNode? VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
        {
            var name = node.Name;
            if (TryReplaceName(name, out var replaced))
                return node.WithName((SimpleNameSyntax)replaced!);

            return base.VisitAliasQualifiedName(node);
        }

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            var expressionText = node.ToString();
            if (ShouldReplace(expressionText))
            {
                var replaced = ReplaceNamespace(expressionText);
                var parsed = SyntaxFactory.ParseExpression(replaced).WithTriviaFrom(node);
                return parsed;
            }

            return base.VisitMemberAccessExpression(node);
        }

        private bool TryReplaceName(NameSyntax name, out NameSyntax? newName)
        {
            var text = name.ToString();
            if (!ShouldReplace(text))
            {
                newName = null;
                return false;
            }

            var replaced = ReplaceNamespace(text);
            newName = SyntaxFactory.ParseName(replaced).WithTriviaFrom(name);
            return true;
        }

        private bool ShouldReplace(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return text.Equals(_oldNamespace, StringComparison.Ordinal) ||
                   text.StartsWith(_oldNamespace + ".", StringComparison.Ordinal);
        }

        private string ReplaceNamespace(string text)
        {
            return text.Equals(_oldNamespace, StringComparison.Ordinal)
                ? _newNamespace
                : text.Replace(_oldNamespace, _newNamespace, StringComparison.Ordinal);
        }
    }
}
