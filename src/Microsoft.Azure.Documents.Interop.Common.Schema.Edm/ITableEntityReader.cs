using System;

namespace Microsoft.Azure.Documents.Interop.Common.Schema.Edm
{
	internal interface ITableEntityReader : IDisposable
	{
		string CurrentName
		{
			get;
		}

		DataType CurrentType
		{
			get;
		}

		void Start();

		void End();

		bool MoveNext();

		string ReadString();

		byte[] ReadBinary();

		bool? ReadBoolean();

		DateTime? ReadDateTime();

		double? ReadDouble();

		Guid? ReadGuid();

		int? ReadInt32();

		long? ReadInt64();
	}
}
