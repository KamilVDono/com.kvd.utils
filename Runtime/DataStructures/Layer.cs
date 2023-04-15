using System;
using UnityEngine;

namespace KVD.Utils.DataStructures
{
	[Serializable]
	public class Layer
	{
		[SerializeField] public int Value;
		
		public static implicit operator int(Layer layer) => layer.Value;
	}
}
