namespace Microsoft.Azure.Documents.Interop.Common.Schema
{
	internal enum DataType
	{
		Guid = 0,
		Double = 1,
		String = 2,
		Document = 3,
		Array = 4,
		Binary = 5,
		Undefined = 6,
		ObjectId = 7,
		Boolean = 8,
		DateTime = 9,
		Null = 10,
		RegularExpression = 11,
		JavaScript = 13,
		Symbol = 14,
		JavaScriptWithScope = 0xF,
		Int32 = 0x10,
		Timestamp = 17,
		Int64 = 18,
		MaxKey = 0x7F,
		MinKey = 0xFF
	}
}
