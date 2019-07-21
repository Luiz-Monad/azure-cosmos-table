using Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand;
using System;
using System.IO;
using System.Xml.Linq;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.Common
{
	internal class TableAccessPolicyResponse : AccessPolicyResponseBase<SharedAccessTablePolicy>
	{
		internal TableAccessPolicyResponse(Stream stream)
			: base(stream)
		{
		}

		protected override SharedAccessTablePolicy ParseElement(XElement accessPolicyElement)
		{
			CommonUtility.AssertNotNull("accessPolicyElement", accessPolicyElement);
			SharedAccessTablePolicy sharedAccessTablePolicy = new SharedAccessTablePolicy();
			string text = (string)accessPolicyElement.Element("Start");
			if (!string.IsNullOrEmpty(text))
			{
				sharedAccessTablePolicy.SharedAccessStartTime = Uri.UnescapeDataString(text).ToUTCTime();
			}
			string text2 = (string)accessPolicyElement.Element("Expiry");
			if (!string.IsNullOrEmpty(text2))
			{
				sharedAccessTablePolicy.SharedAccessExpiryTime = Uri.UnescapeDataString(text2).ToUTCTime();
			}
			string text3 = (string)accessPolicyElement.Element("Permission");
			if (!string.IsNullOrEmpty(text3))
			{
				sharedAccessTablePolicy.Permissions = SharedAccessTablePolicy.PermissionsFromString(text3);
			}
			return sharedAccessTablePolicy;
		}
	}
}
