using EnvDTE100;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LocalsJsonDumper
{
    internal static class ExpressionAnalyzer
    {
        private static readonly IEnumerable<string> _dictionaryTypeStarts = new List<string>
        {
            "System.Collections.Generic.Dictionary",
            "System.Collections.Generic.IDictionary",
            "System.Collections.Generic.SortedDictionary",
            "System.Collections.Concurrent.ConcurrentDictionary",
            "System.Collections.Generic.SortedList",
            "System.Collections.SortedList",
            "System.Collections.Immutable.ImmutableSortedDictionary",
            "System.Collections.Immutable.ImmutableDictionary"
        };

        public static bool ExpressionIsDictionary(Expression2 exp)
        {
            return _dictionaryTypeStarts.Any(start => exp.Type.StartsWith(start));
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
