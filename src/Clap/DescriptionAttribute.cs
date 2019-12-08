using System;

namespace Clap
{
    /// <summary>
    /// Attribute for adding a description that is used in the automatically generated help page.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class DescriptionAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptionAttribute"/> class.
        /// </summary>
        /// <param name="text">The description text.</param>
        public DescriptionAttribute(string text)
            => Text = text;

        /// <summary>
        /// Gets the description text.
        /// </summary>
        public string Text { get; }
    }
}
