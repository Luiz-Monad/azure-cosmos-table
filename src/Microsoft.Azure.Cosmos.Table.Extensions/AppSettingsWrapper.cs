using System.Collections.Generic;

namespace Microsoft.Azure.Cosmos.Table.Extensions
{
	internal class AppSettingsWrapper
	{
		private readonly Dictionary<string, string> propertyBag;

		public AppSettingsWrapper()
		{
			propertyBag = new Dictionary<string, string>();
		}

		public void AddSetting(string key, string value)
		{
			propertyBag[key] = value;
		}

		public string GetValue(string key)
		{
			if (!propertyBag.ContainsKey(key))
			{
				return null;
			}
			return propertyBag[key];
		}
	}
}
