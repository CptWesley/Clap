using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
            HashSet<PropertyInfo> setOptions = new HashSet<PropertyInfo>();

            PropertyInfo property = null;
            string option = null;
            foreach (string arg in args)
            {
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
                            // Handle unknown option?
                        }
                    }
                    else
                    {
                        // Handle non-option arguments.
                    }
                }
                else if (property != null)
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

            if (property != null)
            {
                throw new Exception($"Option '{option}' required a value.");
            }

            return result;
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
            else if (type.IsEnum) return Enum.Parse(type, value);

            throw new Exception($"There is no support for arguments of type '{type.Name}'.");
        }
    }
}
