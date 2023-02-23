namespace SRP.Shared
{
	public enum CacheMissAction
	{
		ReturnNull,
		GetFromGo,
		GetOrCreateFromGo,
	}
		
	public enum CacheWritePolicy
	{
		Always,
		IfNonNull,
	}
}