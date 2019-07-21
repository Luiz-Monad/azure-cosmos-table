namespace Microsoft.Azure.Documents.Interop.Common.Schema.Edm
{
	internal sealed class TableEntityWriterContext
	{
		private bool hasElements;

		public bool HasElements
		{
			get
			{
				return hasElements;
			}
			set
			{
				hasElements = value;
			}
		}

		public TableEntityWriterContext(bool hasElements)
		{
			this.hasElements = hasElements;
		}

		public TableEntityWriterContext()
			: this(hasElements: false)
		{
		}
	}
}
