using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Xunit;
using static AssertNet.Assertions;

namespace Clap.Tests
{
    /// <summary>
    /// Test class for the <see cref="Arguments"/> class.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812", Justification = "Instantiated using reflection.")]
    public static class ArgumentsTests
    {
        /// <summary>
        /// Checks that we can correctly detect a true boolean value.
        /// </summary>
        [Fact]
        public static void SingleBooleanOptionTrue()
        {
            SingleBooleanOptions options = Arguments.Parse<SingleBooleanOptions>("-value", CultureInfo.InvariantCulture);
            AssertThat(options).IsNotNull().IsExactlyInstanceOf<SingleBooleanOptions>();
            AssertThat(options.Value).IsTrue();
        }

        /// <summary>
        /// Checks that we can correctly detect a false boolean value.
        /// </summary>
        [Fact]
        public static void SingleBooleanOptionFalse()
        {
            SingleBooleanOptions options = Arguments.Parse<SingleBooleanOptions>(string.Empty, CultureInfo.InvariantCulture);
            AssertThat(options).IsNotNull().IsExactlyInstanceOf<SingleBooleanOptions>();
            AssertThat(options.Value).IsFalse();
        }

        /// <summary>
        /// Checks that we can correctly detect a set integer value.
        /// </summary>
        /// <param name="name">The name of the option.</param>
        [Theory]
        [InlineData("value")]
        [InlineData("VALUE")]
        [InlineData("VaLuE")]
        [InlineData("v")]
        [InlineData("V")]
        [InlineData("val")]
        [InlineData("VAL")]
        public static void SingleIntegerOptionSet(string name)
        {
            SingleIntegerOptions options = Arguments.Parse<SingleIntegerOptions>($"-{name} 42", CultureInfo.InvariantCulture);
            AssertThat(options).IsNotNull().IsExactlyInstanceOf<SingleIntegerOptions>();
            AssertThat(options.Value).IsEqualTo(42);
        }

        /// <summary>
        /// Checks that we can correctly detect an unset integer value.
        /// </summary>
        [Fact]
        public static void SingleIntegerOptionUnset()
        {
            SingleIntegerOptions options = Arguments.Parse<SingleIntegerOptions>(string.Empty, CultureInfo.InvariantCulture);
            AssertThat(options).IsNotNull().IsExactlyInstanceOf<SingleIntegerOptions>();
            AssertThat(options.Value).IsEqualTo(1337);
        }

        /// <summary>
        /// Checks that options that require a value but don't have a value correctly throw an exception.
        /// </summary>
        [Fact]
        public static void MissingIntegerOption()
        {
            AssertThat(() => Arguments.Parse<SingleIntegerOptions>("-value -x", CultureInfo.InvariantCulture)).ThrowsException();
            AssertThat(() => Arguments.Parse<SingleIntegerOptions>("-value", CultureInfo.InvariantCulture)).ThrowsException();
        }

        /// <summary>
        /// Checks that we can correctly detect an enum.
        /// </summary>
        [Fact]
        public static void SingleEnumOption()
        {
            SingleEnumOptions options = Arguments.Parse<SingleEnumOptions>("-type type2", CultureInfo.InvariantCulture);
            AssertThat(options).IsNotNull().IsExactlyInstanceOf<SingleEnumOptions>();
            AssertThat(options.Type).IsEqualTo(SingleEnumOptions.Types.Type2);
        }

        /// <summary>
        /// Checks that we can correctly detect an array.
        /// </summary>
        [Fact]
        public static void SingleArrayOption()
        {
            SingleArrayOptions options = Arguments.Parse<SingleArrayOptions>("-values 1 2 3 4", CultureInfo.InvariantCulture);
            AssertThat(options).IsNotNull().IsExactlyInstanceOf<SingleArrayOptions>();
            AssertThat(options.Values).ContainsExactly(1, 2, 3, 4);
        }

        /// <summary>
        /// Checks that we can correctly detect a collection.
        /// </summary>
        [Fact]
        public static void SingleListOption()
        {
            SingleListOptions options = Arguments.Parse<SingleListOptions>("-values 1 2 3 4", CultureInfo.InvariantCulture);
            AssertThat(options).IsNotNull().IsExactlyInstanceOf<SingleListOptions>();
            AssertThat(options.Values).ContainsExactly(1, 2, 3, 4);
        }

        /// <summary>
        /// Checks that we can correctly deal with mixed program argument types.
        /// </summary>
        /// <param name="input">The program arguments.</param>
        [Theory]
        [InlineData("-names Pupper Pup Doge Doggo -dog husky -amount 4 -pettable")]
        [InlineData("-dog husky -names Pupper Pup Doge Doggo -amount 4 -pettable")]
        [InlineData("-amount 4 -dog husky -names Pupper Pup Doge Doggo -pettable")]
        [InlineData("-pettable -amount 4 -dog husky -names Pupper Pup Doge Doggo")]
        [InlineData("-p -a 4 -d husky -n Pupper Pup Doge Doggo")]
        [InlineData("-d husky -a 4 -p -n Pupper Pup Doge Doggo")]
        public static void MixedOption(string input)
        {
            MixedOptions options = Arguments.Parse<MixedOptions>(input, CultureInfo.InvariantCulture);
            AssertThat(options).IsNotNull().IsExactlyInstanceOf<MixedOptions>();
            AssertThat(options.Names).ContainsExactly("Pupper", "Pup", "Doge", "Doggo");
            AssertThat(options.Dog).IsEqualTo(MixedOptions.DogType.Husky);
            AssertThat(options.Amount).IsEqualTo(4);
            AssertThat(options.Pettable).IsTrue();
        }

        private class SingleBooleanOptions
        {
            public bool Value { get; set; }
        }

        private class SingleIntegerOptions
        {
            [Alias("v", "val")]
            public int Value { get; set; } = 1337;
        }

        private class SingleEnumOptions
        {
            public enum Types
            {
                Type1,
                Type2,
            }

            public Types Type { get; set; }
        }

        private class SingleArrayOptions
        {
            public int[] Values { get; set; }
        }

        private class SingleListOptions
        {
            public List<int> Values { get; set; }
        }

        private class MixedOptions
        {
            public enum DogType
            {
                Labrador,
                Husky,
            }

            [Alias("d")]
            public DogType Dog { get; set; }

            [Alias("a")]
            public int Amount { get; set; }

            [Alias("n")]
            public string[] Names { get; set; }

            [Alias("p")]
            public bool Pettable { get; set; }
        }
    }
}
