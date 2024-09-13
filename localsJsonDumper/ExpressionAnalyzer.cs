using EnvDTE100;

namespace LocalsJsonDumper
{
    internal static class ExpressionAnalyzer
    {
        public static bool ExpressionIsDictionary(Expression2 exp)
        {
            if (exp.Type.StartsWith("System.Collections.Generic.Dictionary"))
            {
                return true;
            }
            if (exp.Type.StartsWith("System.Collections.Generic.IDictionary"))
            {
                return true;
            }
            if (exp.Type.StartsWith("System.Collections.Generic.SortedDictionary"))
            {
                return true;
            }
            if (exp.Type.StartsWith("System.Collections.Concurrent.ConcurrentDictionary"))
            {
                return true;
            }
            if (exp.Type.StartsWith("System.Collections.Generic.SortedList"))
            {
                return true;
            }
            if (exp.Type.StartsWith("System.Collections.SortedList"))
            {
                return true;
            }
            if (exp.Type.StartsWith("System.Collections.Immutable.ImmutableSortedDictionary"))
            {
                return true;
            }
            if (exp.Type.StartsWith("System.Collections.Immutable.ImmutableDictionary"))
            {
                return true;
            }
            return false;
        }

        public static bool ExpressionIsCollectionOrArray(Expression2 exp)
        {
            if (exp.Type.StartsWith("System.Collections"))
            {
                return true;
            }
            if (exp.Type.EndsWith("[]"))
            {
                return true;
            }
            return false;
        }

        public static bool ExpressionIsValue(Expression2 exp)
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

        public static bool ExpressionIsEnum(Expression2 exp)
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
    }
}
