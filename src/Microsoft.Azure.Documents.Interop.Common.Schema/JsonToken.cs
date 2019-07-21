using System;

namespace Microsoft.Azure.Documents.Interop.Common.Schema
{
	internal class JsonToken
	{
		private sealed class BooleanJsonToken : JsonToken
		{
			private readonly bool value;

			public BooleanJsonToken(bool value, string lexeme)
				: base(JsonTokenType.Boolean, lexeme)
			{
				this.value = value;
			}

			public override bool GetBooleanValue()
			{
				return value;
			}
		}

		private sealed class NumberJsonToken : JsonToken
		{
			private readonly double value;

			public NumberJsonToken(double value, string lexeme)
				: base(JsonTokenType.Number, lexeme)
			{
				if (lexeme == null)
				{
					throw new ArgumentNullException("lexeme");
				}
				this.value = value;
			}

			public override double GetDoubleValue()
			{
				return value;
			}

			public override long GetInt64Value()
			{
				return Convert.ToInt64(value);
			}
		}

		private sealed class StringJsonToken : JsonToken
		{
			private readonly string value;

			public StringJsonToken(string value, string lexeme)
				: base(JsonTokenType.String, lexeme)
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				if (lexeme == null)
				{
					throw new ArgumentNullException("lexeme");
				}
				this.value = value;
			}

			public override string GetStringValue()
			{
				return value;
			}
		}

		private static readonly JsonToken BeginObjectToken = new JsonToken(JsonTokenType.BeginObject, "{");

		private static readonly JsonToken EndObjectToken = new JsonToken(JsonTokenType.EndObject, "}");

		private static readonly JsonToken BeginArrayToken = new JsonToken(JsonTokenType.BeginArray, "[");

		private static readonly JsonToken EndArrayToken = new JsonToken(JsonTokenType.EndArray, "]");

		private static readonly JsonToken ColonToken = new JsonToken(JsonTokenType.Colon, ":");

		private static readonly JsonToken CommaToken = new JsonToken(JsonTokenType.Comma, ",");

		private static readonly JsonToken NullToken = new JsonToken(JsonTokenType.Null, "null");

		private static readonly NumberJsonToken NaNToken = new NumberJsonToken(double.NaN, "NaN");

		private static readonly NumberJsonToken InfinityToken = new NumberJsonToken(double.PositiveInfinity, "Infinity");

		private static readonly BooleanJsonToken TrueToken = new BooleanJsonToken(value: true, "true");

		private static readonly BooleanJsonToken FalseToken = new BooleanJsonToken(value: false, "false");

		private readonly JsonTokenType type;

		private readonly string lexeme;

		public JsonTokenType Type => type;

		public string Lexeme => lexeme;

		public static JsonToken BeginObject => BeginObjectToken;

		public static JsonToken EndObject => EndObjectToken;

		public static JsonToken BeginArray => BeginArrayToken;

		public static JsonToken EndArray => EndArrayToken;

		public static JsonToken Colon => ColonToken;

		public static JsonToken Comma => CommaToken;

		public static JsonToken Null => NullToken;

		public static JsonToken True => TrueToken;

		public static JsonToken False => FalseToken;

		public static JsonToken NaN => NaNToken;

		public static JsonToken Infinity => InfinityToken;

		protected JsonToken(JsonTokenType type, string lexeme)
		{
			this.type = type;
			this.lexeme = lexeme;
		}

		public virtual double GetDoubleValue()
		{
			throw new InvalidCastException();
		}

		public virtual long GetInt64Value()
		{
			throw new InvalidCastException();
		}

		public virtual string GetStringValue()
		{
			throw new InvalidCastException();
		}

		public virtual bool GetBooleanValue()
		{
			throw new InvalidCastException();
		}

		public static JsonToken NumberToken(double value, string lexeme)
		{
			return new NumberJsonToken(value, lexeme);
		}

		public static JsonToken StringToken(string value, string lexeme)
		{
			return new StringJsonToken(value, lexeme);
		}
	}
}
