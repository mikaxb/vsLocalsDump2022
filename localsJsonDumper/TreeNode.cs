using EnvDTE100;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace LocalsJsonDumper
{
    internal enum NodeType
    {
        Value,
        Dictionary,
        Array,
        Enum,
        Object,
        Null
    }

    internal class TreeNode
    {
        public Expression2 Expression { get; set; }
        public TreeNode Parent { get; set; }
        public List<TreeNode> Children { get; set; } = new List<TreeNode>();
        public uint Depth { get; set; }
        public NodeType Type { get; set; }
        public string Name { get; set; }

        public string GenerateJson(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Debug.WriteLine(Constants.TOKEN_CANCELLATION_MESSAGE);
                return Constants.TOKEN_CANCELLATION_MESSAGE;
            }

            StringBuilder returnBuilder = new StringBuilder();

            switch (Type)
            {
                case NodeType.Null:
                    returnBuilder.Append("null");
                    break;
                case NodeType.Value:
                    returnBuilder.Append(ValueHelper.GetJsonRepresentationOfValue(Expression));
                    break;
                case NodeType.Enum:
                    returnBuilder.Append($"\"{Expression.Value}\"");
                    break;
                case NodeType.Array:
                    returnBuilder.Append($"[");
                    returnBuilder.Append(string.Join(",", Children.Select(c => $"{Environment.NewLine}{ValueHelper.GenerateIndentation(c.Depth)}{c.GenerateJson(cancellationToken)}").ToArray()));
                    returnBuilder.Append($"{Environment.NewLine}{ValueHelper.GenerateIndentation(Depth)}]");
                    break;
                case NodeType.Dictionary:
                    returnBuilder.Append("{");
                    returnBuilder.Append(string.Join(",", Children.Select(dicEntry => $"{Environment.NewLine}{ValueHelper.GenerateIndentation(dicEntry.Children.First().Depth)}\"{dicEntry.Children.First(c => c.Name == "Key").Expression.Value.Replace("{", "").Replace("}", "").Trim('"')}\": {dicEntry.Children.First(c => c.Name == "Value").GenerateJson(cancellationToken)}").ToArray()));
                    returnBuilder.Append($"{Environment.NewLine}{ValueHelper.GenerateIndentation(Depth)}}}");
                    break;
                case NodeType.Object:
                    returnBuilder.Append("{");
                    returnBuilder.Append(string.Join(",", Children.Select(c => $"{Environment.NewLine}{ValueHelper.GenerateIndentation(c.Depth)}\"{c.Name}\": {c.GenerateJson(cancellationToken)}").ToArray()));
                    returnBuilder.Append($"{Environment.NewLine}{ValueHelper.GenerateIndentation(Depth)}}}");
                    break;
                default:
                    returnBuilder.Append("<Unsupported node type>");
                    break;
            }


            return returnBuilder.ToString();
        }

        public static NodeType GetNodeTypeFromExpression(Expression2 expression)
        {
            if (expression.Value == "null")
            {
                return NodeType.Null;
            }
            else if (ExpressionAnalyzer.ExpressionIsValue(expression))
            {
                return NodeType.Value;
            }
            else if (ExpressionAnalyzer.ExpressionIsEnum(expression))
            {
                return NodeType.Enum;
            }
            else if (ExpressionAnalyzer.ExpressionIsDictionary(expression))
            {
                return NodeType.Dictionary;
            }
            else if (ExpressionAnalyzer.ExpressionIsCollectionOrArray(expression))
            {
                return NodeType.Array;
            }
            return NodeType.Object;
        }

    }
}
