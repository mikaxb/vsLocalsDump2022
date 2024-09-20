using EnvDTE100;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LocalsJsonDumper
{
    internal static class ValueHelper
    {
        public static string GenerateIndentation(uint depth)
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

        public static string GetJsonRepresentationOfValue(Expression2 exp)
        {
            switch (exp.Type.Trim('?'))
            {
                case "System.DateTime":
                    return HandleDatetime(exp);
                case "System.DateTimeOffset":
                    return HandleDatetimeOffset(exp);
                case "System.Guid":
                case "System.TimeSpan":
                    return $"\"{exp.Value.Replace("{", "").Replace("}", "")}\"";
                case "string":
                case "int":
                case "uint":
                case "nint":
                case "nuint":
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

        public static string HandleDatetime(Expression2 exp)
        {
            try
            {
                var subExpressions = new List<Expression2>();
                foreach (Expression2 subExpression in exp.DataMembers)
                {
                    subExpressions.Add(subExpression);
                }

                int year = int.Parse(subExpressions.First(e => e.Name == nameof(DateTime.Year)).Value);
                int month = int.Parse(subExpressions.First(e => e.Name == nameof(DateTime.Month)).Value);
                int day = int.Parse(subExpressions.First(e => e.Name == nameof(DateTime.Day)).Value);
                int hour = int.Parse(subExpressions.First(e => e.Name == nameof(DateTime.Hour)).Value);
                int minute = int.Parse(subExpressions.First(e => e.Name == nameof(DateTime.Minute)).Value);
                int second = int.Parse(subExpressions.First(e => e.Name == nameof(DateTime.Second)).Value);
                int millisecond = int.Parse(subExpressions.First(e => e.Name == nameof(DateTime.Millisecond)).Value);

                var dt = new DateTime(year, month, day, hour, minute, second, millisecond);
                return $"\"{dt:yyyy-MM-ddTHH:mm:ss.fff}\"";
            }
            catch
            {
                return $"\"{exp.Value.Replace("{", "").Replace("}", "")}\"";
            }
        }

        public static string HandleDatetimeOffset(Expression2 exp)
        {
            try
            {
                var subExpressions = new List<Expression2>();
                foreach (Expression2 expression in exp.DataMembers)
                {
                    subExpressions.Add(expression);
                }

                int year = int.Parse(subExpressions.First(e => e.Name == nameof(DateTimeOffset.Year)).Value);
                int month = int.Parse(subExpressions.First(e => e.Name == nameof(DateTimeOffset.Month)).Value);
                int day = int.Parse(subExpressions.First(e => e.Name == nameof(DateTimeOffset.Day)).Value);
                int hour = int.Parse(subExpressions.First(e => e.Name == nameof(DateTimeOffset.Hour)).Value);
                int minute = int.Parse(subExpressions.First(e => e.Name == nameof(DateTimeOffset.Minute)).Value);
                int second = int.Parse(subExpressions.First(e => e.Name == nameof(DateTimeOffset.Second)).Value);
                int millisecond = int.Parse(subExpressions.First(e => e.Name == nameof(DateTimeOffset.Millisecond)).Value);

                var offsetExpressions = new List<Expression2>();
                foreach (Expression2 expression in subExpressions.First(e => e.Name == nameof(DateTimeOffset.Offset)).DataMembers)
                {
                    offsetExpressions.Add(expression);
                }
                int dayOffset = int.Parse(offsetExpressions.First(e => e.Name == nameof(TimeSpan.Days)).Value);
                int hourOffset = int.Parse(offsetExpressions.First(e => e.Name == nameof(TimeSpan.Hours)).Value);
                int minuteOffset = int.Parse(offsetExpressions.First(e => e.Name == nameof(TimeSpan.Minutes)).Value);
                int secondOffset = int.Parse(offsetExpressions.First(e => e.Name == nameof(TimeSpan.Seconds)).Value);
                int millisecondOffset = int.Parse(offsetExpressions.First(e => e.Name == nameof(TimeSpan.Milliseconds)).Value);

                var offset = new TimeSpan(dayOffset, hourOffset, minuteOffset, secondOffset, millisecondOffset);
                var dto = new DateTimeOffset(year, month, day, hour, minute, second, millisecond, offset);
                return $"\"{dto:yyyy-MM-ddTHH:mm:ss.fffzzz}\"";
            }
            catch
            {
                return $"\"{exp.Value.Replace("{", "").Replace("}", "")}\"";
            }
        }
    }
}
