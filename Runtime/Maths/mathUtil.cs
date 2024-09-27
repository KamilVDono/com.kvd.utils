using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace KVD.Utils.Maths
{
	public static class mathUtil
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float3x4 orthonormal(this in float4x4 fullMatrix)
		{
			return new float3x4(fullMatrix.c0.xyz, fullMatrix.c1.xyz, fullMatrix.c2.xyz, fullMatrix.c3.xyz);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float3x4 mul(in float3x4 a, in float3x4 b)
		{
			var x = new float4x4(
				new float4(a.c0, 0f),
				new float4(a.c1, 0f),
				new float4(a.c2, 0f),
				new float4(a.c3, 1f)
				);

			var y = new float4x4(
				new float4(b.c0, 0f),
				new float4(b.c1, 0f),
				new float4(b.c2, 0f),
				new float4(b.c3, 1f)
				);

			var r = math.mul(x, y);

			return r.orthonormal();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float4x4 toFloat4x4(this in float3x4 orthonormal)
		{
			return new float4x4(
				new float4(orthonormal.c0, 0f),
				new float4(orthonormal.c1, 0f),
				new float4(orthonormal.c2, 0f),
				new float4(orthonormal.c3, 1f));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float3 extractPosition(this in float3x4 orthonormal)
		{
			return orthonormal.c3;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static quaternion extractRotation(this in float3x4 orthonormal)
		{
			return math.quaternion(orthonormal.toFloat4x4());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float3 extractScale(this in float3x4 orthonormal)
		{
			return new float3(
				math.length(orthonormal.c0),
				math.length(orthonormal.c1),
				math.length(orthonormal.c2)
				);
		}
	}
}
