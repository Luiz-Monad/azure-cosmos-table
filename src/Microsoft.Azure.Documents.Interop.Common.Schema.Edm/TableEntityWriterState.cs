namespace Microsoft.Azure.Documents.Interop.Common.Schema.Edm
{
	internal enum TableEntityWriterState
	{
		Initial,
		Name,
		Value,
		Done,
		CLosed
	}
}
