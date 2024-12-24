using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Rename;
using TeReoLocalizer.Shared.Code.Services;

namespace TeReoLocalizer.Shared.Code;

public static class CsWorkspace
{
    public static MSBuildWorkspace? Workspace => InitService.Workspace?.Workspace;
    public static Solution Solution => InitService.Workspace?.CurrentSolution!;
    
    public static async Task ReplaceIdentifiers(string oldSuffix, string newSuffix)
    {
        if (Workspace == null || Solution == null)
        {
            throw new InvalidOperationException("Workspace nebo Solution není inicializováno.");
        }

        Microsoft.CodeAnalysis.Project? project = Solution.Projects.FirstOrDefault();
        if (project == null)
        {
            throw new InvalidOperationException("V Solution nebyl nalezen žádný projekt.");
        }

        Compilation? compilation = await project.GetCompilationAsync();
        IEnumerable<ISymbol>? symbols = compilation.GetSymbolsWithName(name => name.StartsWith("Reo.") && name.EndsWith(oldSuffix), SymbolFilter.All);

        foreach (ISymbol? symbol in symbols)
        {
            string? newName = $"Reo.{newSuffix}";

            Solution? solution = await Renamer.RenameSymbolAsync(Solution, symbol, new SymbolRenameOptions(true, true, true), newName);
        
            if (solution != Solution)
            {
                bool applied = Workspace.TryApplyChanges(solution);
                if (applied)
                {
                    Console.WriteLine($"Změněno: {symbol.Name} na {newName}");
                    bool? set = InitService.Workspace?.SetCurrentSolution(solution);  // Aktualizujeme Solution pro další iterace
                    
                    
                }
                else
                {
                    Console.WriteLine($"Nepodařilo se změnit: {symbol.Name}");
                }
            }
        }
        
        Solution? newSolution = Solution;
        foreach (Document? document in project.Documents)
        {
            SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync();
            IdentifierRewriter? rewriter = new IdentifierRewriter(oldSuffix, newSuffix);
            SyntaxNode? newRoot = rewriter.Visit(syntaxRoot);

            if (newRoot != syntaxRoot)
            {
                newSolution = newSolution.WithDocumentSyntaxRoot(document.Id, newRoot);
            }
        }
        
        if (newSolution != Solution)
        {
            bool applied = Workspace.TryApplyChanges(newSolution);
            if (applied)
            {
                Console.WriteLine("Změny byly úspěšně aplikovány.");
                bool? set = InitService.Workspace?.SetCurrentSolution(newSolution);
            }
            else
            {
                Console.WriteLine("Nepodařilo se aplikovat změny.");
            }
        }
        else
        {
            Console.WriteLine("Nebyly nalezeny žádné identifikátory k nahrazení.");
        }

        Console.WriteLine("Proces nahrazování dokončen.");
    }
    
    private class IdentifierRewriter : CSharpSyntaxRewriter
    {
        private readonly string _oldSuffix;
        private readonly string _newSuffix;

        public IdentifierRewriter(string oldSuffix, string newSuffix)
        {
            _oldSuffix = oldSuffix;
            _newSuffix = newSuffix;
        }

        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.StringLiteralExpression))
            {
                string? oldText = $"Reo.{_oldSuffix}";
                string? newText = $"Reo.{_newSuffix}";
                if (node.Token.ValueText.Contains(oldText))
                {
                    string? replacedText = node.Token.ValueText.Replace(oldText, newText);
                    return node.Update(SyntaxFactory.Literal(replacedText));
                }
            }
            return base.VisitLiteralExpression(node);
        }

        public override SyntaxNode VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node)
        {
            string? oldText = $"Reo.{_oldSuffix}";
            string? newText = $"Reo.{_newSuffix}";
            List<InterpolatedStringContentSyntax>? newContents = new List<InterpolatedStringContentSyntax>();
            bool changed = false;

            foreach (InterpolatedStringContentSyntax? content in node.Contents)
            {
                if (content is InterpolatedStringTextSyntax textPart)
                {
                    string? replacedText = textPart.TextToken.Text.Replace(oldText, newText);
                    if (replacedText != textPart.TextToken.Text)
                    {
                        SyntaxToken newToken = SyntaxFactory.Token(
                            textPart.TextToken.LeadingTrivia,
                            SyntaxKind.InterpolatedStringTextToken,
                            replacedText,
                            replacedText,
                            textPart.TextToken.TrailingTrivia);
                        newContents.Add(SyntaxFactory.InterpolatedStringText(newToken));
                        changed = true;
                    }
                    else
                    {
                        newContents.Add(textPart);
                    }
                }
                else
                {
                    newContents.Add(content);
                }
            }

            if (changed)
            {
                return node.WithContents(SyntaxFactory.List(newContents));
            }
    
            return node;
        }
    }
}