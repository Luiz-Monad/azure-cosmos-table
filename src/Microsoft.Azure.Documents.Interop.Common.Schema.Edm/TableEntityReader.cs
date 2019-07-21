using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.Azure.Documents.Interop.Common.Schema.Edm
{
	internal sealed class TableEntityReader : ITableEntityReader, IDisposable
	{
		private static List<int> edmTypeValues;

		private readonly JsonScanner scanner;

		private TableEntityReaderState state;

		private JsonToken currentToken;

		private JsonToken pushedToken;

		private JsonToken currentValue;

		private string currentName;

		private DataType currentEdmType;

		private bool disposed;

		public string CurrentName
		{
			get
			{
				ThrowIfDisposed();
				ThrowIfInvalidState("get_CurrentName", TableEntityReaderState.HasValue);
				return currentName;
			}
		}

		public DataType CurrentType
		{
			get
			{
				ThrowIfDisposed();
				ThrowIfInvalidState("get_CurrentType", TableEntityReaderState.HasValue);
				return currentEdmType;
			}
		}

		static TableEntityReader()
		{
			edmTypeValues = new List<int>
			{
				2,
				1,
				8,
				5,
				0,
				16,
				18,
				9
			};
		}

		public TableEntityReader(string json)
		{
			if (string.IsNullOrEmpty(json))
			{
				throw new ArgumentException("json");
			}
			scanner = new JsonScanner(json);
			state = TableEntityReaderState.Initial;
		}

		public void Start()
		{
			ThrowIfDisposed();
			ThrowIfInvalidState("Start", default(TableEntityReaderState));
			Expect(JsonTokenType.BeginObject);
		}

		public void End()
		{
			ThrowIfDisposed();
			ThrowIfInvalidState("End", TableEntityReaderState.Done);
		}

		public bool MoveNext()
		{
			ThrowIfDisposed();
			if (state == TableEntityReaderState.Done)
			{
				return false;
			}
			JsonToken jsonToken = PopToken();
			if (jsonToken.Type == JsonTokenType.EndObject)
			{
				state = TableEntityReaderState.Done;
				return false;
			}
			if (jsonToken.Type == JsonTokenType.String)
			{
				currentName = jsonToken.GetStringValue();
			}
			else
			{
				ThrowFormatException("Expecting a name but found '{0}'", jsonToken.Lexeme);
			}
			Expect(JsonTokenType.Colon);
			JsonToken jsonToken2 = PopToken();
			if (EdmSchemaMapping.IsDocumentDBProperty(currentName) || currentName == "$pk" || currentName == "$id")
			{
				switch (jsonToken2.Type)
				{
				case JsonTokenType.String:
					currentEdmType = DataType.String;
					currentValue = jsonToken2;
					break;
				case JsonTokenType.Number:
					currentEdmType = DataType.Double;
					currentValue = jsonToken2;
					break;
				default:
					ThrowFormatException("Unexpected value type '{0}' for DocumentDB property.", jsonToken2.Type);
					break;
				}
			}
			else
			{
				if (jsonToken2.Type != JsonTokenType.BeginObject)
				{
					ThrowFormatException("Value is expected to be an object instead it was '{0}'.", jsonToken2.Type);
				}
				currentEdmType = ParseEdmType();
			}
			TryReadComma();
			state = TableEntityReaderState.HasValue;
			return true;
		}

		public string ReadString()
		{
			ThrowIfDisposed();
			ThrowIfInvalidState("ReadString", TableEntityReaderState.HasValue);
			ThrowIfInvalidType("ReadString", DataType.String);
			if (currentValue.Type == JsonTokenType.Null)
			{
				return null;
			}
			EnsureMatchingTypes(currentValue.Type, JsonTokenType.String);
			return currentValue.GetStringValue();
		}

		public byte[] ReadBinary()
		{
			ThrowIfDisposed();
			ThrowIfInvalidState("ReadBinary", TableEntityReaderState.HasValue);
			ThrowIfInvalidType("ReadBinary", DataType.Binary);
			if (currentValue.Type == JsonTokenType.Null)
			{
				return null;
			}
			EnsureMatchingTypes(currentValue.Type, JsonTokenType.String);
			return SchemaUtil.StringToBytes(currentValue.GetStringValue());
		}

		public bool? ReadBoolean()
		{
			ThrowIfDisposed();
			ThrowIfInvalidState("ReadBoolean", TableEntityReaderState.HasValue);
			ThrowIfInvalidType("ReadBoolean", DataType.Boolean);
			if (currentValue.Type == JsonTokenType.Null)
			{
				return null;
			}
			EnsureMatchingTypes(currentValue.Type, JsonTokenType.Boolean);
			return currentValue.GetBooleanValue();
		}

		public DateTime? ReadDateTime()
		{
			ThrowIfDisposed();
			ThrowIfInvalidState("ReadDateTime", TableEntityReaderState.HasValue);
			ThrowIfInvalidType("ReadDateTime", DataType.DateTime);
			if (currentValue.Type == JsonTokenType.Null)
			{
				return null;
			}
			EnsureMatchingTypes(currentValue.Type, JsonTokenType.String);
			return SchemaUtil.GetDateTimeFromUtcTicks(long.Parse(currentValue.GetStringValue(), CultureInfo.InvariantCulture));
		}

		public double? ReadDouble()
		{
			ThrowIfDisposed();
			ThrowIfInvalidState("ReadDouble", TableEntityReaderState.HasValue);
			ThrowIfInvalidType("ReadDouble", DataType.Double);
			if (currentValue.Type == JsonTokenType.Null)
			{
				return null;
			}
			if (currentValue.Type == JsonTokenType.String)
			{
				return double.Parse(currentValue.GetStringValue(), CultureInfo.InvariantCulture);
			}
			EnsureMatchingTypes(currentValue.Type, JsonTokenType.Number);
			return currentValue.GetDoubleValue();
		}

		public Guid? ReadGuid()
		{
			ThrowIfDisposed();
			ThrowIfInvalidState("ReadGuid", TableEntityReaderState.HasValue);
			ThrowIfInvalidType("ReadGuid", DataType.Guid);
			if (currentValue.Type == JsonTokenType.Null)
			{
				return null;
			}
			EnsureMatchingTypes(currentValue.Type, JsonTokenType.String);
			return Guid.Parse(currentValue.GetStringValue());
		}

		public int? ReadInt32()
		{
			ThrowIfDisposed();
			ThrowIfInvalidState("ReadInt32", TableEntityReaderState.HasValue);
			ThrowIfInvalidType("ReadInt32", DataType.Int32);
			if (currentValue.Type == JsonTokenType.Null)
			{
				return null;
			}
			EnsureMatchingTypes(currentValue.Type, JsonTokenType.Number);
			return (int)currentValue.GetDoubleValue();
		}

		public long? ReadInt64()
		{
			ThrowIfDisposed();
			ThrowIfInvalidState("ReadInt64", TableEntityReaderState.HasValue);
			ThrowIfInvalidType("ReadInt64", DataType.Int64);
			if (currentValue.Type == JsonTokenType.Null)
			{
				return null;
			}
			EnsureMatchingTypes(currentValue.Type, JsonTokenType.String);
			return long.Parse(currentValue.GetStringValue(), CultureInfo.InvariantCulture);
		}

		public void Dispose()
		{
			if (!disposed)
			{
				disposed = true;
			}
		}

		private DataType ParseEdmType()
		{
			JsonToken jsonToken = PopToken();
			if (jsonToken.Type != JsonTokenType.String || jsonToken.GetStringValue() != "$t")
			{
				ThrowFormatException("Expecting type marker but found '{0}'", jsonToken.Type.ToString());
			}
			Expect(JsonTokenType.Colon);
			Expect(JsonTokenType.Number);
			double doubleValue = currentToken.GetDoubleValue();
			if (doubleValue % 1.0 != 0.0 || !edmTypeValues.Contains((int)doubleValue))
			{
				ThrowFormatException("Invalid Edm type: {0}", doubleValue);
			}
			int result = (int)doubleValue;
			Expect(JsonTokenType.Comma);
			Expect("$v");
			Expect(JsonTokenType.Colon);
			currentToken = PopToken();
			currentValue = currentToken;
			Expect(JsonTokenType.EndObject);
			return (DataType)result;
		}

		private void Expect(JsonTokenType type)
		{
			JsonToken jsonToken = PopToken();
			if (jsonToken.Type != type)
			{
				ThrowFormatException("Expecting type {0} but found {1}", type, jsonToken.Type);
			}
			currentToken = jsonToken;
		}

		private void EnsureMatchingTypes(JsonTokenType type1, JsonTokenType type2)
		{
			if (type1 != type2)
			{
				ThrowFormatException("type should be {0} but found {1}", type1, type2);
			}
		}

		private void Expect(string stringToken)
		{
			Expect(JsonTokenType.String);
			if (currentToken.GetStringValue() != stringToken)
			{
				ThrowFormatException("Expecting token {0} but found {1}", stringToken, currentToken.GetStringValue());
			}
		}

		private void PushToken(JsonToken token)
		{
			pushedToken = token;
		}

		private JsonToken PopToken()
		{
			if (pushedToken != null)
			{
				JsonToken result = pushedToken;
				pushedToken = null;
				return result;
			}
			if (scanner.ScanNext())
			{
				return scanner.GetCurrentToken();
			}
			throw new Exception("Scanner failed.");
		}

		private bool TryReadComma()
		{
			JsonToken jsonToken = PopToken();
			if (jsonToken.Type == JsonTokenType.Comma)
			{
				return true;
			}
			PushToken(jsonToken);
			return false;
		}

		private void ThrowIfDisposed()
		{
			if (disposed)
			{
				throw new ObjectDisposedException("TableEntityReader");
			}
		}

		private void ThrowIfInvalidState(string methodName, params TableEntityReaderState[] validStates)
		{
			foreach (TableEntityReaderState tableEntityReaderState in validStates)
			{
				if (state == tableEntityReaderState)
				{
					return;
				}
			}
			string arg = string.Join(" or ", (from s in validStates
			select s.ToString()).ToArray());
			throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "{0} can only be called when state is {1}, actual state is {2}", methodName, arg, state.ToString()));
		}

		private void ThrowIfInvalidType(string methodName, DataType expectedType)
		{
			if (currentEdmType == expectedType)
			{
				return;
			}
			throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "{0} expects current type to be {1}, actual type is {2}", methodName, expectedType.ToString(), currentEdmType.ToString()));
		}

		private void ThrowFormatException(string format, params object[] args)
		{
			throw new FormatException(string.Format(CultureInfo.InvariantCulture, format, args));
		}
	}
}
