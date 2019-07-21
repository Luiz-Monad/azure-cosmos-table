using System;
using System.Globalization;
using System.Text;

namespace Microsoft.Azure.Documents.Interop.Common.Schema
{
	internal sealed class JsonScanner
	{
		private sealed class BufferReader
		{
			private readonly string buffer;

			private int atomStartIndex;

			private int atomEndIndex;

			public int AtomLength => atomEndIndex - atomStartIndex;

			public bool IsEof => atomEndIndex >= buffer.Length;

			public BufferReader(string buffer)
			{
				if (buffer == null)
				{
					throw new ArgumentNullException("buffer");
				}
				this.buffer = buffer;
			}

			public bool CheckNext(Func<char, bool> predicate)
			{
				if (!IsEof)
				{
					return predicate(buffer[atomEndIndex]);
				}
				return false;
			}

			public bool ReadNext(out char c)
			{
				if (!IsEof)
				{
					c = buffer[atomEndIndex++];
					return true;
				}
				c = '\0';
				return false;
			}

			public bool ReadNextIfEquals(char c)
			{
				if (!IsEof && c == buffer[atomEndIndex])
				{
					atomEndIndex++;
					return true;
				}
				return false;
			}

			public bool ReadNextIfEquals(char c1, char c2)
			{
				if (!IsEof && (c1 == buffer[atomEndIndex] || c2 == buffer[atomEndIndex]))
				{
					atomEndIndex++;
					return true;
				}
				return false;
			}

			public int AdvanceWhile(Func<char, bool> predicate, bool condition)
			{
				int num = atomEndIndex;
				while (atomEndIndex < buffer.Length && predicate(buffer[atomEndIndex]) == condition)
				{
					atomEndIndex++;
				}
				return atomEndIndex - num;
			}

			public bool UndoRead()
			{
				if (atomEndIndex > atomStartIndex)
				{
					atomEndIndex--;
					return true;
				}
				return false;
			}

			public void StartNewAtom()
			{
				atomStartIndex = atomEndIndex;
			}

			public bool TryParseAtomAsDecimal(out double number)
			{
				if (double.TryParse(GetAtomText(), NumberStyles.Any, CultureInfo.InvariantCulture, out number))
				{
					return true;
				}
				number = 0.0;
				return false;
			}

			public bool TryParseAtomAsHex(out double number)
			{
				string atomText = GetAtomText();
				if (!string.IsNullOrEmpty(atomText) && atomText.Length >= 2 && atomText[0] == '0' && atomText[1] != 'x')
				{
					char c = atomText[1];
				}
				if (long.TryParse(atomText.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long result))
				{
					number = result;
					return true;
				}
				number = 0.0;
				return false;
			}

			public string GetAtomText()
			{
				if (atomEndIndex > buffer.Length)
				{
					throw new InvalidOperationException();
				}
				return buffer.Substring(atomStartIndex, AtomLength);
			}
		}

		private enum ScanState
		{
			Error,
			HasValue,
			Initial
		}

		private enum ScanStringState
		{
			Continue,
			Done,
			Error,
			ReadEscapedCharacter,
			ReadUnicodeCharacter
		}

		private struct UnicodeCharacter
		{
			public int Value;

			public int DigitCount;
		}

		private readonly BufferReader reader;

		private ScanState state;

		private JsonToken currentToken;

		public bool IsEof => reader.IsEof;

		public JsonScanner(string buffer)
		{
			reader = new BufferReader(buffer);
			state = ScanState.Initial;
		}

		public JsonToken GetCurrentToken()
		{
			if (state != ScanState.HasValue)
			{
				throw new InvalidOperationException();
			}
			return currentToken;
		}

		public bool ScanNext()
		{
			if (state != ScanState.Initial && state != ScanState.HasValue)
			{
				throw new InvalidOperationException();
			}
			state = ScanNextPrivate();
			if (state == ScanState.HasValue)
			{
				return true;
			}
			return false;
		}

		private ScanState ScanNextPrivate()
		{
			reader.StartNewAtom();
			if (!reader.ReadNext(out char c))
			{
				return ScanState.Error;
			}
			switch (c)
			{
			case '"':
			case '\'':
				return ScanDelimitedString(c);
			case '.':
				if (reader.CheckNext(char.IsDigit))
				{
					reader.UndoRead();
					return ScanDecimal();
				}
				return ScanState.Error;
			case '-':
				reader.UndoRead();
				return ScanDecimal();
			case ',':
				currentToken = JsonToken.Comma;
				return ScanState.HasValue;
			case ':':
				currentToken = JsonToken.Colon;
				return ScanState.HasValue;
			case '{':
				currentToken = JsonToken.BeginObject;
				return ScanState.HasValue;
			case '}':
				currentToken = JsonToken.EndObject;
				return ScanState.HasValue;
			case '[':
				currentToken = JsonToken.BeginArray;
				return ScanState.HasValue;
			case ']':
				currentToken = JsonToken.EndArray;
				return ScanState.HasValue;
			default:
				if (char.IsWhiteSpace(c))
				{
					reader.AdvanceWhile(char.IsWhiteSpace, condition: true);
					return ScanNextPrivate();
				}
				if (char.IsDigit(c))
				{
					if (c == '0' && reader.ReadNextIfEquals('x', 'X'))
					{
						return ScanHexNumber();
					}
					reader.UndoRead();
					return ScanDecimal();
				}
				return ScanUnquotedString();
			}
		}

		private ScanState ScanDelimitedString(char quotationChar)
		{
			StringBuilder stringBuilder = new StringBuilder();
			ScanStringState scanStringState = ScanStringState.Continue;
			UnicodeCharacter unicodeCharacter = default(UnicodeCharacter);
			char c;
			while (reader.ReadNext(out c))
			{
				switch (scanStringState)
				{
				case ScanStringState.Continue:
					if (c == quotationChar)
					{
						scanStringState = ScanStringState.Done;
					}
					else if (c == '\\')
					{
						scanStringState = ScanStringState.ReadEscapedCharacter;
					}
					break;
				case ScanStringState.ReadEscapedCharacter:
					if (c == 'u')
					{
						unicodeCharacter = default(UnicodeCharacter);
						scanStringState = ScanStringState.ReadUnicodeCharacter;
						break;
					}
					switch (c)
					{
					case '\'':
						c = '\'';
						break;
					case '"':
						c = '"';
						break;
					case '\\':
						c = '\\';
						break;
					case '/':
						c = '/';
						break;
					case 'b':
						c = '\b';
						break;
					case 'f':
						c = '\f';
						break;
					case 'n':
						c = '\n';
						break;
					case 'r':
						c = '\r';
						break;
					case 't':
						c = '\t';
						break;
					}
					scanStringState = ScanStringState.Continue;
					break;
				case ScanStringState.ReadUnicodeCharacter:
					if (SchemaUtil.IsHexCharacter(c))
					{
						unicodeCharacter.Value <<= 4;
						unicodeCharacter.Value += SchemaUtil.GetHexValue(c);
						unicodeCharacter.DigitCount++;
						if (unicodeCharacter.DigitCount == 4)
						{
							c = (char)unicodeCharacter.Value;
							scanStringState = ScanStringState.Continue;
						}
					}
					else
					{
						scanStringState = ScanStringState.Error;
					}
					break;
				}
				switch (scanStringState)
				{
				case ScanStringState.Continue:
					stringBuilder.Append(c);
					continue;
				default:
					continue;
				case ScanStringState.Done:
				case ScanStringState.Error:
					break;
				}
				break;
			}
			if (scanStringState == ScanStringState.Done)
			{
				currentToken = JsonToken.StringToken(stringBuilder.ToString(), reader.GetAtomText());
				return ScanState.HasValue;
			}
			return ScanState.Error;
		}

		private ScanState ScanUnquotedString()
		{
			reader.AdvanceWhile(SchemaUtil.IsIdentifierCharacter, condition: true);
			switch (reader.GetAtomText())
			{
			case "Infinity":
				currentToken = JsonToken.Infinity;
				return ScanState.HasValue;
			case "NaN":
				currentToken = JsonToken.NaN;
				return ScanState.HasValue;
			case "true":
				currentToken = JsonToken.True;
				return ScanState.HasValue;
			case "false":
				currentToken = JsonToken.False;
				return ScanState.HasValue;
			case "null":
				currentToken = JsonToken.Null;
				return ScanState.HasValue;
			default:
				return ScanState.Error;
			}
		}

		private ScanState ScanDecimal()
		{
			reader.ReadNextIfEquals('-');
			reader.AdvanceWhile(char.IsDigit, condition: true);
			if (reader.ReadNextIfEquals('.'))
			{
				reader.AdvanceWhile(char.IsDigit, condition: true);
			}
			if (reader.ReadNextIfEquals('e', 'E'))
			{
				reader.ReadNextIfEquals('+', '-');
				if (reader.AdvanceWhile(char.IsDigit, condition: true) <= 0)
				{
					return ScanState.Error;
				}
			}
			if (reader.AdvanceWhile(SchemaUtil.IsIdentifierCharacter, condition: true) > 0)
			{
				return ScanState.Error;
			}
			if (!reader.TryParseAtomAsDecimal(out double number))
			{
				return ScanState.Error;
			}
			currentToken = JsonToken.NumberToken(number, reader.GetAtomText());
			return ScanState.HasValue;
		}

		private ScanState ScanHexNumber()
		{
			reader.AdvanceWhile(SchemaUtil.IsHexCharacter, condition: true);
			if (reader.AdvanceWhile(SchemaUtil.IsIdentifierCharacter, condition: true) > 0)
			{
				return ScanState.Error;
			}
			if (!reader.TryParseAtomAsHex(out double number))
			{
				return ScanState.Error;
			}
			currentToken = JsonToken.NumberToken(number, reader.GetAtomText());
			return ScanState.HasValue;
		}
	}
}
