using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class SharedAccessTablePolicies : IDictionary<string, SharedAccessTablePolicy>, ICollection<KeyValuePair<string, SharedAccessTablePolicy>>, IEnumerable<KeyValuePair<string, SharedAccessTablePolicy>>, IEnumerable
	{
		private readonly Dictionary<string, SharedAccessTablePolicy> policies = new Dictionary<string, SharedAccessTablePolicy>();

		public ICollection<string> Keys => policies.Keys;

		public ICollection<SharedAccessTablePolicy> Values => policies.Values;

		public SharedAccessTablePolicy this[string key]
		{
			get
			{
				return policies[key];
			}
			set
			{
				policies[key] = value;
			}
		}

		public int Count => policies.Count;

		public bool IsReadOnly => false;

		public void Add(string key, SharedAccessTablePolicy value)
		{
			policies.Add(key, value);
		}

		public bool ContainsKey(string key)
		{
			return policies.ContainsKey(key);
		}

		public bool Remove(string key)
		{
			return policies.Remove(key);
		}

		public bool TryGetValue(string key, out SharedAccessTablePolicy value)
		{
			return policies.TryGetValue(key, out value);
		}

		public void Add(KeyValuePair<string, SharedAccessTablePolicy> item)
		{
			Add(item.Key, item.Value);
		}

		public void Clear()
		{
			policies.Clear();
		}

		public bool Contains(KeyValuePair<string, SharedAccessTablePolicy> item)
		{
			if (TryGetValue(item.Key, out SharedAccessTablePolicy value))
			{
				return string.Equals(SharedAccessTablePolicy.PermissionsToString(item.Value.Permissions), SharedAccessTablePolicy.PermissionsToString(value.Permissions), StringComparison.Ordinal);
			}
			return false;
		}

		public void CopyTo(KeyValuePair<string, SharedAccessTablePolicy>[] array, int arrayIndex)
		{
			CommonUtility.AssertNotNull("array", array);
			foreach (KeyValuePair<string, SharedAccessTablePolicy> policy in policies)
			{
				array[arrayIndex++] = policy;
			}
		}

		public bool Remove(KeyValuePair<string, SharedAccessTablePolicy> item)
		{
			if (Contains(item))
			{
				return Remove(item.Key);
			}
			return false;
		}

		public IEnumerator<KeyValuePair<string, SharedAccessTablePolicy>> GetEnumerator()
		{
			return policies.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)policies).GetEnumerator();
		}
	}
}
