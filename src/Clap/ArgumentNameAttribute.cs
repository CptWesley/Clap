using System;

namespace Clap
{
    /// <summary>
    /// Attribute used for setting custom defined names on arguments.
    /// </summary>
    /// <seealso cref="Attribute" />
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class ArgumentNameAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentNameAttribute"/> class.
        /// </summary>
        /// <param name="name">The argument name.</param>
        public ArgumentNameAttribute(string name)
            => Name = name;

        /// <summary>
        /// Gets the argument name.
        /// </summary>
        public string Name { get; }
    }
}
