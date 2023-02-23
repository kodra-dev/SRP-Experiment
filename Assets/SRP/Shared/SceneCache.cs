using System;
using System.Collections.Generic;
using SRP.Runtime;
using UnityEngine;

namespace SRP.Shared
{
	// Scene-level cache. It gets cleared when the scene changes
	public static partial class SceneCache
	{
		private delegate void ClearCacheDelegate();
		
		private static List<ClearCacheDelegate> _clearCacheDelegates = new();

		private static class GenericCache<T> where T : Component
		{
			public static readonly Dictionary<int, T> CacheDict;
			
			static GenericCache()
			{
				CacheDict = new Dictionary<int, T>();
				_clearCacheDelegates.Add(() => CacheDict.Clear());
			}
		}
		
		public static T ObtainComponent<T>(
			GameObject go,
			CacheMissAction missAction = CacheMissAction.GetFromGo,
			CacheWritePolicy writePolicy = CacheWritePolicy.Always)
			where T : Component
		{
			int id = go.GetInstanceID();
			var cacheDict = GenericCache<T>.CacheDict;
			if (cacheDict.TryGetValue(id, out var cached))
			{
				return cached;
			}

			T component;
			switch (missAction)
			{
				case CacheMissAction.ReturnNull:
					return null;
				case CacheMissAction.GetFromGo:
					component = go.GetComponent<T>();
					if (writePolicy == CacheWritePolicy.Always || component != null)
					{
						cacheDict.Add(id, component);
					}
					return component;
				case CacheMissAction.GetOrCreateFromGo:
					component = go.GetComponent<T>();
					if (component == null)
					{
						component = go.AddComponent<T>();
					}
					if (writePolicy == CacheWritePolicy.Always || component != null)
					{
						cacheDict.Add(id, component);
					}

					return component;
				default:
					throw new ArgumentOutOfRangeException(nameof(missAction), missAction, null);
			}
		}
		
		public static T ObtainComponent<T>(Component component,
			CacheMissAction missAction = CacheMissAction.GetFromGo,
			CacheWritePolicy writePolicy = CacheWritePolicy.Always)
			where T : Component
		{
			return ObtainComponent<T>(component.gameObject, missAction, writePolicy);
		}

		public static void Clear()
		{
			foreach (ClearCacheDelegate d in _clearCacheDelegates)
			{
				d();
			}
		}

	}
}