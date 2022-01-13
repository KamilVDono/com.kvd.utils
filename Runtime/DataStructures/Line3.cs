using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace KVD.Utils.DataStructures
{
	[Serializable]
	public struct Line3
	{
		private readonly float _lineDirDot;
		
		public readonly float3 point1;
		public readonly float3 point2;

		private readonly float3 _lineDir;
		private readonly float3 _lineDirNormalized;

		public Line3(float3 point1, float3 point2)
		{
			this.point1       = point1;
			this.point2       = point2;
			
			_lineDir           = point2-point1;
			_lineDirNormalized = math.normalize(_lineDir);
			_lineDirDot        = math.dot(_lineDir, _lineDir);
		}
		
		public Line3(Ray ray, float distance)
		{
			point1            = ray.origin;
			point2            = ray.origin + ray.direction*distance;
			
			_lineDir           = point2-point1;
			_lineDirNormalized = math.normalize(_lineDir);
			_lineDirDot        = math.dot(_lineDir, _lineDir);
		}

		// Based on Wikipedia
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float DistanceLine(float3 point)
		{
			var lineToPoint = point-point1;
			return math.length(lineToPoint - math.dot(lineToPoint, _lineDirNormalized)*_lineDirNormalized);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float DistanceLineSq(float3 point)
		{
			var lineToPoint = point-point1;
			return math.lengthsq(lineToPoint - math.dot(lineToPoint, _lineDirNormalized)*_lineDirNormalized);
		}
		
		// Based on https://diego.assencio.com/?index=ec3d5dfdfc0b6a0d147a656f0af332bd
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool DistanceSegment(float3 point, out float distance)
		{
			distance = float.MaxValue;
			
			var dividend     = math.dot(point-point1, _lineDir);
			var divisor      = _lineDirDot;
			var segmentCoeff = dividend / divisor;
			var inSegment    = segmentCoeff is > 0 and < 1;
			
			if (inSegment)
			{
				distance = DistanceLine(point);
			}
			return inSegment;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool DistanceSqSegment(float3 point, out float distanceSq)
		{
			distanceSq = float.MaxValue;
			
			var dividend     = math.dot(point-point1, _lineDir);
			var divisor      = _lineDirDot;
			var segmentCoeff = dividend / divisor;
			var inSegment    = segmentCoeff is > 0 and < 1;
			
			if (inSegment)
			{
				distanceSq = DistanceLineSq(point);
			}
			return inSegment;
		}
	}
}
