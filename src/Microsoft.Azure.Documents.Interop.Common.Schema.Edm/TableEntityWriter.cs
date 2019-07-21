using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Microsoft.Azure.Documents.Interop.Common.Schema.Edm
{
	internal sealed class TableEntityWriter : ITableEntityWriter, IDisposable
	{
		private readonly TextWriter textWriter;

		private TableEntityWriterContext context;

		private TableEntityWriterState state;

		private string elemantName;

		private bool disposed;

		public TableEntityWriter(TextWriter writer)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			textWriter = writer;
			context = new TableEntityWriterContext();
			state = TableEntityWriterState.Initial;
		}

		public void Close()
		{
			if (state != TableEntityWriterState.CLosed)
			{
				Flush();
				state = TableEntityWriterState.CLosed;
			}
		}

		public void Flush()
		{
			ThrowIfDisposed();
			textWriter.Flush();
		}

		public void Start()
		{
			ThrowIfDisposed();
			ThrowIfInvalidState("WriteStart", default(TableEntityWriterState));
			textWriter.Write('{');
			state = TableEntityWriterState.Name;
		}

		public void End()
		{
			ThrowIfDisposed();
			ThrowIfInvalidState("WriteStart", TableEntityWriterState.Name);
			textWriter.Write('}');
			state = TableEntityWriterState.Done;
		}

		public void WriteName(string name)
		{
			if (name == null)
			{
				throw new ArgumentException("name");
			}
			if (name.StartsWith(EdmSchemaMapping.SystemPropertiesPrefix, StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException("name");
			}
			ThrowIfDisposed();
			ThrowIfInvalidState("WriteName", TableEntityWriterState.Name);
			if (EdmSchemaMapping.IsDocumentDBProperty(name))
			{
				elemantName = EdmSchemaMapping.SystemPropertiesPrefix + name;
			}
			else
			{
				elemantName = name;
			}
			state = TableEntityWriterState.Value;
		}

		public void WriteString(string value)
		{
			ThrowIfDisposed();
			ThrowIfInvalidState("WriteString", TableEntityWriterState.Value);
			WriteNameAux(elemantName);
			if (value == null)
			{
				WriteNull(DataType.String);
			}
			else
			{
				WriteValue(DataType.String, SchemaUtil.GetQuotedString(value));
			}
			UpdateWriterState();
		}

		public void WriteBinary(byte[] value)
		{
			ThrowIfDisposed();
			ThrowIfInvalidState("WriteBinary", TableEntityWriterState.Value);
			WriteNameAux(elemantName);
			if (value == null)
			{
				WriteNull(DataType.Binary);
			}
			else
			{
				WriteValue(DataType.Binary, SchemaUtil.GetQuotedString(SchemaUtil.BytesToString(value)));
			}
			UpdateWriterState();
		}

		public void WriteBoolean(bool? value)
		{
			ThrowIfDisposed();
			ThrowIfInvalidState("WriteBoolean", TableEntityWriterState.Value);
			WriteNameAux(elemantName);
			if (!value.HasValue)
			{
				WriteNull(DataType.Boolean);
			}
			else
			{
				WriteValue(DataType.Boolean, value.Value ? "true" : "false");
			}
			UpdateWriterState();
		}

		public void WriteDateTime(DateTime? value)
		{
			ThrowIfDisposed();
			ThrowIfInvalidState("WriteDateTime", TableEntityWriterState.Value);
			WriteNameAux(elemantName);
			if (!value.HasValue)
			{
				WriteNull(DataType.DateTime);
			}
			else
			{
				WriteValue(DataType.DateTime, SchemaUtil.GetQuotedString(SchemaUtil.GetUtcTicksString(value.Value)));
			}
			UpdateWriterState();
		}

		public void WriteDouble(double? value)
		{
			ThrowIfDisposed();
			ThrowIfInvalidState("WriteDouble", TableEntityWriterState.Value);
			WriteNameAux(elemantName);
			if (!value.HasValue)
			{
				WriteNull(DataType.Double);
			}
			else
			{
				WriteValue(DataType.Double, value.Value.ToString("G17", CultureInfo.InvariantCulture));
			}
			UpdateWriterState();
		}

		public void WriteGuid(Guid? value)
		{
			ThrowIfDisposed();
			ThrowIfInvalidState("WriteGuid", TableEntityWriterState.Value);
			WriteNameAux(elemantName);
			if (!value.HasValue)
			{
				WriteNull(DataType.Guid);
			}
			else
			{
				WriteValue(DataType.Guid, SchemaUtil.GetQuotedString(value.ToString()));
			}
			UpdateWriterState();
		}

		public void WriteInt32(int? value)
		{
			ThrowIfDisposed();
			ThrowIfInvalidState("WriteInt32", TableEntityWriterState.Value);
			WriteNameAux(elemantName);
			if (!value.HasValue)
			{
				WriteNull(DataType.Int32);
			}
			else
			{
				WriteValue(DataType.Int32, value.Value);
			}
			UpdateWriterState();
		}

		public void WriteInt64(long? value)
		{
			ThrowIfDisposed();
			ThrowIfInvalidState("WriteInt64", TableEntityWriterState.Value);
			WriteNameAux(elemantName);
			if (!value.HasValue)
			{
				WriteNull(DataType.Int64);
			}
			else
			{
				WriteValue(DataType.Int64, SchemaUtil.GetQuotedString(value.Value.ToString("D20", CultureInfo.InvariantCulture)));
			}
			UpdateWriterState();
		}

		public void Dispose()
		{
			if (!disposed)
			{
				Close();
				disposed = true;
			}
		}

		private void WriteNameAux(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (context.HasElements)
			{
				textWriter.Write(", ");
			}
			textWriter.Write(SchemaUtil.GetQuotedString(name));
			textWriter.Write(": ");
			context.HasElements = true;
		}

		private void UpdateWriterState()
		{
			if (state == TableEntityWriterState.Name)
			{
				state = TableEntityWriterState.Value;
			}
			else
			{
				state = TableEntityWriterState.Name;
			}
		}

		private void WriteNull(DataType type)
		{
			textWriter.Write('{');
			textWriter.Write("\"{0}\": {1}", "$t", (int)type);
			textWriter.Write(", ");
			textWriter.Write("\"{0}\": {1}", "$v", "null");
			textWriter.Write('}');
		}

		private void WriteValue<TValue>(DataType type, TValue value)
		{
			if (value == null)
			{
				WriteNull(type);
				return;
			}
			textWriter.Write('{');
			textWriter.Write("\"{0}\": {1}", "$t", (int)type);
			textWriter.Write(", ");
			textWriter.Write("\"{0}\": {1}", "$v", value);
			textWriter.Write('}');
		}

		private void ThrowIfDisposed()
		{
			if (disposed)
			{
				throw new ObjectDisposedException("TableEntityWriter");
			}
		}

		private void ThrowIfInvalidState(string methodName, params TableEntityWriterState[] validStates)
		{
			foreach (TableEntityWriterState tableEntityWriterState in validStates)
			{
				if (state == tableEntityWriterState)
				{
					return;
				}
			}
			string arg = string.Join(" or ", (from s in validStates
			select s.ToString()).ToArray());
			throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "{0} can only be called when state is {1}, actual state is {2}", methodName, arg, state.ToString()));
		}
	}
}
