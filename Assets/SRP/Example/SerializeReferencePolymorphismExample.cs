using System;
using System.Collections.Generic;
using SRP.Shared.Attributes;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable CollectionNeverQueried.Global

namespace Example
{

	public class SerializeReferencePolymorphismExample : MonoBehaviour
	{
		public interface FruitBase
		{
		}

		[Serializable]
		public class Apple : FruitBase
		{
			public string m_Description = "Ripe";
		}

		[Serializable]
		public class Orange : FruitBase
		{
			public bool m_IsRound = true;
		}

		[Serializable]
		public class Banana : FruitBase
		{
			public string m_name = "Banana";
			public bool m_IsYellow = true;
		}

		[Serializable]
		public class Pineapple : FruitBase
		{
			[ReadOnly] public string m_name = "Pineapple";
			public bool m_IsYellow = true;
		}
		

		// Use SerializeReference if this field needs to hold both
		// Apples and Oranges.  Otherwise only m_Data from Base object would be serialized
		[SerializeReference] public FruitBase normalReference = new Apple();

		[SerializeReference] [Polymorphic(typeof(FruitBase))]
		public FruitBase polymorphicFruit = new Orange();

		[SerializeReference] [Polymorphic(typeof(FruitBase))]
		private List<FruitBase> polymorphicList =
			new List<FruitBase> {new Apple(), new Orange()};



		// Use by-value instead of SerializeReference, because
		// no polymorphism and no other field needs to share this object
		public Apple m_MyApple = new Apple();
	}

}
