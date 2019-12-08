using System;
using System.Runtime.CompilerServices;

namespace Clap
{
    /// <summary>
    /// Indicates that the option does not have a name when parsing.
    /// </summary>
    /// <seealso cref="Attribute" />
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class UnnamedAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnnamedAttribute"/> class.
        /// </summary>
        /// <param name="order">The order number.</param>
        public UnnamedAttribute([CallerLineNumber]int order = 0)
            => Order = order;

        /// <summary>
        /// Gets the order number.
        /// </summary>
        public int Order { get; }
    }
}
