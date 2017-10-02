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

        public static Func<IReadOnlyList<Assembly>> AssemblyResolver = () =>
        {
            Assembly current = Assembly.GetExecutingAssembly();
            return AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetReferencedAssemblies(), (a, r) => new {a, r})
                .Where((a) => a.r.FullName == current.FullName || a.a == current)
                .Select(a=>a.a)
                .Distinct()
                .ToList();
        };

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

        public static string Expand(string value, bool recursive = false)
        {
            return recursive ? ExpandRecursive(value) : ExpandOne(value);
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

        public static object Parse(string str, bool forceList = false)
        {
            if (str == null)
                return null;

            var parts = IsLiteral(str) ? new[] { str.Trim('\'') } : str.Split(ListSeparatorChar);

            if (parts.Length == 1 && !forceList)
                return string.IsNullOrEmpty(parts[0]) ? null : Convert(parts[0]) ?? str;

            var result = parts.Where(a => !string.IsNullOrEmpty(a)).Select(a => Convert(a) ?? a).ToArray();
            return result.Any() ? result.ToArrayOfType(result.First().GetType()) : null;
        }

        public static bool TryEvaluate<T>(string value, out T result, bool recursive=false)
        {
            try
            {
                var res = Evaluate(value,recursive);
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

        public static object Evaluate(string value,bool recursive = false)
        {
            return Parse(Expand(value,recursive));
        }

        //public static T Evaluate<T>(string value, T @default)
        //{
        //    return TryEvaluate(value, out T res) ? res : @default;
        //}

        //public static object Evaluate(string value,bool recursive = false)
        //{
        //    return Parse(Expand(value,recursive));
        //}
        //public static string EvaluateToString(string value)
        //{
        //    return TryEvaluate(value, out string res) ? res : value;
        //}

        internal static object ResolveCore(Type t)
        {
            return CurlyContext.GetService(t) ?? Resolve(t);
        }

    }
}