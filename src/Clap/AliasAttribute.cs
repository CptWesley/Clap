using System;
using System.Collections.Generic;

namespace Clap
{
    /// <summary>
    /// Attribute for indicating different aliases for argument options.
    /// </summary>
    /// <seealso cref="Attribute" />
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class AliasAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AliasAttribute"/> class.
        /// </summary>
        /// <param name="aliases">The aliases.</param>
        public AliasAttribute(params string[] aliases)
            => Aliases = aliases;

        /// <summary>
        /// Gets the aliases.
        /// </summary>
        public IReadOnlyList<string> Aliases { get; }
    }
}
