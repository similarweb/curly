using System;
using System.Globalization;
using System.Linq;
using Curly.Attributes;
using Curly.Helpers;
using static Curly.CurlyDsl;

namespace Curly
{
    public static class Stdlib
    {
        /// <summary>
        /// Returns null
        /// </summary>
        /// <returns>null</returns>
        [DslMethod("null")]
        public static string Null()
        {
            return null;
        }
        /// <summary>
        /// Logical 'not'
        /// </summary>
        /// <param name="param">value to negate</param>
        /// <returns>result of negation</returns>
        [DslMethod("not")]
        public static string Not([FromParam] bool param)
        {
            return (!param).ToString();
        }

        /// <summary>
        /// Logical 'and'
        /// </summary>
        /// <param name="first">left operand</param>
        /// <param name="second">right operand</param>
        /// <returns>boolean result</returns>
        [DslMethod("and")]
        public static string And([FromParam(0)] string first, [FromParam(1)] string second)
        {
            return (
                bool.TryParse(first, out bool firstBool) && firstBool &&
                bool.TryParse(second, out bool seconBool) && seconBool).ToString();
        }
        /// <summary>
        /// Logical 'or'
        /// </summary>
        /// <param name="first">left operand</param>
        /// <param name="second">right operand</param>
        /// <returns>boolean result</returns>
        [DslMethod("or")]
        public static string Or([FromParam(0)] string first, [FromParam(1)] string second)
        {
            return (
                bool.TryParse(first, out bool firstBool) && firstBool ||
                bool.TryParse(second, out bool seconBool) && seconBool).ToString();
        }

        /// <summary>
        /// return current time
        /// </summary>
        /// <returns>current time in format "yyyy-MM-dd"</returns>
        [DslMethod("now")]
        public static string Now()
        {
            return DateTime.Now.ToString(SwDateFormat);
        }

        /// <summary>
        /// returns first element from sequence
        /// </summary>
        /// <param name="items">sequence</param>
        /// <returns>first element</returns>
        [DslMethod("first")]
        public static string First([FromParam] string[] items)
        {
            return items.FirstOrDefault();
        }
        /// <summary>
        /// Returns last element of sequence
        /// </summary>
        /// <param name="items">sequence</param>
        /// <returns>last element</returns>
        [DslMethod("last")]
        public static string Last([FromParam] string[] items)
        {
            return items.LastOrDefault();
        }

        /// <summary>
        /// Concatenates multiple sequences
        /// </summary>
        /// <param name="param">params sequences</param>
        /// <returns>resulting sequence</returns>
        [DslMethod("concat")]
        public static string Concat([FromParam] params string[][] param)
        {
            //string[] @params = param.Split(ParamsSeparatorChar);
            var result = param[0];
            param.Skip(1).ForEach(a => result = result.Concat(a).ToArray());
            return string.Join(ListSeparator, result);
        }
        /// <summary>
        /// Returns distinct items from sequence
        /// </summary>
        /// <param name="param">sequence</param>
        /// <returns>distinct elements</returns>
        [DslMethod("distinct")]
        public static string Distinct([FromParam] string[] param)
        {
            return param != null ? string.Join(ListSeparator, param.Distinct()) : null;
        }
        /// <summary>
        /// Filters items in second sequence from first sequence
        /// </summary>
        /// <param name="param">first sequence</param>
        /// <param name="except">items to filter</param>
        /// <returns>resulting sequence</returns>
        [DslMethod("except")]
        public static string Except([FromParam(0)] string[] param, [FromParam(1)] string[] except)
        {
            var result = param.Except(except);
            return string.Join(ListSeparator, result);
        }

        /// <summary>
        /// Returns items existing in all passed sequences
        /// </summary>
        /// <param name="param">sequences to evaluate</param>
        /// <returns>resulting sequence</returns>
        [DslMethod("intersect")]
        public static string Intersect([FromParam] params string[][] param)
        {
            //string[] @params = param.Split(ParamsSeparatorChar);
            var result = param[0];
            param.Skip(1).ForEach(a => result = result.Intersect(a).ToArray());
            return string.Join(ListSeparator, result);
        }
        /// <summary>
        /// returns minimal value from sequence of double values
        /// </summary>
        /// <param name="ints">values</param>
        /// <returns>minimal value</returns>
        [DslMethod("min")]
        public static string Min([FromParam] double[] ints)
        {
            return ints.Any() ? ints.Min().ToString(CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// returns maximum value from sequence of double values
        /// </summary>
        /// <param name="ints">values</param>
        /// <returns>maximum value</returns>
        [DslMethod("max")]
        public static string Max([FromParam] double[] ints)
        {
            return ints.Any() ? ints.Max().ToString(CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// returns sum of double items in sequence
        /// </summary>
        /// <param name="ints">items</param>
        /// <returns>sum of items</returns>
        [DslMethod("sum")]
        public static string Sum([FromParam] double[] ints)
        {
            return ints.Any() ? ints.Sum().ToString(CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// Returns minimal date from sequence of dates
        /// </summary>
        /// <param name="dates">sequence of dates</param>
        /// <returns>minimal date</returns>
        [DslMethod("mindate")]
        public static string Mindate([FromParam] string[] dates)
        {
            var result = dates
                .Select(a => ParseDateOrInvalid(a, DateTime.MinValue))
                .Where(a => !DateTime.MinValue.Equals(a)).ToArray();

            if (result.Any())
                return result.Min().ToString(SwDateFormat, CultureInfo.InvariantCulture);
            return null;
        }

        /// <summary>
        /// Returns maximal date from sequence of dates
        /// </summary>
        /// <param name="dates">sequence of dates</param>
        /// <returns>maximal date</returns>
        [DslMethod("maxdate")]
        public static string Maxdte([FromParam] string[] dates)
        {
            var result = dates
                .Select(a => ParseDateOrInvalid(a, DateTime.MaxValue))
                .Where(a => !DateTime.MaxValue.Equals(a)).ToArray();

            if (result.Any())
                return result.Max().ToString(SwDateFormat, CultureInfo.InvariantCulture);
            return null;
        }
        /// <summary>
        /// Return time from unixdate (seconds from 1970)
        /// </summary>
        /// <param name="seconds">seconds from 1970 </param>
        /// <returns>date in form "yyyy-MM-dd"</returns>
        [DslMethod("unixdate")]
        public static string Unixdate([FromParam] int seconds)
        {
            if (seconds <= 0)
                return DateTime.Now.ToString(SwDateFormat, CultureInfo.InvariantCulture);
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(seconds)
                .ToString(SwDateFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Filters key-value pairs by their value
        /// </summary>
        /// <param name="params">list of keyvalue pairs</param>
        /// <param name="where">value to compare</param>
        /// <returns>resulting sequence of keys</returns>
        [DslMethod("where")]
        [OnErrorNull]
        public static string Where([FromParam(0)] string[] @params, [FromParam(1)] string where)
        {
            return string.Join(ListSeparatorChar.ToString(),
                @params
                    .Select(a =>
                    {
                        var spl = a.Split(KeyValSeparatorChar);
                        return new { Key = spl[0], Value = spl[1] };
                    })
                    .Where(a => a.Value.Equals(where))
                    .Select(a => a.Key));
        }

        /// <summary>
        /// Checks if all items in sequence are equal to operand
        /// </summary>
        /// <param name="params">sequence</param>
        /// <param name="all">operand </param>
        /// <returns>noolean result</returns>
        [DslMethod("all")]
        [OnErrorNull]
        public static string All([FromParam(0)] string[] @params, [FromParam(1)] string all)
        {
            return @params.All(a => a.Trim().ToLower().Equals(all.ToLower())).ToString();
        }

        /// <summary>
        /// Checks if any of items in sequence is equal to operand
        /// </summary>
        /// <param name="params">sequence</param>
        /// <param name="any">operand </param>
        /// <returns>boolean result</returns>
        [DslMethod("any")]
        [OnErrorNull]
        public static string Any([FromParam(0)] string[] @params, [FromParam(1)] string any)
        {
            return @params.Any(a => a.Trim().ToLower().Equals(any.ToLower())).ToString();
        }

        /// <summary>
        /// Evaluates condition and returns one of the next operands depending on evaluation result
        /// </summary>
        /// <param name="booleanValue">condition (boolean)</param>
        /// <param name="trueVal">positive result</param>
        /// <param name="falseVal">negative result</param>
        /// <returns>result</returns>
        [DslMethod("condition")]
        public static string Condition([FromParam(0)] string booleanValue, [FromParam(1)] string trueVal,
            [FromParam(2)] string falseVal)
        {
            bool val;
            var success = bool.TryParse(booleanValue, out val);
            return val && success ? trueVal : falseVal;
        }
        /// <summary>
        /// Parse enum from assembly
        /// </summary>
        /// <param name="enumName"></param>
        /// <param name="assemblyName"></param>
        /// <returns></returns>
        [DslMethod("enum")]
        public static string EnumValues([FromParam(0)] string enumName, [FromParam(1)] string assemblyName)
        {
            var assemmbly = AppDomain.CurrentDomain.Load(assemblyName);
            var enumType = assemmbly.GetType(enumName);
            if (enumType == null)
                return null;

            return string.Join(ListSeparator, Enum.GetNames(enumType));
        }

        /// <summary>
        /// Compares 2 operands
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        [DslMethod("equals")]
        public static string Equals([FromParam(0)] string first, [FromParam(1)] string second)
        {
            return string.Equals(first, second).ToString();
        }

    }
}
