using System;

namespace Clap
{
    /// <summary>
    /// Exception thrown when something goes wrong while parsing the command line arguments.
    /// </summary>
    /// <seealso cref="Exception" />
    public class CommandLineArgumentsException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineArgumentsException"/> class.
        /// </summary>
        public CommandLineArgumentsException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineArgumentsException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CommandLineArgumentsException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineArgumentsException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public CommandLineArgumentsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
