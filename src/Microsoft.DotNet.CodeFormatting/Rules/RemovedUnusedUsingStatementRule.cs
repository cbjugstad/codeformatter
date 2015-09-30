using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.DotNet.CodeFormatting.Rules
{
    [LocalSemanticRule(RemoveUnusedUsingStatementRule.Name, RemoveUnusedUsingStatementRule.Description, LocalSemanticRuleOrder.RemoveUnusedUsingStatementRule)]
    internal sealed class RemoveUnusedUsingStatementRule : ILocalSemanticFormattingRule
    {
        internal const string Name = "RemoveUnusedUsingStatement";
        internal const string Description = "Remove unused using statements from every document";

        public bool SupportsLanguage(string languageName)
        {
            return
                languageName == LanguageNames.CSharp;
        }

        public async Task<SyntaxNode> ProcessAsync(Document document, SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken);
            var diagnostics = model.GetDiagnostics().Where(s => s.Id == "CS8019");
            var usingDirectives = new List<SyntaxNode>();
            var newDocument = document;

            foreach (var diagnostic in diagnostics)
            {
                var text = diagnostic.Location.SourceSpan;
                var token = syntaxNode.FindToken(text.Start); // do I need to get the root from the document?
                usingDirectives.Add(token.Parent);
            }

            var newNode = syntaxNode.RemoveNodes(usingDirectives, SyntaxRemoveOptions.KeepNoTrivia);
            if (newNode != syntaxNode)
            {
                newDocument = document.WithSyntaxRoot(newNode);
            }
            
            return await newDocument.GetSyntaxRootAsync(cancellationToken);
        }
    }
}