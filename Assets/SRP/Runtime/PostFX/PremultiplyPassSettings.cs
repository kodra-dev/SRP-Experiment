using System;

namespace SRP.Runtime
{
	[Serializable]
	public class PremultiplyPassSettings : IPostFXPassSettings
	{
		public bool IsEnabled => true;
		public IPostFXPass CreatePass()
		{
			return new PremultiplyPass(this);
		}
	}
}