using Unity.Mathematics;
using UnityEngine;

namespace KVD.Utils.Extensions
{
	public static class PlaneExt
	{
		public static bool Intersection(this Plane plane, Ray ray, out float3 point)
		{
			if (!plane.Raycast(ray, out var dist))
			{
				point = float3.zero;
				return false;
			}

			point = ray.origin + ray.direction*dist;
			return true;
		}
	}
}
