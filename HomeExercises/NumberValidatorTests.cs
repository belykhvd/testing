using System;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace HomeExercises
{
	public class NumberValidatorTests
	{		
		[TestCase(-1, 2, true, TestName = "Negative precision")]
		[TestCase(3, -1, true, TestName = "Negative scale")]
		[TestCase(3, 3, true, TestName = "Scale equals precision")]
		[TestCase(3, 4, true, TestName = "Scale is bigger than precision")]
		public static void ArgumentException_OnBadPrecisionOrScale_InConstructor(int precision, int scale, bool onlyPositive)
			=> Assert.Throws<ArgumentException>(() => new NumberValidator(precision, scale, onlyPositive));

		[TestCase(1, 0, true, TestName = "Valid precision and scale")]
		public static void Assert_ValidPrecisionAndScale_Passed_InConstructor(int precision, int scale, bool onlyPositive)
			=> Assert.DoesNotThrow(() => new NumberValidator(precision, scale, onlyPositive));

		[TestCase(null, ExpectedResult = false, TestName = "Null value")]
		[TestCase("", ExpectedResult = false, TestName = "Empty value")]
		[TestCase(" ", ExpectedResult = false, TestName = "Whitespace value")]
		public static bool NoNumberTests(string value)
			=> new NumberValidator(int.MaxValue, int.MaxValue - 100500).IsValidNumber(value);

		[TestCase("0", ExpectedResult = true, TestName = "Integer")]
		[TestCase("0.1", ExpectedResult = true, TestName = "With fraction")]
		[TestCase("+0.1", ExpectedResult = true, TestName = "With unary plus")]
		[TestCase("-0.1", ExpectedResult = true, TestName = "With unary minus")]
		[TestCase("  1.1", ExpectedResult = false, TestName = "With leading whitespaces")]
		[TestCase("1.1   ", ExpectedResult = false, TestName = "With closing whitespaces")]
		[TestCase("1 .1", ExpectedResult = false, TestName = "With whitespace between number symbols")]
		[TestCase("5.", ExpectedResult = false, TestName = "With dot but without fraction part")]
		[TestCase(".5", ExpectedResult = false, TestName = "With dot but without integer part")]
		[TestCase("--4.5", ExpectedResult = false, TestName = "With multiple signs")]
		[TestCase("+a.b", ExpectedResult = false, TestName = "On HEXadecimal value")]
		[TestCase("^NaN$", ExpectedResult = false, TestName = "When not a number")]
		[TestCase("1..1", ExpectedResult = false, TestName = "When multiple dots")]
		public static bool RegExpTests(string value)
			=> new NumberValidator(int.MaxValue, int.MaxValue - 100500).IsValidNumber(value);

		[TestCase(4, 2, true, "+3.14", ExpectedResult = true, TestName = "Valid precision and scale")]
		[TestCase(4, 2, true, "+31.41", ExpectedResult = false, TestName = "More digits than precision allows")]
		[TestCase(4, 2, true, "3.141", ExpectedResult = false, TestName = "Fraction part is bigger than scale allows")]
		[TestCase(4, 2, true, "-3.14", ExpectedResult = false, TestName = "On minus when only positive allowed")]
		public static bool PrecisionScaleRulesTests(int precision, int scale, bool onlyPositive, string value)
			=> new NumberValidator(precision, scale, onlyPositive).IsValidNumber(value);
	}

	public class NumberValidator
	{
		private readonly Regex numberRegex;
		private readonly bool onlyPositive;
		private readonly int precision;
		private readonly int scale;

		public NumberValidator(int precision, int scale = 0, bool onlyPositive = false)
		{
			this.precision = precision;
			this.scale = scale;
			this.onlyPositive = onlyPositive;
			if (precision <= 0)
				throw new ArgumentException("precision must be a positive number");
			if (scale < 0 || scale >= precision)
				throw new ArgumentException("scale must be a non-negative number less or equal than precision");
			numberRegex = new Regex(@"^([+-]?)(\d+)([.,](\d+))?$", RegexOptions.IgnoreCase);
		}

		public bool IsValidNumber(string value)
		{
			// Проверяем соответствие входного значения формату N(m,k), в соответствии с правилом, 
			// описанным в Формате описи документов, направляемых в налоговый орган в электронном виде по телекоммуникационным каналам связи:
			// Формат числового значения указывается в виде N(m.к), где m – максимальное количество знаков в числе, включая знак (для отрицательного числа), 
			// целую и дробную часть числа без разделяющей десятичной точки, k – максимальное число знаков дробной части числа. 
			// Если число знаков дробной части числа равно 0 (т.е. число целое), то формат числового значения имеет вид N(m).

			if (string.IsNullOrEmpty(value))
				return false;

			var match = numberRegex.Match(value);
			if (!match.Success)
				return false;

			// Знак и целая часть
			var intPart = match.Groups[1].Value.Length + match.Groups[2].Value.Length;
			// Дробная часть
			var fracPart = match.Groups[4].Value.Length;

			if (intPart + fracPart > precision || fracPart > scale)
				return false;

			if (onlyPositive && match.Groups[1].Value == "-")
				return false;
			return true;
		}
	}
}