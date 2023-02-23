using System;
using UnityEngine;

namespace SRP.Shared.Attributes
{

	public class PolymorphicAttribute : PropertyAttribute
	{
		public Type FieldType;

		public PolymorphicAttribute(Type fieldType)
		{
			FieldType = fieldType;
		}
	}
}