using System;
using System.Linq;

namespace SRP.Shared.Reflection
{
	public static class TypeUtils
	{
		#if UNITY_EDITOR
		public static Type[] GetImplementations<T>()
		{
			return GetImplementations(typeof(T));
		}
		
		public static Type[] GetImplementations(Type interfaceType)
		{
			var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes());
			return types.Where(p => interfaceType.IsAssignableFrom(p) && !p.IsAbstract).ToArray();
		}
		
		#endif
		
	}
}