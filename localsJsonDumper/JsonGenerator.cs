﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using EnvDTE100;

namespace LocalsJsonDumper
{      
    internal class JsonGenerator
    {
        public JsonGenerator()
        {
            
        }

        private const string _tokenCancelledMessage = "<Operation cancelled>";

        private Regex PartOfCollection { get; } = new Regex(@"\[\d+\]");

        private CancellationToken OperationCancellationToken { get; set; }

        private uint MaxRecurseDepth { get; set; }

        private Regex IgnorePropNameRegex { get; set; }

        private Regex IgnorePropTypeRegex { get; set; }

        public string GenerateJson(Expression2 expression, CancellationToken cancellationToken, uint maxDepth, Regex nameIgnoreRegex, Regex typeIgnoreRegex)
        {
            try
            {
                OperationCancellationToken = cancellationToken;
                MaxRecurseDepth = maxDepth;
                IgnorePropNameRegex = null;
                if (nameIgnoreRegex.ToString() != string.Empty)
                {
                    IgnorePropNameRegex = nameIgnoreRegex;
                }
                IgnorePropTypeRegex = null;
                if (typeIgnoreRegex.ToString() != string.Empty)
                {
                    IgnorePropTypeRegex = typeIgnoreRegex;
                }
                var result = GenerateJsonRecurse(expression, 0);
                return result;
            }
            catch (Exception ex)
            {
                return $"<ERROR>{Environment.NewLine}Could not generate JSON due to {ex.GetType().Name}:{Environment.NewLine}{ex.Message}";
            }
        }

        private bool ExpressionIsDictionary(Expression2 exp)
        {
            if (exp.Type.StartsWith("System.Collections.Generic.Dictionary"))
            {
                return true;
            }
            if (exp.Type.StartsWith("System.Collections.Generic.IDictionary"))
            {
                return true;
            }
            return false;
        }

        private bool ExpressionIsListOrArray(Expression2 exp)
        {
            if (exp.Type.StartsWith("System.Collections.Generic.List"))
            {
                return true;
            }
            if (exp.Type.StartsWith("System.Collections.Generic.ICollection"))
            {
                return true;
            }
            if (exp.Type.StartsWith("System.Collections.Generic.IList"))
            {
                return true;
            }
            if (exp.Type.StartsWith("System.Collections.Generic.IEnumerable"))
            {
                return true;
            }
            if (exp.Type.EndsWith("[]"))
            {
                return true;
            }
            return false;
        }

        private bool ExpressionIsValue(Expression2 exp)
        {
            switch (exp.Type.Trim('?'))
            {
                case "string":
                case "System.DateTime":
                case "System.TimeSpan":
                case "System.DateTimeOffset":
                case "System.Guid":
                case "int":
                case "uint":
                case "nint":
                case "nuint":
                case "char":
                case "bool":
                case "double":
                case "float":
                case "decimal":
                case "long":
                case "ulong":
                case "byte":
                case "sbyte":
                case "short":
                case "ushort":
                    return true;

                default:
                    return false;
            }
        }

        private bool ExpressionIsEnum(Expression2 exp)
        {
            if (exp.DataMembers.Count > 0)
            {
                return false;
            }
            if (exp.Value.Contains("{"))
            {
                return false;
            }
            if (exp.Value.Contains("}"))
            {
                return false;
            }
            return true;
        }

        private string GenerateJsonRecurse(Expression2 currentExpression, uint currentDepth)
        {
            if (currentExpression == null)
            {
                return $"<Could not evaluate Expression. Check that a known type is selected>";
            }

            Debug.WriteLine($"Depth: {currentDepth}. {currentExpression.Name} {currentExpression.Type}:{currentExpression.Value}");

            if (OperationCancellationToken.IsCancellationRequested)
            {
                Debug.WriteLine(_tokenCancelledMessage);
                return _tokenCancelledMessage;
            }

            if(currentExpression.Value == "null")
            {
                return "null";
            }
            else if(ExpressionIsValue(currentExpression))
            {
                return $"{GetJsonRepresentationofValue(currentExpression)}";
            }
            else if (ExpressionIsEnum(currentExpression))
            {
                return $"\"{currentExpression.Value}\"";
            }
            else if (currentDepth >= MaxRecurseDepth)
            {
                return $"\"{currentExpression.Value}\"";
            }
            else if (ExpressionIsDictionary(currentExpression))
            {
                var values = new List<string>();
                string DictionaryReturn()
                {
                    return $"{{{Environment.NewLine}{GenerateIndentation(currentDepth + 1)}{string.Join($",{Environment.NewLine}{GenerateIndentation(currentDepth + 1)}", values.ToArray())}{Environment.NewLine}{GenerateIndentation(currentDepth)}}}";
                }
                foreach (Expression2 dicSubExpression in currentExpression.DataMembers)
                {
                    if (OperationCancellationToken.IsCancellationRequested)
                    {
                        Debug.WriteLine(_tokenCancelledMessage);
                        values.Add(_tokenCancelledMessage);
                        return DictionaryReturn();
                    }

                    if (PartOfCollection.IsMatch(dicSubExpression.Name))
                    {
                        string key = null;
                        string value = null;
                        foreach (Expression2 dicCollectionExpression in dicSubExpression.DataMembers)
                        {
                            if (dicCollectionExpression.Name == "Key")
                            {
                                key = dicCollectionExpression.Value.Replace("{", "").Replace("}", "");
                            }
                            if (dicCollectionExpression.Name == "Value")
                            {
                                value = GenerateJsonRecurse(dicCollectionExpression, currentDepth + 1);
                            }
                        }

                        if (!string.IsNullOrEmpty(key))
                        {
                            values.Add($"\"{key.Trim('"')}\": {value}");
                        }
                    }
                }
                return DictionaryReturn();
            }
            else if (ExpressionIsListOrArray(currentExpression))
            {
                var values = new List<string>();
                string ListReturn()
                {
                    return $"[{Environment.NewLine}{GenerateIndentation(currentDepth + 1)}{string.Join($",{Environment.NewLine}{GenerateIndentation(currentDepth + 1)}", values.ToArray())}{Environment.NewLine}{GenerateIndentation(currentDepth)}]";
                }
                foreach (Expression2 ex in currentExpression.DataMembers)
                {
                    if (OperationCancellationToken.IsCancellationRequested)
                    {
                        Debug.WriteLine(_tokenCancelledMessage);
                        values.Add(_tokenCancelledMessage);
                        return ListReturn();
                    }
                    if (PartOfCollection.IsMatch(ex.Name))
                    {
                        values.Add(GenerateJsonRecurse(ex, currentDepth + 1));
                    }
                }
                return ListReturn();
            }
            else
            {
                var values = new List<string>();
                string ObjectReturn()
                {
                    return $"{{{Environment.NewLine}{GenerateIndentation(currentDepth + 1)}{string.Join($",{Environment.NewLine}{GenerateIndentation(currentDepth + 1)}", values.ToArray())}{Environment.NewLine}{GenerateIndentation(currentDepth)}}}";
                }
                foreach (Expression2 subExpression in currentExpression.DataMembers)
                {                   
                    if (OperationCancellationToken.IsCancellationRequested)
                    {
                        Debug.WriteLine(_tokenCancelledMessage);
                        values.Add(_tokenCancelledMessage);
                        return ObjectReturn();
                    }
                    //Omit this. It is automatically part of record-objects.
                    if (subExpression.Name == "EqualityContract")
                    {
                        Debug.WriteLine("Omitting EqualityContract");
                        continue;
                    }
                    //Skip user filtered properties
                    if (IgnorePropNameRegex != null && IgnorePropNameRegex.IsMatch(subExpression.Name))
                    {
                        Debug.WriteLine($"Ignoring {subExpression.Name} due to name regex match: {IgnorePropNameRegex}");
                        continue;
                    }
                    if (IgnorePropTypeRegex != null && IgnorePropTypeRegex.IsMatch(subExpression.Type))
                    {
                        Debug.WriteLine($"Ignoring {subExpression.Type} due to type regex match: {IgnorePropTypeRegex}");
                        continue;
                    }
                    if (subExpression.Value == "null")
                    {
                        values.Add($"\"{subExpression.Name}\": null");
                    }
                    else
                    {
                        values.Add($"\"{subExpression.Name}\": {GenerateJsonRecurse(subExpression, currentDepth + 1)}");
                    }
                }
                return ObjectReturn();
            }
        }

        private string GenerateIndentation(uint depth)
        {
            if (depth == uint.MaxValue)
            {
                depth = uint.MinValue;
            }

            var indentation = new StringBuilder();

            for (int i = 0; i < depth; i++)
            {
                indentation.Append("  ");
            }

            return indentation.ToString();
        }

        private string GetJsonRepresentationofValue(Expression2 exp)
        {
            switch (exp.Type.Trim('?'))
            {
                case "System.Guid":
                case "System.DateTime":
                case "System.TimeSpan":
                case "System.DateTimeOffset":
                    return $"\"{exp.Value.Replace("{", "").Replace("}", "")}\"";

                case "string":
                case "int":
                case "uint":
                case "bool":
                case "double":
                case "float":
                case "decimal":
                case "long":
                case "ulong":
                case "byte":
                case "sbyte":
                case "short":
                case "ushort":
                    return exp.Value;

                case "char":
                    return $"\"{exp.Value.Substring(exp.Value.IndexOf("'") + 1, 1)}\"";

                default:
                    return $" <UNHANDLED TYPE: {exp.Type}>";
            }
        }
    }
}