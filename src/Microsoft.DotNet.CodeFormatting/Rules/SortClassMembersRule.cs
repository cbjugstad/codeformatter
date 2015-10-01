using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.DotNet.CodeFormatting.Rules
{
    [SyntaxRule(SortClassMembersRule.Name, SortClassMembersRule.Description, SyntaxRuleOrder.SortClassMembersRule)]
    internal sealed class SortClassMembersRule : CSharpOnlyFormattingRule, ISyntaxFormattingRule
    {
        internal const string Name = "SortClassMembers";
        internal const string Description = "Sorts all members in the class";

        public SyntaxNode Process(SyntaxNode syntaxRoot, string languageName)
        {
            var newSyntaxRoot = syntaxRoot;

            var classBlocks = EnumerateClassBlocks(syntaxRoot);

            foreach (var classBlock in classBlocks)
            {
                foreach (var classDeclarationSyntax in classBlock)
                {
                    //var fieldDeclarations = EnumerateBlocks<FieldDeclarationSyntax>(classDeclarationSyntax);
                    //var methodDeclarations = EnumerateBlocks<MethodDeclarationSyntax>(classDeclarationSyntax);
                    //var propertyDeclarations = EnumerateBlocks<PropertyDeclarationSyntax>(classDeclarationSyntax);
                    //var constructorDeclarations = EnumerateBlocks<ConstructorDeclarationSyntax>(classDeclarationSyntax);

                    newSyntaxRoot = AutoArrangeMembersInType(newSyntaxRoot, classDeclarationSyntax,
                        default(CancellationToken));
                }
            }

            return newSyntaxRoot;
        }


        private static IEnumerable<T> EnumerateBlocks<T>(ClassDeclarationSyntax classDeclaration) where T : MemberDeclarationSyntax
        {
            return classDeclaration.ChildNodes().OfType<T>().ToArray();
        }

        private IEnumerable<IEnumerable<ClassDeclarationSyntax>> EnumerateClassBlocks(SyntaxNode root)
        {
            var alreadyAddedNodes = new HashSet<ClassDeclarationSyntax>();

            foreach (var child in root.DescendantNodes())
            {
                if (child is ClassDeclarationSyntax && !alreadyAddedNodes.Contains(child))
                {
                    var blockNodes = child.Parent.ChildNodes().OfType<ClassDeclarationSyntax>().ToArray();

                    alreadyAddedNodes.UnionWith(blockNodes);
                    yield return blockNodes;
                }
            }
        }

        private static SyntaxNode AutoArrangeMembersInType(SyntaxNode root,
            TypeDeclarationSyntax typeDeclaration, CancellationToken cancellationToken)
        {
            var captureWalker = new AutoArrangeCaptureWalker(typeDeclaration);     

            var result = new AutoArrangeReplaceRewriter(
                captureWalker).VisitTypeDeclaration(typeDeclaration);

            var newTree = root.ReplaceNodes(new[] { typeDeclaration },
                (a, b) => result);
            return newTree;
        }

    }

    public sealed class AutoArrangeCaptureWalker
        : CSharpSyntaxWalker
    {
        public AutoArrangeCaptureWalker(TypeDeclarationSyntax node)
            : base()
        {
            this.Constructors =
                new List<ConstructorDeclarationSyntax>();
            this.Enums =
                new List<EnumDeclarationSyntax>();
            this.Events =
                new List<EventFieldDeclarationSyntax>();
            this.Fields =
                new List<FieldDeclarationSyntax>();
            this.Methods =
                new List<MethodDeclarationSyntax>();
            this.Properties =
                new List<PropertyDeclarationSyntax>();
            this.Types =
                new List<AutoArrangeCaptureWalker>();
            this.Target = node;

            var classNode = node as ClassDeclarationSyntax;

            if (classNode != null)
            {
                base.VisitClassDeclaration(classNode);
            }
            else
            {
                base.VisitStructDeclaration(
                    node as StructDeclarationSyntax);
            }
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var capture = new AutoArrangeCaptureWalker(node);
            this.Types.Add(capture);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            this.Constructors.Add(node);
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            this.Enums.Add(node);
        }

        public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            this.Events.Add(node);
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            this.Fields.Add(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            this.Methods.Add(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            this.Properties.Add(node);
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            var capture = new AutoArrangeCaptureWalker(node);
            this.Types.Add(capture);
        }

        public List<ConstructorDeclarationSyntax> Constructors { get; private set; }
        public List<EnumDeclarationSyntax> Enums { get; private set; }
        public List<EventFieldDeclarationSyntax> Events { get; private set; }
        public List<FieldDeclarationSyntax> Fields { get; private set; }
        public List<PropertyDeclarationSyntax> Properties { get; private set; }
        public List<MethodDeclarationSyntax> Methods { get; private set; }
        public TypeDeclarationSyntax Target { get; private set; }
        public List<AutoArrangeCaptureWalker> Types { get; private set; }
    }

    public sealed class AutoArrangeReplaceRewriter
        : CSharpSyntaxRewriter
    {
        private int count;
        private List<SyntaxNode> nodes;

        public AutoArrangeReplaceRewriter(
            AutoArrangeCaptureWalker walker)
        {
            walker.Constructors.Sort(
                (a, b) => a.Identifier.ValueText.CompareTo(
                    b.Identifier.ValueText));
            walker.Enums.Sort(
                (a, b) => a.Identifier.ValueText.CompareTo(
                    b.Identifier.ValueText));
            walker.Events.Sort(
                (a, b) => a.Declaration.Variables[0].Identifier.ValueText.CompareTo(
                    b.Declaration.Variables[0].Identifier.ValueText));
            walker.Fields.Sort(
                (a, b) => a.Declaration.Variables[0]
                    .Identifier.ValueText.CompareTo(
                        b.Declaration.Variables[0]
                            .Identifier.ValueText));
            walker.Methods.Sort(
                (a, b) => a.Identifier.ValueText.CompareTo(
                    b.Identifier.ValueText));
            walker.Properties.Sort(
                (a, b) => a.Identifier.ValueText.CompareTo(
                    b.Identifier.ValueText));
            walker.Types.Sort(
                (a, b) => a.Target.Identifier.ValueText.CompareTo(
                    b.Target.Identifier.ValueText));

            this.nodes = new List<SyntaxNode>();
            this.nodes.AddRange(walker.Events);
            this.nodes.AddRange(walker.Fields);
            this.nodes.AddRange(walker.Properties);
            this.nodes.AddRange(walker.Constructors);
            this.nodes.AddRange(walker.Methods);            
            this.nodes.AddRange(walker.Enums);

            this.nodes.AddRange(
                from typeRewriter in walker.Types
                select new AutoArrangeReplaceRewriter(typeRewriter)
                    .VisitTypeDeclaration(typeRewriter.Target)
                        as TypeDeclarationSyntax);
        }

        private SyntaxNode Replace(SyntaxNode node)
        {
            SyntaxNode result = null;

            if (this.count < this.nodes.Count)
            {
                result = this.nodes[this.count];
                this.count++;
            }
            else
            {
                throw new NotSupportedException();
            }

            return result;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            return this.Replace(node);
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            return this.Replace(node);
        }

        public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            return this.Replace(node);
        }

        public override SyntaxNode VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            return this.Replace(node);
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            return this.Replace(node);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            return this.Replace(node);
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            return this.Replace(node);
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            return this.Replace(node);
        }

        public TypeDeclarationSyntax VisitTypeDeclaration(
            TypeDeclarationSyntax node)
        {
            var classNode = node as ClassDeclarationSyntax;

            if (classNode != null)
            {
                return base.VisitClassDeclaration(classNode)
                    as ClassDeclarationSyntax;
            }
            else
            {
                return base.VisitStructDeclaration(
                    node as StructDeclarationSyntax)
                    as StructDeclarationSyntax;
            }
        }
    }
}