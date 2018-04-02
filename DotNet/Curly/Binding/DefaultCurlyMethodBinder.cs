using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Curly.Attributes;
using Curly.Contract;
using Curly.Helpers;

namespace Curly.Binding
{
    public class DefaultCurlyMethodBinder : ICurlyMethodBinder
    {
        public delegate object ParameterProvider(string value, string param, string def);
        public delegate object ConverterParameterProvider(string value);

        public CurlyConverter BindConverter(MethodInfo methodInfo)
        {
            if (methodInfo.GetParameters().Length < 1 ||
                !typeof(string).IsAssignableFrom(methodInfo.GetParameters().First().ParameterType))
            {
                Console.WriteLine("Curly converter binding failed: wrong parameter count or parameter types in method marked by CurlyInterpreter");
                return null;
            }

            IEnumerable<ConverterParameterProvider> invocators =
                methodInfo.GetParameters().Take(1).Select<ParameterInfo, ConverterParameterProvider>(a => (value) => value)
                .Concat(methodInfo.GetParameters().Skip(1).Select<ParameterInfo, ConverterParameterProvider>(a => (x) => CurlyDsl.ResolveCore(a.ParameterType)));

            return (value) =>
            {
                try
                {
                    return methodInfo.Invoke(null, invocators.Select(a =>
                    {
                        try
                        {
                            return a(value);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(
                                $"Curly interpreter {methodInfo.Name} failed:V={value ?? "null"} calling converter.{ e.InnerException ?? e}");
                            return null;
                        }
                    }).ToArray());

                }
                catch (Exception e)
                {
                    Console.WriteLine($"Curly interpreter {methodInfo.Name} failed:V={value ?? "none"}, E={e.InnerException ?? e}");
                    return null;
                }
            };
        }

        public CurlyMethod Bind(MethodInfo methodInfo)
        {
            IEnumerable<ParameterProvider> invocators = methodInfo.GetParameters()
                .Select<ParameterInfo, ParameterProvider>(a =>
                {
                    Type pType = a.ParameterType;
                    //if (pType == typeof (IComponentConfiguration))
                    //    return (config, key, value, param, defVal) => config;
                    FromParamAttribute fromParamAttribute = a.GetCustomAttribute<FromParamAttribute>();
                    if (fromParamAttribute != null)
                    {
                        //params case
                        if (a.GetCustomAttribute<ParamArrayAttribute>() != null)
                        {
                            if (fromParamAttribute.Index.HasValue)
                                throw new ArgumentException("indexed property access attribute is not supported when parameter has 'params' modifier");

                            pType = pType.GetElementType();
                            if (pType.IsArray)
                            {
                                //array of arrays
                                pType = pType.GetElementType();
                                return (value, param, defVal) => param
                                    .Split(CurlyDsl.ParamsSeparatorChar)
                                    .NotNullOrEmpty()
                                    .Select(b => b.Split(CurlyDsl.ListSeparatorChar)
                                        .NotNullOrEmpty()
                                        .Select(c => ConvertToType(c, pType))
                                        .NotNull()
                                        .ToArrayOfType(pType))
                                    .ToArrayOfType(pType.MakeArrayType());
                            }
                            else
                            {
                                //single array splitted by array sep char
                                return (value, param, defVal) => param
                                    .Split(CurlyDsl.ParamsSeparatorChar)
                                    .NotNullOrEmpty()
                                    .Select(b => ConvertToType(b, pType))
                                    .NotNull()
                                    .ToArrayOfType(pType);
                            }

                        }
                        else //regular case
                        {
                            if (pType.IsArray)
                            {
                                pType = pType.GetElementType();
                                if (fromParamAttribute.Index.HasValue)
                                {
                                    return (value, param, defVal) => param
                                        .Split(CurlyDsl.ParamsSeparatorChar)[fromParamAttribute.Index.Value]
                                        .Split(CurlyDsl.ListSeparatorChar)
                                        .NotNullOrEmpty()
                                        .Select(b => ConvertToType(b, pType))
                                        .NotNull()
                                        .ToArrayOfType(pType);
                                }
                                else
                                {
                                    return (value, param, defVal) => param
                                        .Split(CurlyDsl.ListSeparatorChar)
                                        .NotNullOrEmpty()
                                        .Select(b => ConvertToType(b, pType))
                                        .NotNull()
                                        .ToArrayOfType(pType);
                                }

                            }
                            else
                            {
                                if (fromParamAttribute.Index.HasValue)
                                {
                                    return (value, param, defVal) => ConvertToType(param
                                        .Split(CurlyDsl.ParamsSeparatorChar)[fromParamAttribute.Index.Value], pType);
                                }
                                else
                                {
                                    return (value, param, defVal) => ConvertToType(param, pType);
                                }
                            }
                        }
                    }
                    FromDefaultAttribute fromDefaultAttribute = a.GetCustomAttribute<FromDefaultAttribute>();
                    if (fromDefaultAttribute != null)
                        return (value, param, defVal) => ConvertToType(defVal, pType);
                    FromValueAttribute fromValueAttribute = a.GetCustomAttribute<FromValueAttribute>();
                    if (fromValueAttribute != null)
                        return (value, param, defVal) => ConvertToType(value, pType);

                    return (value, param, defVal) => CurlyDsl.ResolveCore(pType);

                });

            return (value, param, defVal) =>
            {
                try
                {
                    return (string)methodInfo.Invoke(null, invocators.Select(a =>
                    {
                        try
                        {
                            return a(value, param, defVal);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(
                                $"Dsl method {methodInfo.Name} failed:V={value ?? "null"},P={param ?? "null"} calling provider. E={e.InnerException ?? e}");
                            return null;
                        }
                    }).ToArray());

                }
                catch (Exception e)
                {
                    Console.WriteLine($"Dsl method {methodInfo.Name} failed:V={value ?? "null"},P={param ?? "null"},E={e.InnerException ?? e}");
                    return null;
                }
            };
        }

        object ConvertToType(string o, Type to)
        {
            object src = o;
            if (to == typeof(DateTime))
            {
                return CurlyDsl.ParseDateOrInvalid(o, DateTime.MinValue);
            }

            if (CurlyDsl.Unlimited.Equals(o))
            {
                src = Int32.MaxValue;
            }
            try
            {
                return Convert.ChangeType(src, to);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error converting {o} to type {to.FullName}. E={e}");
                return null;
            }
        }

    }
}