using EnvDTE100;
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

        private static readonly IEnumerable<string> _valueTypes = new List<string>
        {
            "string",
            "System.DateTime",
            "System.TimeSpan",
            "System.DateTimeOffset",
            "System.Guid",
            "int",
            "uint",
            "nint",
            "nuint",
            "char",
            "bool",
            "double",
            "float",
            "decimal",
            "long",
            "ulong",
            "byte",
            "sbyte",
            "short"
        };

        public static bool ExpressionIsDictionary(Expression2 exp) => _dictionaryTypeStarts.Any(start => exp.Type.StartsWith(start));
        public static bool ExpressionIsCollectionOrArray(Expression2 exp) => exp.Type.StartsWith("System.Collections") || exp.Type.EndsWith("[]");
        public static bool ExpressionIsEnum(Expression2 exp) => !(exp.DataMembers.Count > 0 || exp.Value.Contains("{") || exp.Value.Contains("}"));
        public static bool ExpressionIsValue(Expression2 exp) => _valueTypes.Contains(exp.Type.Trim('?'));      

    }
}
