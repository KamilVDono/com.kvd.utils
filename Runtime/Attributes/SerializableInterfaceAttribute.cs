using System;
using UnityEngine;

namespace KVD.Utils.Attributes
{
	public class SerializableInterfaceAttribute : PropertyAttribute
	{
		public Type InterfaceType{ get; }

		public SerializableInterfaceAttribute(Type interfaceType)
		{
			InterfaceType = interfaceType;
		}
	}
}
