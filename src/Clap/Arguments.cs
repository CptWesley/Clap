using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

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
                    else if (unnamedIndex < unnamedOptions.Length)
                    {
                        property = unnamedOptions[unnamedIndex++];
                        i--;
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

        /// <summary>
        /// Generate a help page for the given type.
        /// </summary>
        /// <typeparam name="T">Type of which to create a help page.</typeparam>
        /// <returns>A help page as a string.</returns>
        public static string GetHelpPage<T>()
            => GetHelpPage(typeof(T));

        /// <summary>
        /// Generate a help page for the given type.
        /// </summary>
        /// <typeparam name="T">Type of which to create a help page.</typeparam>
        /// <param name="programName">The name of the program in the console.</param>
        /// <returns>A help page as a string.</returns>
        public static string GetHelpPage<T>(string programName)
            => GetHelpPage(typeof(T), programName);

        /// <summary>
        /// Generate a help page for the given type.
        /// </summary>
        /// <param name="type">Type of which to create a help page.</param>
        /// <returns>A help page as a string.</returns>
        public static string GetHelpPage(Type type)
            => GetHelpPage(type, Environment.GetCommandLineArgs()[0]);

        /// <summary>
        /// Generate a help page for the given type.
        /// </summary>
        /// <param name="type">Type of which to create a help page.</param>
        /// <param name="programName">The name of the program in the console.</param>
        /// <returns>A help page as a string.</returns>
        public static string GetHelpPage(Type type, string programName)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            PropertyInfo[] properties = type.GetProperties();
            List<string> options = new List<string>();
            List<string> arguments = new List<string>();
            List<string> descriptions = new List<string>();
            List<string> unnamedArgs = new List<string>();

            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];
                UnnamedAttribute unnamed = property.GetCustomAttribute<UnnamedAttribute>();
                if (unnamed != null)
                {
                    unnamedArgs.Add(GetArgumentName(property));
                }
                else
                {
                    StringBuilder aliases = new StringBuilder();

                    aliases.Append($"-{property.Name}");
                    AliasAttribute aliasAttribute = property.GetCustomAttribute<AliasAttribute>();
                    if (aliasAttribute != null)
                    {
                        foreach (string alias in aliasAttribute.Aliases)
                        {
                            aliases.Append($" -{alias}");
                        }
                    }

                    DescriptionAttribute descAttr = property.GetCustomAttribute<DescriptionAttribute>();

                    options.Add(aliases.ToString());
                    arguments.Add(GetArgumentName(property));
                    descriptions.Add(descAttr == null ? string.Empty : descAttr.Text);
                }
            }

            int optionsLength = options.Max(x => x.Length);
            int argumentsLength = arguments.Max(x => x.Length);

            StringBuilder help = new StringBuilder();
            DescriptionAttribute classDescAttr = type.GetCustomAttribute<DescriptionAttribute>();
            if (classDescAttr != null && classDescAttr.Text != null)
            {
                help.AppendLine(classDescAttr.Text);
            }

            help.AppendLine($"Usage: {programName} [options] {string.Join(" ", unnamedArgs.Select(x => $"[{x}]"))}".Trim());
            help.AppendLine("Options:");

            for (int i = 0; i < options.Count; i++)
            {
                help.Append("  ").AppendLine($"{GetTextWithFiller(options[i], optionsLength + 4)}{GetTextWithFiller(arguments[i], argumentsLength + 4)}{descriptions[i]}".Trim());
            }

            return help.ToString();
        }

        private static string GetTextWithFiller(string text, int size)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(text);

            for (int i = 0; i < size - text.Length; i++)
            {
                sb.Append(' ');
            }

            return sb.ToString();
        }

        private static string GetArgumentName(PropertyInfo property)
        {
            ArgumentNameAttribute attribute = property.GetCustomAttribute<ArgumentNameAttribute>();
            if (IsArrayLike(property))
            {
                Type type = GetArrayLikeElementType(property);

                return $"{GetArgumentName(type, attribute, "1")} {GetArgumentName(type, attribute, "2")} ...";
            }

            return GetArgumentName(property.PropertyType, attribute, string.Empty);
        }

        private static string GetArgumentName(Type type, ArgumentNameAttribute attribute, string suffix)
        {
            if (type == typeof(bool))
            {
                return string.Empty;
            }

            if (type.IsEnum)
            {
                return string.Join("|", Enum.GetNames(type));
            }

            if (attribute != null)
            {
                return $"<{attribute.Name}{suffix}>";
            }

            return $"<{type.Name}{suffix}>";
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
