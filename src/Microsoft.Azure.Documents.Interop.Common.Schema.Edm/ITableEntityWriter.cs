using System;

namespace Microsoft.Azure.Documents.Interop.Common.Schema.Edm
{
	internal interface ITableEntityWriter : IDisposable
	{
		void Close();

		void Flush();

		void Start();

		void End();

		void WriteName(string name);

		void WriteString(string value);

		void WriteBinary(byte[] value);

		void WriteBoolean(bool? value);

		void WriteDateTime(DateTime? value);

		void WriteDouble(double? value);

		void WriteGuid(Guid? value);

		void WriteInt32(int? value);

		void WriteInt64(long? value);
	}
}
