using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using EnvDTE;

namespace LocalsJsonDumper
{  
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "Done in constructor.")]
    internal class JsonGenerator
    {
        public JsonGenerator()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
        }

        private Regex PartOfCollection { get; } = new Regex(@"\[\d+\]");

        private CancellationTokenSource CancellationTokenSource { get; set; }

        private CancellationToken OperationTimeoutToken { get; set; }

        private uint MaxRecurseDepth { get; set; }

        public string GenerateJson(Expression expression, TimeSpan timeout, uint maxDepth)
        {
            try
            {
                CancellationTokenSource = new CancellationTokenSource();
                CancellationTokenSource.CancelAfter(timeout);
                OperationTimeoutToken = CancellationTokenSource.Token;
                MaxRecurseDepth = maxDepth;
                var result = GenerateJsonRecurse(expression, 0);
                return result;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Could not generate JSON due to {ex.GetType().Name}: {ex.Message}.");
            }
            return string.Empty;
        }

        public void StopGeneration()
        {
            CancellationTokenSource.Cancel();
        }

        private bool ExpressionIsDictionary(Expression exp)
        {
            if (exp.Type.StartsWith("System.Collections.Generic.Dictionary"))
            {
                return true;
            }
            return false;
        }

        private bool ExpressionIsListOrArray(Expression exp)
        {
            if (exp.Type.StartsWith("System.Collections.Generic.List"))
            {
                return true;
            }
            if (exp.Type.EndsWith("[]"))
            {
                return true;
            }
            return false;
        }

        private bool ExpressionIsValue(Expression exp)
        {
            switch (exp.Type.Trim('?'))
            {
                case "string":
                case "System.DateTime":
                case "System.TimeSpan":
                case "System.DateTimeOffset":
                case "int":
                case "uint":
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

        private bool ExpressionIsEnum(Expression exp)
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

        private string GenerateJsonRecurse(Expression currentExpression, uint currentDepth)
        {
            if (currentExpression == null)
            {
                return $"< Could not evaluate Expression. Check that a known type is selected. >";
            }

            Debug.WriteLine($"Depth: {currentDepth}. {currentExpression.Type}:{currentExpression.Value}");

            if (OperationTimeoutToken.IsCancellationRequested)
            {
                Debug.WriteLine($"< Timeout occured >");
                return $"<Timeout occured>";
            }

            if (ExpressionIsValue(currentExpression))
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
                foreach (Expression dicSubExpression in currentExpression.DataMembers)
                {
                    if (OperationTimeoutToken.IsCancellationRequested)
                    {
                        Debug.WriteLine($"< Timeout occured >");
                        return $"<Timeout occured>";
                    }

                    if (PartOfCollection.IsMatch(dicSubExpression.Name))
                    {
                        string key = null;
                        string value = null;
                        foreach (Expression dicCollectionExpression in dicSubExpression.DataMembers)
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
                            values.Add($"\"{key.Trim('"')}\":{value}");
                        }
                    }
                }
                return $"{{{Environment.NewLine}{GenerateIndentation(currentDepth + 1)}{string.Join($",{Environment.NewLine}{GenerateIndentation(currentDepth + 1)}", values.ToArray())}{Environment.NewLine}{GenerateIndentation(currentDepth)}}}";
            }
            else if (ExpressionIsListOrArray(currentExpression))
            {
                var values = new List<string>();
                foreach (Expression ex in currentExpression.DataMembers)
                {
                    if (OperationTimeoutToken.IsCancellationRequested)
                    {
                        Debug.WriteLine($"< Timeout occured >");
                        return $"<Timeout occured>";
                    }
                    if (PartOfCollection.IsMatch(ex.Name))
                    {
                        values.Add(GenerateJsonRecurse(ex, currentDepth + 1));
                    }
                }
                return $"[{Environment.NewLine}{GenerateIndentation(currentDepth + 1)}{string.Join($",{Environment.NewLine}{GenerateIndentation(currentDepth + 1)}", values.ToArray())}{Environment.NewLine}{GenerateIndentation(currentDepth)}]";
            }
            else
            {
                var values = new List<string>();
                foreach (Expression subExpression in currentExpression.DataMembers)
                {
                    if (OperationTimeoutToken.IsCancellationRequested)
                    {
                        Debug.WriteLine($"< Timeout occured >");
                        return $"<Timeout occured>";
                    }
                    if (subExpression.Value == "null")
                    {
                        values.Add($"\"{subExpression.Name}\":null");
                    }
                    else
                    {
                        values.Add($"\"{subExpression.Name}\":{GenerateJsonRecurse(subExpression, currentDepth + 1)}");
                    }
                }
                return $"{{{Environment.NewLine}{GenerateIndentation(currentDepth + 1)}{string.Join($",{Environment.NewLine}{GenerateIndentation(currentDepth + 1)}", values.ToArray())}{Environment.NewLine}{GenerateIndentation(currentDepth)}}}";
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
                indentation.Append("\t");
            }

            return indentation.ToString();
        }

        private string GetJsonRepresentationofValue(Expression exp)
        {
            switch (exp.Type.Trim('?'))
            {
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