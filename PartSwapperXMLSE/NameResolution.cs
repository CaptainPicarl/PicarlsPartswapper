using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PartSwapperXMLSE
{
    public class NameResolver
    {
        public Dictionary<SyntaxToken, I_WCDefinition?> resolutionDict;

        public NameResolver()
        {
            resolutionDict = new Dictionary<SyntaxToken, I_WCDefinition?>();
        }

        // returns true if identifier was added or already there. False if an error occurs.
        public bool addIdentifierOnly(SyntaxToken identifier)
        {
            try
            {
                if (resolutionDict.Keys.Contains(identifier))
                {
                    // return true since the identifier is already there
                    return true;
                }
                else
                {
                    resolutionDict.Add(identifier, null);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error adding Identifier Only!:\n" + ex.Message.ToString());
                return false;
            }

        }

        public bool addIdentifierAndDefinition(SyntaxToken identifier, I_WCDefinition wcDef)
        {
            try
            {
                if (resolutionDict.Keys.Contains(identifier))
                {
                    // return true since the identifier is already there
                    return true;
                }
                else
                {
                    resolutionDict.Add(identifier, wcDef);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error adding Identifier Only!:\n" + ex.Message.ToString());
                return false;
            }
        }

        // Returns true if identifier added to dict. False otherwise.
        public bool assignIdentifierToWCDef(SyntaxToken identifier, I_WCDefinition wcDef)
        {
            if (resolutionDict.ContainsKey(identifier))
            {
                resolutionDict[identifier] = wcDef;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool checkIdentifierExists(SyntaxToken identifier)
        {
            // Check if the the key is inside the dict
            if (resolutionDict.Keys.Contains(identifier))
            {
                return true;
            }
            else
            {
                // If the key is not inside the dict, then the value definitely isn't in the dict.
                return false;
            }
        }

        public bool checkIdentifierDefined(SyntaxToken identifier)
        {
            // Check if the the key is inside the dict
            if (resolutionDict.Keys.Contains(identifier))
            {
                // Then check if the value is defined
                if (resolutionDict[identifier] == null)
                {
                    // if it's not: false
                    return false;
                }
                else
                {
                    // if it is: true
                    return true;
                }
            }
            else
            {
                // If the key is not inside the dict, then the value definitely isn't in the dict.
                return false;
            }
        }

        public T? checkResolveIdentifier<T>(SyntaxToken identifier) where T : I_WCDefinition
        {

            if (checkIdentifierExists(identifier) && checkIdentifierDefined(identifier))
            {
                return resolveSyntaxIdentifier<T>(identifier);
            }
            else
            {
                // At this point: We have an rvalue that we cannot resolve. Throw error.
                throw new ArgumentException($"Found unresolved token!\nToken is:\n{identifier}");
            }
        }

        public T? resolveSyntaxIdentifier<T>(SyntaxToken identifier)
        {

            T wcDef = default;

            if (resolutionDict.ContainsKey(identifier))
            {
                wcDef = (T)resolutionDict[identifier];
                return wcDef;
            }
            else
            {
                return wcDef;

                // Decision between behaviors: Null or throw? Or something else?

                //throw new Exception($"resolveSyntaxIdentifier did not find {identifier.ToString()} in resolveDict!");
            }
        }
    }

}
