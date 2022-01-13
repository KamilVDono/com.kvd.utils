using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace KVD.Utils.DataStructures
{
	[Serializable]
	public struct Line2
	{
		public float2 point1;
		public float2 point2;

		public Line2(float2 point1, float2 point2)
		{
			this.point1 = point1;
			this.point2 = point2;
		}
		
		public Line2(Ray ray, float distance)
		{
			point1 = new(ray.origin.x, ray.origin.z);
			var endPoint  = ray.origin + ray.direction*distance;
			point2 = new(endPoint.x, endPoint.z);
		}

		// Based on Wikipedia
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float DistanceLine(float3 point)
		{
			return DistanceLine(new float2(point.x, point.z));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float DistanceLine(float2 point)
		{
			var gradient = point2 - point1;
			var dividend = math.abs(gradient.x*(point1.y-point.y) - gradient.y*(point1.x-point.x));
			var divisor  = math.sqrt(gradient.x*gradient.x + gradient.y*gradient.y);
			return dividend/divisor;
		}
		
		// Based on https://diego.assencio.com/?index=ec3d5dfdfc0b6a0d147a656f0af332bd
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool DistanceSegment(float3 point, out float distance)
		{
			return DistanceSegment(new float2(point.x, point.z), out distance);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool DistanceSegment(float2 point, out float distance)
		{
			distance = DistanceLine(point);
			
			var lineDir      = point2-point1;
			var dividend     = math.dot(point-point1, lineDir);
			var divisor      = math.dot(lineDir, lineDir);
			var segmentCoeff = dividend / divisor;
			return segmentCoeff is > 0 and < 1;
		}
	}
}
