using System;
using System.Collections.Generic;
using SRP.Shared.Attributes;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.Serialization;

namespace SRP.Runtime
{
	[Serializable]
	public class PostFXSettings
	{
		[SerializeReference][Polymorphic(typeof(IPostFXPassSettings))]
		public List<IPostFXPassSettings> PassSettings = new();

	}
}