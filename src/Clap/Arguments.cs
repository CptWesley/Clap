using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Clap
{
    /// <summary>
    /// Class containing static functions for parsing arguments.
    /// </summary>
    public static class Arguments
    {
        /// <summary>
        /// Parses the specified arguments.
        /// </summary>
        /// <typeparam name="T">Type of object to parse the arguments to.</typeparam>
        /// <param name="args">The arguments.</param>
        /// <returns>Returns the parsed arguments object.</returns>
        public static T Parse<T>(string args)
            => Parse<T>(SplitArgs(args));

        /// <summary>
        /// Parses the specified arguments.
        /// </summary>
        /// <typeparam name="T">Type of object to parse the arguments to.</typeparam>
        /// <param name="args">The arguments.</param>
        /// <returns>Returns the parsed arguments object.</returns>
        public static T Parse<T>(string[] args)
            => Parse<T>(args, CultureInfo.CurrentCulture);

        /// <summary>
        /// Parses the specified arguments.
        /// </summary>
        /// <typeparam name="T">Type of object to parse the arguments to.</typeparam>
        /// <param name="args">The arguments.</param>
        /// <param name="culture">Culture used for parsing arguments.</param>
        /// <returns>Returns the parsed arguments object.</returns>
        public static T Parse<T>(string args, CultureInfo culture)
            => Parse<T>(SplitArgs(args), culture);

        /// <summary>
        /// Parses the specified arguments.
        /// </summary>
        /// <typeparam name="T">Type of object to parse the arguments to.</typeparam>
        /// <param name="args">The arguments.</param>
        /// <param name="culture">Culture used for parsing arguments.</param>
        /// <returns>Returns the parsed arguments object.</returns>
        public static T Parse<T>(string[] args, CultureInfo culture)
            => (T)Parse(typeof(T), args, culture);

        /// <summary>
        /// Parses the specified arguments.
        /// </summary>
        /// <param name="type">Type of object to parse the arguments to.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>Returns the parsed arguments object.</returns>
        public static object Parse(Type type, string args)
            => Parse(type, SplitArgs(args), CultureInfo.CurrentCulture);

        /// <summary>
        /// Parses the specified arguments.
        /// </summary>
        /// <param name="type">Type of object to parse the arguments to.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>Returns the parsed arguments object.</returns>
        public static object Parse(Type type, string[] args)
            => Parse(type, args, CultureInfo.CurrentCulture);

        /// <summary>
        /// Parses the specified arguments.
        /// </summary>
        /// <param name="type">Type of object to parse the arguments to.</param>
        /// <param name="args">The arguments.</param>
        /// <param name="culture">Culture used for parsing arguments.</param>
        /// <returns>Returns the parsed arguments object.</returns>
        public static object Parse(Type type, string args, CultureInfo culture)
            => Parse(type, SplitArgs(args), culture);

        /// <summary>
        /// Parses the specified arguments.
        /// </summary>
        /// <param name="type">Type of object to parse the arguments to.</param>
        /// <param name="args">The arguments.</param>
        /// <param name="culture">Culture used for parsing arguments.</param>
        /// <returns>Returns the parsed arguments object.</returns>
        public static object Parse(Type type, string[] args, CultureInfo culture)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            object result = Activator.CreateInstance(type);
            Dictionary<string, PropertyInfo> options = CreateAliasMap(type);
            PropertyInfo[] unnamedOptions = CreateUnnamedMap(type);
            HashSet<PropertyInfo> setOptions = new HashSet<PropertyInfo>();

            PropertyInfo property = null;
            List<object> parsedArgs = new List<object>();
            string option = null;
            int unnamedIndex = 0;
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (property == null)
                {
                    if (arg[0] == '-')
                    {
                        option = arg.Substring(1).ToUpperInvariant();
                        if (options.TryGetValue(option, out property))
                        {
                            if (setOptions.Contains(property))
                            {
                                throw new Exception($"Option '{option}' can not be set twice.");
                            }

                            if (property.PropertyType == typeof(bool))
                            {
                                setOptions.Add(property);
                                property.SetValue(result, true);
                                property = null;
                            }
                        }
                        else
                        {
                            throw new Exception($"Option '{option}' was not recognized.");
                        }
                    }
                    else
                    {
                        if (unnamedIndex < unnamedOptions.Length)
                        {
                            property = unnamedOptions[unnamedIndex++];
                            i--;
                        }
                        else
                        {
                            // Handle too many unnamed arguments.
                        }
                    }
                }
                else if (property != null)
                {
                    if (IsArrayLike(property))
                    {
                        if (arg[0] == '-')
                        {
                            EndArray(property, result, parsedArgs);
                            property = null;
                            i--;
                        }
                        else
                        {
                            Type elementType = GetArrayLikeElementType(property);
                            object parsedArg = ParseValue(elementType, arg, culture);
                            parsedArgs.Add(parsedArg);
                        }
                    }
                    else
                    {
                        if (arg[0] == '-')
                        {
                            throw new Exception($"Option '{option}' required a value.");
                        }
                        else
                        {
                            object value = ParseValue(property.PropertyType, arg, culture);
                            property.SetValue(result, value);
                            property = null;
                        }
                    }
                }
            }

            if (property != null)
            {
                if (IsArrayLike(property))
                {
                    EndArray(property, result, parsedArgs);
                }
                else
                {
                    throw new Exception($"Option '{option}' required a value.");
                }
            }

            return result;
        }

        private static Type GetArrayLikeElementType(PropertyInfo property)
        {
            Type type = property.PropertyType;
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            return type.GetGenericArguments()[0];
        }

        private static bool IsArrayLike(PropertyInfo property)
        {
            Type type = property.PropertyType;
            return type.IsArray || type.GetInterface("ICollection`1") != null;
        }

        private static void EndArray(PropertyInfo property, object result, List<object> parsedArgs)
        {
            object value;
            if (property.PropertyType.IsArray)
            {
                Type elementType = GetArrayLikeElementType(property);
                value = Array.CreateInstance(elementType, parsedArgs.Count);
                Array array = value as Array;

                for (int i = 0; i < array.Length; i++)
                {
                    array.SetValue(parsedArgs[i], i);
                }
            }
            else
            {
                value = Activator.CreateInstance(property.PropertyType);
                MethodInfo add = property.PropertyType.GetMethod("Add");

                foreach (object parsedArg in parsedArgs)
                {
                    add.Invoke(value, new[] { parsedArg });
                }
            }

            property.SetValue(result, value);
            parsedArgs.Clear();
        }

        private static string[] SplitArgs(string args)
            => args?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        private static Dictionary<string, PropertyInfo> CreateAliasMap(Type type)
        {
            Dictionary<string, PropertyInfo> options = new Dictionary<string, PropertyInfo>();
            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                AddProperty(options, property.Name, property);

                AliasAttribute aliasAttribute = property.GetCustomAttribute<AliasAttribute>();
                if (aliasAttribute != null)
                {
                    foreach (string alias in aliasAttribute.Aliases)
                    {
                        AddProperty(options, alias, property);
                    }
                }
            }

            return options;
        }

        private static PropertyInfo[] CreateUnnamedMap(Type type)
            => type.GetProperties()
            .Where(x => x.GetCustomAttribute<UnnamedAttribute>() != null)
            .OrderBy(x => x.GetCustomAttribute<UnnamedAttribute>().Order)
            .ToArray();

        private static void AddProperty(Dictionary<string, PropertyInfo> options, string name, PropertyInfo property)
        {
            string key = name.ToUpperInvariant();
            if (options.ContainsKey(key))
            {
                throw new Exception($"Key '{name}' is used multiple times.");
            }

            options.Add(key, property);
        }

        [SuppressMessage("Layout", "SA1503", Justification = "Readability.")]
        private static object ParseValue(Type type, string value, CultureInfo culture)
        {
            if (type == typeof(string)) return value;
            else if (type == typeof(char)) return char.Parse(value);
            else if (type == typeof(byte)) return byte.Parse(value, culture);
            else if (type == typeof(sbyte)) return sbyte.Parse(value, culture);
            else if (type == typeof(short)) return short.Parse(value, culture);
            else if (type == typeof(ushort)) return ushort.Parse(value, culture);
            else if (type == typeof(int)) return int.Parse(value, culture);
            else if (type == typeof(uint)) return uint.Parse(value, culture);
            else if (type == typeof(long)) return long.Parse(value, culture);
            else if (type == typeof(ulong)) return ulong.Parse(value, culture);
            else if (type == typeof(float)) return float.Parse(value, culture);
            else if (type == typeof(double)) return double.Parse(value, culture);
            else if (type.IsEnum) return Enum.Parse(type, value, true);

            throw new Exception($"There is no support for arguments of type '{type.Name}'.");
        }
    }
}
