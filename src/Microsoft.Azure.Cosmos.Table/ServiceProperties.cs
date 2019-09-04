using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class ServiceProperties
	{
		internal const string StorageServicePropertiesName = "StorageServiceProperties";

		internal const string LoggingName = "Logging";

		internal const string HourMetricsName = "HourMetrics";

		internal const string CorsName = "Cors";

		internal const string MinuteMetricsName = "MinuteMetrics";

		internal const string VersionName = "Version";

		internal const string DeleteName = "Delete";

		internal const string ReadName = "Read";

		internal const string WriteName = "Write";

		internal const string RetentionPolicyName = "RetentionPolicy";

		internal const string EnabledName = "Enabled";

		internal const string DaysName = "Days";

		internal const string IncludeApisName = "IncludeAPIs";

		internal const string DefaultServiceVersionName = "DefaultServiceVersion";

		internal const string CorsRuleName = "CorsRule";

		internal const string AllowedOriginsName = "AllowedOrigins";

		internal const string AllowedMethodsName = "AllowedMethods";

		internal const string MaxAgeInSecondsName = "MaxAgeInSeconds";

		internal const string ExposedHeadersName = "ExposedHeaders";

		internal const string AllowedHeadersName = "AllowedHeaders";

		public LoggingProperties Logging
		{
			get;
			set;
		}

		public MetricsProperties HourMetrics
		{
			get;
			set;
		}

		public CorsProperties Cors
		{
			get;
			set;
		}

		public MetricsProperties MinuteMetrics
		{
			get;
			set;
		}

		public string DefaultServiceVersion
		{
			get;
			set;
		}

		public ServiceProperties()
		{
		}

		public ServiceProperties(LoggingProperties logging = null, MetricsProperties hourMetrics = null, MetricsProperties minuteMetrics = null, CorsProperties cors = null)
		{
			Logging = logging;
			HourMetrics = hourMetrics;
			MinuteMetrics = minuteMetrics;
			Cors = cors;
		}

		internal static Task<ServiceProperties> FromServiceXmlAsync(XDocument servicePropertiesDocument)
		{
			XElement xElement = servicePropertiesDocument.Element("StorageServiceProperties");
			ServiceProperties serviceProperties = new ServiceProperties
			{
				Logging = ReadLoggingPropertiesFromXml(xElement.Element("Logging")),
				HourMetrics = ReadMetricsPropertiesFromXml(xElement.Element("HourMetrics")),
				MinuteMetrics = ReadMetricsPropertiesFromXml(xElement.Element("MinuteMetrics")),
				Cors = ReadCorsPropertiesFromXml(xElement.Element("Cors"))
			};
			XElement xElement2 = xElement.Element("DefaultServiceVersion");
			if (xElement2 != null)
			{
				serviceProperties.DefaultServiceVersion = xElement2.Value;
			}
			return Task.FromResult(serviceProperties);
		}

		internal XDocument ToServiceXml()
		{
			if (Logging == null && HourMetrics == null && MinuteMetrics == null && Cors == null)
			{
				throw new InvalidOperationException("At least one service property needs to be non-null for SetServiceProperties API.");
			}
			XElement xElement = new XElement("StorageServiceProperties");
			if (Logging != null)
			{
				xElement.Add(GenerateLoggingXml(Logging));
			}
			if (HourMetrics != null)
			{
				xElement.Add(GenerateMetricsXml(HourMetrics, "HourMetrics"));
			}
			if (MinuteMetrics != null)
			{
				xElement.Add(GenerateMetricsXml(MinuteMetrics, "MinuteMetrics"));
			}
			if (Cors != null)
			{
				xElement.Add(GenerateCorsXml(Cors));
			}
			if (DefaultServiceVersion != null)
			{
				xElement.Add(new XElement("DefaultServiceVersion", DefaultServiceVersion));
			}
			return new XDocument(xElement);
		}

		private static XElement GenerateRetentionPolicyXml(int? retentionDays)
		{
			bool hasValue = retentionDays.HasValue;
			XElement xElement = new XElement("RetentionPolicy", new XElement("Enabled", hasValue));
			if (hasValue)
			{
				xElement.Add(new XElement("Days", retentionDays.Value));
			}
			return xElement;
		}

		private static XElement GenerateMetricsXml(MetricsProperties metrics, string metricsName)
		{
			if (!Enum.IsDefined(typeof(MetricsLevel), metrics.MetricsLevel))
			{
				throw new InvalidOperationException("Invalid metrics level specified.");
			}
			if (string.IsNullOrEmpty(metrics.Version))
			{
				throw new InvalidOperationException("The metrics version is null or empty.");
			}
			bool flag = metrics.MetricsLevel != MetricsLevel.None;
			XElement xElement = new XElement(metricsName, new XElement("Version", metrics.Version), new XElement("Enabled", flag), GenerateRetentionPolicyXml(metrics.RetentionDays));
			if (flag)
			{
				xElement.Add(new XElement("IncludeAPIs", metrics.MetricsLevel == MetricsLevel.ServiceAndApi));
			}
			return xElement;
		}

		private static XElement GenerateLoggingXml(LoggingProperties logging)
		{
			if ((LoggingOperations.All & logging.LoggingOperations) != logging.LoggingOperations)
			{
				throw new InvalidOperationException("Invalid logging operations specified.");
			}
			if (string.IsNullOrEmpty(logging.Version))
			{
				throw new InvalidOperationException("The logging version is null or empty.");
			}
			return new XElement("Logging", new XElement("Version", logging.Version), new XElement("Delete", (logging.LoggingOperations & LoggingOperations.Delete) != LoggingOperations.None), new XElement("Read", (logging.LoggingOperations & LoggingOperations.Read) != LoggingOperations.None), new XElement("Write", (logging.LoggingOperations & LoggingOperations.Write) != LoggingOperations.None), GenerateRetentionPolicyXml(logging.RetentionDays));
		}

		private static XElement GenerateCorsXml(CorsProperties cors)
		{
			CommonUtility.AssertNotNull("cors", cors);
			IList<CorsRule> corsRules = cors.CorsRules;
			XElement xElement = new XElement("Cors");
			foreach (CorsRule item in corsRules)
			{
				if (item.AllowedOrigins.Count < 1 || item.AllowedMethods == CorsHttpMethods.None || item.MaxAgeInSeconds < 0)
				{
					throw new InvalidOperationException("A CORS rule must contain at least one allowed origin and allowed method, and MaxAgeInSeconds cannot have a value less than zero.");
				}
				XElement content = new XElement("CorsRule", new XElement("AllowedOrigins", string.Join(",", item.AllowedOrigins.ToArray())), new XElement("AllowedMethods", item.AllowedMethods.ToString().Replace(" ", string.Empty).ToUpperInvariant()), new XElement("ExposedHeaders", string.Join(",", item.ExposedHeaders.ToArray())), new XElement("AllowedHeaders", string.Join(",", item.AllowedHeaders.ToArray())), new XElement("MaxAgeInSeconds", item.MaxAgeInSeconds));
				xElement.Add(content);
			}
			return xElement;
		}

		private static LoggingProperties ReadLoggingPropertiesFromXml(XElement element)
		{
			if (element == null)
			{
				return null;
			}
			LoggingOperations loggingOperations = LoggingOperations.None;
			if (bool.Parse(element.Element("Delete").Value))
			{
				loggingOperations |= LoggingOperations.Delete;
			}
			if (bool.Parse(element.Element("Read").Value))
			{
				loggingOperations |= LoggingOperations.Read;
			}
			if (bool.Parse(element.Element("Write").Value))
			{
				loggingOperations |= LoggingOperations.Write;
			}
			return new LoggingProperties
			{
				Version = element.Element("Version").Value,
				LoggingOperations = loggingOperations,
				RetentionDays = ReadRetentionPolicyFromXml(element.Element("RetentionPolicy"))
			};
		}

		internal static MetricsProperties ReadMetricsPropertiesFromXml(XElement element)
		{
			if (element == null)
			{
				return null;
			}
			MetricsLevel metricsLevel = MetricsLevel.None;
			if (bool.Parse(element.Element("Enabled").Value))
			{
				metricsLevel = MetricsLevel.Service;
				if (bool.Parse(element.Element("IncludeAPIs").Value))
				{
					metricsLevel = MetricsLevel.ServiceAndApi;
				}
			}
			return new MetricsProperties
			{
				Version = element.Element("Version").Value,
				MetricsLevel = metricsLevel,
				RetentionDays = ReadRetentionPolicyFromXml(element.Element("RetentionPolicy"))
			};
		}

		internal static CorsProperties ReadCorsPropertiesFromXml(XElement element)
		{
			if (element == null)
			{
				return null;
			}
			CorsProperties corsProperties = new CorsProperties();
			IEnumerable<XElement> source = element.Descendants("CorsRule");
			corsProperties.CorsRules = (from rule in source
			select new CorsRule
			{
				AllowedOrigins = rule.Element("AllowedOrigins").Value.Split(new char[1]
				{
					','
				}).ToList(),
				AllowedMethods = (CorsHttpMethods)Enum.Parse(typeof(CorsHttpMethods), rule.Element("AllowedMethods").Value, ignoreCase: true),
				AllowedHeaders = rule.Element("AllowedHeaders").Value.Split(new char[1]
				{
					','
				}, StringSplitOptions.RemoveEmptyEntries).ToList(),
				ExposedHeaders = rule.Element("ExposedHeaders").Value.Split(new char[1]
				{
					','
				}, StringSplitOptions.RemoveEmptyEntries).ToList(),
				MaxAgeInSeconds = int.Parse(rule.Element("MaxAgeInSeconds").Value, CultureInfo.InvariantCulture)
			}).ToList();
			return corsProperties;
		}

		private static int? ReadRetentionPolicyFromXml(XElement element)
		{
			if (!bool.Parse(element.Element("Enabled").Value))
			{
				return null;
			}
			return int.Parse(element.Element("Days").Value, CultureInfo.InvariantCulture);
		}

		internal void WriteServiceProperties(Stream outputStream)
		{
			using (XmlWriter writer = XmlWriter.Create(outputStream, new XmlWriterSettings
			{
				Encoding = Encoding.UTF8,
				NewLineHandling = NewLineHandling.Entitize
			}))
			{
				ToServiceXml().Save(writer);
			}
		}
	}
}
