using System;
using System.Collections.Generic;
using System.Diagnostics;
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
      
        private Regex PartOfCollection { get; } = new Regex(@"\[\d+\]");

        private uint MaxDepth { get; set; }

        private Regex IgnorePropNameRegex { get; set; }

        private Regex IgnorePropTypeRegex { get; set; }

        #region Breadth first generation
        public string GenerateBreadthFirst(Expression2 expression, CancellationToken cancellationToken, uint maxDepth, Regex nameIgnoreRegex, Regex typeIgnoreRegex)
        {
            try
            {
                MaxDepth = maxDepth;
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
                var result = BuildNodeTree(expression, cancellationToken);
                return result.GenerateJson(cancellationToken);
            }
            catch (Exception ex)
            {
                return $"<ERROR>{Environment.NewLine}Could not generate JSON due to {ex.GetType().Name}:{Environment.NewLine}{ex.Message}";
            }
        }

        private TreeNode BuildNodeTree(Expression2 expression, CancellationToken cancellationToken)
        {
            var root = new TreeNode
            {
                Depth = 0,
                Expression = expression,
                Type = TreeNode.GetNodeTypeFromExpression(expression),
                Name = expression.Name
            };
            var queue = new Queue<TreeNode>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                if(cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                var node = queue.Dequeue();
                if (node.Depth >= MaxDepth)
                {
                    continue;
                }
                foreach (Expression2 expr in node.Expression.DataMembers)
                {

                    if (node.Type == NodeType.Value)
                    {
                        continue;
                    }
                    if ((node.Type == NodeType.Dictionary || node.Type == NodeType.Array) && !PartOfCollection.IsMatch(expr.Name) && node.Parent?.Type != NodeType.Dictionary)
                    {
                        continue;
                    }
                    //Omit record specific datamember
                    if (expr.Name == "EqualityContract")
                    {
                        Debug.WriteLine("Omitting EqualityContract");
                        continue;
                    }
                    //Omit user filtered properties
                    if (IgnorePropNameRegex != null && IgnorePropNameRegex.IsMatch(expr.Name))
                    {
                        Debug.WriteLine($"Ignoring {expr.Name} due to name regex match: {IgnorePropNameRegex}");
                        continue;
                    }
                    if (IgnorePropTypeRegex != null && IgnorePropTypeRegex.IsMatch(expr.Type))
                    {
                        Debug.WriteLine($"Ignoring {expr.Name} due to type regex match: {IgnorePropNameRegex}");
                        continue;
                    }
                    

                    var child = new TreeNode
                    {
                        //Dictionaries have a collection datamember that holds the key-value pair. We do not want that to affect the tree depth since it will not be part of the JSON.
                        Depth = node.Type != NodeType.Dictionary ? node.Depth + 1 : node.Depth,
                        Expression = expr,
                        Type = TreeNode.GetNodeTypeFromExpression(expr),
                        Parent = node,
                        Name = expr.Name
                    };
                    queue.Enqueue(child);
                    node.Children.Add(child);
                }
            }
            return root;
        }
        #endregion

        #region Recursive generation

        [Obsolete("Use breadth first instead")]
        public string GenerateRecursive(Expression2 expression, CancellationToken cancellationToken, uint maxDepth, Regex nameIgnoreRegex, Regex typeIgnoreRegex)
        {
            try
            {
                MaxDepth = maxDepth;
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
                var result = GenerateJsonRecurse(expression, 0, cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                return $"<ERROR>{Environment.NewLine}Could not generate JSON due to {ex.GetType().Name}:{Environment.NewLine}{ex.Message}";
            }
        }
       

        private string GenerateJsonRecurse(Expression2 currentExpression, uint currentDepth, CancellationToken cancellationToken)
        {
            if (currentExpression == null)
            {
                return $"<Could not evaluate Expression>";
            }

            Debug.WriteLine($"Depth: {currentDepth}. {currentExpression.Name} {currentExpression.Type}:{currentExpression.Value}");

            if (cancellationToken.IsCancellationRequested)
            {
                Debug.WriteLine(Constants.TOKEN_CANCELLATION_MESSAGE);
                return Constants.TOKEN_CANCELLATION_MESSAGE;
            }

            if (currentExpression.Value == "null")
            {
                return "null";
            }
            else if (ExpressionAnalyzer.ExpressionIsValue(currentExpression))
            {
                return $"{ValueHelper.GetJsonRepresentationOfValue(currentExpression)}";
            }
            else if (ExpressionAnalyzer.ExpressionIsEnum(currentExpression))
            {
                return $"\"{currentExpression.Value}\"";
            }
            else if (currentDepth >= MaxDepth)
            {
                return $"\"{currentExpression.Value}\"";
            }
            else if (ExpressionAnalyzer.ExpressionIsDictionary(currentExpression))
            {
                var values = new List<string>();
                string DictionaryReturn()
                {
                    return $"{{{Environment.NewLine}{ValueHelper.GenerateIndentation(currentDepth + 1)}{string.Join($",{Environment.NewLine}{ValueHelper.GenerateIndentation(currentDepth + 1)}", values.ToArray())}{Environment.NewLine}{ValueHelper.GenerateIndentation(currentDepth)}}}";
                }

                // collections are dealt with in entirety
                foreach (Expression2 dicSubExpression in currentExpression.DataMembers)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Debug.WriteLine(Constants.TOKEN_CANCELLATION_MESSAGE);
                        values.Add(Constants.TOKEN_CANCELLATION_MESSAGE);
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
                                value = GenerateJsonRecurse(dicCollectionExpression, currentDepth + 1, cancellationToken);
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
            else if (ExpressionAnalyzer.ExpressionIsCollectionOrArray(currentExpression))
            {
                var values = new List<string>();
                string ListReturn()
                {
                    return $"[{Environment.NewLine}{ValueHelper.GenerateIndentation(currentDepth + 1)}{string.Join($",{Environment.NewLine}{ValueHelper.GenerateIndentation(currentDepth + 1)}", values.ToArray())}{Environment.NewLine}{ValueHelper.GenerateIndentation(currentDepth)}]";
                }

                // collections are dealt with in entirety
                foreach (Expression2 ex in currentExpression.DataMembers)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Debug.WriteLine(Constants.TOKEN_CANCELLATION_MESSAGE);
                        values.Add(Constants.TOKEN_CANCELLATION_MESSAGE);
                        return ListReturn();
                    }
                    if (PartOfCollection.IsMatch(ex.Name))
                    {
                        values.Add(GenerateJsonRecurse(ex, currentDepth + 1, cancellationToken));
                    }
                }
                return ListReturn();
            }
            else
            {
                var values = new List<string>();
                string ObjectReturn()
                {
                    return $"{{{Environment.NewLine}{ValueHelper.GenerateIndentation(currentDepth + 1)}{string.Join($",{Environment.NewLine}{ValueHelper.GenerateIndentation(currentDepth + 1)}", values.ToArray())}{Environment.NewLine}{ValueHelper.GenerateIndentation(currentDepth)}}}";
                }
                foreach (Expression2 subExpression in currentExpression.DataMembers)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Debug.WriteLine(Constants.TOKEN_CANCELLATION_MESSAGE);
                        values.Add(Constants.TOKEN_CANCELLATION_MESSAGE);
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
                        values.Add($"\"{subExpression.Name}\": {GenerateJsonRecurse(subExpression, currentDepth + 1, cancellationToken)}");
                    }
                }
                return ObjectReturn();
            }
        }
        #endregion
    }
    
}