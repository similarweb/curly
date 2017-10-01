using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Curly.Attributes;
using Curly.Binding;
using Curly.Contract;
using Curly.Helpers;
using Newtonsoft.Json;

namespace Curly
{
    public delegate string CurlyMethod(string value, string param, string defVal);

    internal delegate string InternalCurlyMethod(string value, string param, string defVal);

    public delegate object CurlyConverter(string value);

    public static class CurlyDsl
    {
        public static readonly string AnyDate = "any";
        public static readonly string Unlimited = "unlimited";
        public static readonly string SwDateFormat = "yyyy-MM-dd";
        public static readonly string SwShortDateFormat = "yyyy-MM";
        public static readonly string SwSuperShortDateFormat = "yyyy-M";
        public static readonly char ParamsSeparatorChar = ',';
        public static readonly char ListSeparatorChar = ';';
        public static readonly char KeyValSeparatorChar = '|';
        public static readonly string ListSeparator = ListSeparatorChar.ToString();
        private static readonly Regex PatMaRegex;

        public static ICurlyMethodBinder MethodBinder = new DefaultCurlyMethodBinder();
        public static Func<Type, object> Resolve = type => Activator.CreateInstance(type);
        public static Action<string, Exception> ErrorLogger = (mes, ex) => Console.WriteLine($"{mes}:{ex?.Message}");

        public static Func<IReadOnlyList<Assembly>> AssemblyResolver = () => AppDomain
            .CurrentDomain
            .GetAssemblies();
            //.Where(a => a.FullName.ToLowerInvariant().StartsWith("similarweb")).ToList();

        private static Dictionary<string, InternalCurlyMethod> _newResolvers;
        private static IList<CurlyConverter> _newConverters;

        static CurlyDsl()
        {
            PatMaRegex = new Regex(
                @"(?x)
            { (?'name'" + string.Join(" | ", InternalCurlyMethods.Keys) + @") :?
            (?'value'
                (?>  
                    (?: [^{}]+ | { (?<open>) | } (?<-open>))
                )*                
                (?(open)(?!))
            ) 
            }", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        }

        private static Dictionary<string, InternalCurlyMethod> InternalCurlyMethods => _newResolvers ?? (_newResolvers = LoadMethodResolvers());

        private static IList<CurlyConverter> Converters => _newConverters ?? (_newConverters = LoadConverters());

        private static Dictionary<string, InternalCurlyMethod> LoadMethodResolvers()
        {
            var currentMethodName = "";
            try
            {
                return AssemblyResolver().SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch (Exception e)
                    {
                        ErrorLogger("Curly loading", e);
                        return Enumerable.Empty<Type>().ToArray();
                    }
                })
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(method => method.GetCustomAttribute<DslMethodAttribute>() != null && method.ReturnType == typeof(string))
                .ToDictionary<MethodInfo, string, InternalCurlyMethod>(a => currentMethodName = a.GetCustomAttribute<DslMethodAttribute>().Name, m =>
                {
                    var dslm = MethodBinder.Bind(m);
                    return (a, b, c) => dslm(a, b, c);
                });
            }
            catch (ArgumentException)
            {
                throw new ArgumentException("A method with name '" + currentMethodName + "' already seen");
            }
        }

        private static List<CurlyConverter> LoadConverters()
        {
            return AssemblyResolver().SelectMany(a =>
            {
                try
                {
                    return a.GetTypes();
                }
                catch (Exception e)
                {
                    ErrorLogger("Curly loading", e);
                    return Enumerable.Empty<Type>().ToArray();
                }
            })
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(method => method.GetCustomAttribute<CurlyInterpreterAttribute>() != null)
            .OrderBy(method => method.GetCustomAttribute<CurlyInterpreterAttribute>().Order)
            .Select<MethodInfo, CurlyConverter>(m =>
            {
                var dslm = MethodBinder.BindConverter(m);
                return a => dslm(a);
            })
            .ToList();
        }

        private static bool IsLiteral(string str)
        {
            return str.StartsWith("'") && str.EndsWith("'");
        }

        [DslMethod("null")]
        public static string Null()
        {
            return null;
        }

        [DslMethod("now")]
        public static string Now()
        {
            return DateTime.Now.ToString(SwDateFormat);
        }


        [DslMethod("not")]
        public static string Not([FromParam] bool param)
        {
            return (!param).ToString();
        }

        [DslMethod("first")]
        public static string First([FromParam] string[] items)
        {
            return items.FirstOrDefault();
        }

        [DslMethod("last")]
        public static string Last([FromParam] string[] items)
        {
            return items.LastOrDefault();
        }

        [DslMethod("concat")]
        public static string Concat([FromParam] params string[][] param)
        {
            //string[] @params = param.Split(ParamsSeparatorChar);
            var result = param[0];
            param.Skip(1).ForEach(a => result = result.Concat(a).ToArray());
            return string.Join(ListSeparator, result);
        }

        [DslMethod("distinct")]
        public static string Distinct([FromParam] string[] param)
        {
            return param != null ? string.Join(ListSeparator, param.Distinct()) : null;
        }

        [DslMethod("except")]
        public static string Except([FromParam(0)] string[] param, [FromParam(1)] string[] except)
        {
            var result = param.Except(except);
            return string.Join(ListSeparator, result);
        }

        [DslMethod("intersect")]
        public static string Intersect([FromParam] params string[][] param)
        {
            //string[] @params = param.Split(ParamsSeparatorChar);
            var result = param[0];
            param.Skip(1).ForEach(a => result = result.Intersect(a).ToArray());
            return string.Join(ListSeparator, result);
        }

        [DslMethod("min")]
        public static string Min([FromParam] double[] ints)
        {
            return ints.Any() ? ints.Min().ToString(CultureInfo.InvariantCulture) : null;
        }

        [DslMethod("max")]
        public static string Max([FromParam] double[] ints)
        {
            return ints.Any() ? ints.Max().ToString(CultureInfo.InvariantCulture) : null;
        }

        [DslMethod("sum")]
        public static string Sum([FromParam] double[] ints)
        {
            return ints.Any() ? ints.Sum().ToString(CultureInfo.InvariantCulture) : null;
        }

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

        [DslMethod("unixdate")]
        public static string Unixdate([FromParam] int seconds)
        {
            if (seconds <= 0)
                return DateTime.Now.ToString(SwDateFormat, CultureInfo.InvariantCulture);
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(seconds)
                .ToString(SwDateFormat, CultureInfo.InvariantCulture);
        }


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

        [DslMethod("all")]
        [OnErrorNull]
        public static string All([FromParam(0)] string[] @params, [FromParam(1)] string all)
        {
            return @params.All(a => a.Trim().ToLower().Equals(all.ToLower())).ToString();
        }

        [DslMethod("any")]
        [OnErrorNull]
        public static string Any([FromParam(0)] string[] @params, [FromParam(1)] string any)
        {
            return @params.Any(a => a.Trim().ToLower().Equals(any.ToLower())).ToString();
        }

        [DslMethod("condition")]
        public static string Condition([FromParam(0)] string booleanValue, [FromParam(1)] string trueVal,
            [FromParam(2)] string falseVal)
        {
            bool val;
            var success = bool.TryParse(booleanValue, out val);
            return val && success ? trueVal : falseVal;
        }

        [DslMethod("enum")]
        public static string EnumValues([FromParam(0)] string enumName, [FromParam(1)] string assemblyName)
        {
            var assemmbly = AppDomain.CurrentDomain.Load(assemblyName);
            var enumType = assemmbly.GetType(enumName);
            if (enumType == null)
                return null;

            return string.Join(ListSeparator, Enum.GetNames(enumType));
        }

        [DslMethod("equals")]
        public static string Equals([FromParam(0)] string first, [FromParam(1)] string second)
        {
            return string.Equals(first, second).ToString();
        }

        [DslMethod("and")]
        public static string And([FromParam(0)] string first, [FromParam(1)] string second)
        {
            return (
                bool.TryParse(first, out bool firstBool) && firstBool &&
                bool.TryParse(second, out bool seconBool) && seconBool).ToString();
        }

        [DslMethod("or")]
        public static string Or([FromParam(0)] string first, [FromParam(1)] string second)
        {
            return  (
                bool.TryParse(first, out bool firstBool) && firstBool ||
                bool.TryParse(second, out bool seconBool) && seconBool).ToString();
        }

        internal static DateTime ParseDateOrInvalid(string str, DateTime def)
        {
            return !TryParseDate(str, out DateTime dt) ? def : dt;
        }

        internal static bool TryParseDate(string str, out DateTime dt)
        {
            dt = DateTime.MaxValue;
            return str.Trim().ToLower().Equals(AnyDate) ||
                DateTime.TryParseExact(str, SwDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt) || 
                DateTime.TryParseExact(str, SwShortDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None,out dt) || 
                DateTime.TryParseExact(str, SwSuperShortDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None,out dt);
        }

        private static object Convert(string str)
        {
            return Converters
                .Select(a => a(str))
                .FirstOrDefault(a => a!= null) ?? str;
        }

        [CurlyInterpreter(0)]
        public static object PrimitiveInterpreter(string str)
        {
            var jsonRegex = new Regex("\\{\".*\":\".*\"(,\".*\":\".*\")?\\}");
            return
                bool.TryParse(str, out bool bres) ? bres :
                str.Trim().ToLower().Equals(Unlimited) ? (long)int.MaxValue :
                long.TryParse(str, out long lres) ? lres :
                double.TryParse(str, out double dres) ? dres :
                TryParseDate(str, out DateTime dt) ? (object)dt :
                jsonRegex.IsMatch(str) ? JsonConvert.DeserializeObject<Dictionary<string, string>>(str) : null;
        }

        public static string ExpandRecursive(string value)
        {
            while (true)
            {
                string result = ExpandOne(value);
                if (result == value)
                    return result;
                value = result;
            }
        }
        public static string ExpandOne(string value)
        {
            MatchEvaluator matchEvaluator = a => string.Empty;
            matchEvaluator = match =>
            {
                var parts = match.Value.Substring(1, match.Value.Length - 2).Split(new[] { ':' }, 2);
                if (parts.Length > 1)
                    parts[1] = parts[1].Trim();
                return InternalCurlyMethods[parts[0]](value,
                    parts.Count() > 1 ? PatMaRegex.Replace(parts[1], matchEvaluator) : value,
                    parts.Count() > 2 ? PatMaRegex.Replace(parts[2], matchEvaluator) : null);
            };
            if (value == null)
                return null;
            return PatMaRegex.Replace(value, matchEvaluator);
        }

        public static object Typify(string str, bool forceList = false)
        {
            if (str == null)
                return null;

            var parts = IsLiteral(str) ? new[] { str.Trim('\'') } : str.Split(ListSeparatorChar);

            if (parts.Length == 1 && !forceList)
                return string.IsNullOrEmpty(parts[0]) ? null : Convert(parts[0]) ?? str;

            var result = parts.Where(a => !string.IsNullOrEmpty(a)).Select(a => Convert(a) ?? a).ToArray();
            return result.Any() ? result.ToArrayOfType(result.First().GetType()) : null;
        }

        public static bool TryEvaluate<T>(string value, out T result)
        {
            try
            {
                var res = Typify(ExpandOne(value));
                result = (T)System.Convert.ChangeType(res, typeof(T));
                return true;
            }
            catch (Exception e)
            {
                ErrorLogger($"Cant convert {value} to type {typeof(T).FullName}", e);
                result = default(T);
                return false;
            }
        }

        public static T Evaluate<T>(string value, T @default)
        {
            return TryEvaluate(value, out T res) ? res : @default;
        }

        public static string StringValue(string value)
        {
            return TryEvaluate(value, out string res) ? res : value;
        }

        public static object Evaluate(string value)
        {
            return Typify(ExpandOne(value));
        }

        internal static object ResolveCore(Type t)
        {
            return CurlyContext.GetService(t) ?? Resolve(t);
        }

    }
}