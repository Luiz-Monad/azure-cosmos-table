using System;
using System.Text;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class SharedAccessTablePolicy
	{
		public DateTimeOffset? SharedAccessStartTime
		{
			get;
			set;
		}

		public DateTimeOffset? SharedAccessExpiryTime
		{
			get;
			set;
		}

		public SharedAccessTablePermissions Permissions
		{
			get;
			set;
		}

		public static string PermissionsToString(SharedAccessTablePermissions permissions)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if ((permissions & SharedAccessTablePermissions.Query) == SharedAccessTablePermissions.Query)
			{
				stringBuilder.Append("r");
			}
			if ((permissions & SharedAccessTablePermissions.Add) == SharedAccessTablePermissions.Add)
			{
				stringBuilder.Append("a");
			}
			if ((permissions & SharedAccessTablePermissions.Update) == SharedAccessTablePermissions.Update)
			{
				stringBuilder.Append("u");
			}
			if ((permissions & SharedAccessTablePermissions.Delete) == SharedAccessTablePermissions.Delete)
			{
				stringBuilder.Append("d");
			}
			return stringBuilder.ToString();
		}

		public static SharedAccessTablePermissions PermissionsFromString(string input)
		{
			CommonUtility.AssertNotNull("input", input);
			SharedAccessTablePermissions sharedAccessTablePermissions = SharedAccessTablePermissions.None;
			for (int i = 0; i < input.Length; i++)
			{
				switch (input[i])
				{
				case 'a':
					sharedAccessTablePermissions |= SharedAccessTablePermissions.Add;
					break;
				case 'd':
					sharedAccessTablePermissions |= SharedAccessTablePermissions.Delete;
					break;
				case 'r':
					sharedAccessTablePermissions |= SharedAccessTablePermissions.Query;
					break;
				case 'u':
					sharedAccessTablePermissions |= SharedAccessTablePermissions.Update;
					break;
				default:
					throw new ArgumentOutOfRangeException("input");
				}
			}
			// if (sharedAccessTablePermissions == SharedAccessTablePermissions.None)
			// {
			// 	sharedAccessTablePermissions = sharedAccessTablePermissions;
			// }
			return sharedAccessTablePermissions;
		}
	}
}
