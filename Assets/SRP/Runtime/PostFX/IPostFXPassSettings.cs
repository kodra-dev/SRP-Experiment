namespace SRP.Runtime
{
	public interface IPostFXPassSettings
	{
		bool IsEnabled { get; }
		IPostFXPass CreatePass();
	}
}