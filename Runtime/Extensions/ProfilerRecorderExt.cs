using Unity.Profiling;

namespace KVD.Utils.Extensions
{
	public static class ProfilerRecorderExt
	{
		public static double GetRecorderAverageTime(this ProfilerRecorder recorder)
		{
			var samplesCount = recorder.Capacity;
			if (samplesCount == 0)
				return 0;

			double r = 0;
			unsafe
			{
				var samples = stackalloc ProfilerRecorderSample[samplesCount];
				recorder.CopyTo(samples, samplesCount);
				for (var i = 0; i < samplesCount; ++i)
					r += samples[i].Value;
				r /= samplesCount;
			}

			return r * 1e-6f;
		}
	}
}
