using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class NameResolver
{
    public Dictionary<SyntaxNode, WCDefinition> identifierToDefinitionsResolveDict = new Dictionary<IdentifierNameSyntax, WCDefinition>();

    public NameResolver()
    {
    }
}
