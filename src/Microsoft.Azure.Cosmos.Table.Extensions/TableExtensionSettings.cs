using System;

namespace Microsoft.Azure.Cosmos.Table.Extensions
{
	internal static class TableExtensionSettings
	{
		public static bool EnableAppSettingsBasedOptions
		{
			get;
			private set;
		}

		public static int? MaxItemCount
		{
			get;
			private set;
		}

		public static bool EnableScan
		{
			get;
			private set;
		}

		public static int MaxDegreeOfParallelism
		{
			get;
			private set;
		}

		public static int? ContinuationTokenLimitInKb
		{
			get;
			private set;
		}

		static TableExtensionSettings()
		{
			LoadFrom(new AppSettingsWrapper());
		}

		public static void LoadFrom(AppSettingsWrapper settingsWrapper)
		{
			EnableAppSettingsBasedOptions = ConvertTo(settingsWrapper.GetValue("EnableTableQueryOptions"), defaultValue: false);
			MaxItemCount = ConvertTo(settingsWrapper.GetValue("TableQueryMaxItemCount"), (int?)1000);
			EnableScan = ConvertTo(settingsWrapper.GetValue("TableQueryEnableScan"), defaultValue: false);
			MaxDegreeOfParallelism = ConvertTo(settingsWrapper.GetValue("TableQueryMaxDegreeOfParallelism"), -1);
			ContinuationTokenLimitInKb = ConvertTo<int?>(settingsWrapper.GetValue("TableQueryContinuationTokenLimitInKb"));
			if (!EnableAppSettingsBasedOptions && !string.IsNullOrEmpty(settingsWrapper.GetValue("TableQueryMaxItemCount")) && !string.IsNullOrEmpty(settingsWrapper.GetValue("TableQueryEnableScan")) && !string.IsNullOrEmpty(settingsWrapper.GetValue("TableQueryMaxDegreeOfParallelism")) && !string.IsNullOrEmpty(settingsWrapper.GetValue("TableQueryContinuationTokenLimitInKb")))
			{
				throw new NotSupportedException("Options set in the App.config are not supported. Please use TableRequestOptions.");
			}
		}

		private static T ConvertTo<T>(string value, T defaultValue = default(T))
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			Type typeFromHandle = typeof(T);
			return ConvertTo<T>(Nullable.GetUnderlyingType(typeFromHandle) ?? typeFromHandle, value);
		}

		private static T ConvertTo<T>(Type underlyingType, string value)
		{
			if (underlyingType.IsEnum)
			{
				return (T)Enum.Parse(underlyingType, value);
			}
			return (T)Convert.ChangeType(value, underlyingType);
		}
	}
}
