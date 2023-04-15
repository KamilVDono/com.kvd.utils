using UnityEngine;
using UnityEngine.InputSystem;

namespace KVD.Utils
{
	public class RenderingInfo : MonoBehaviour
	{
		private bool _isDebugging;
		
		private void Update()
		{
			if (Keyboard.current[Key.Backquote].wasReleasedThisFrame)
			{
				_isDebugging = !_isDebugging;
			}
		}

		private void OnGUI()
		{
			if (!_isDebugging)
			{
				return;
			}
			
			var renderingStateInfo = new GUIContent(
				$"Graphics API: {SystemInfo.graphicsDeviceType}"+
				$"Multithreading mode: {SystemInfo.renderingThreadingMode}"+
#if UNITY_EDITOR
				$"\nMultithreading: {UnityEditor.PlayerSettings.MTRendering}"+
				$"\nGraphics jobs: {UnityEditor.PlayerSettings.graphicsJobs}-{UnityEditor.PlayerSettings.graphicsJobMode}"+
#endif
#if ENABLE_IL2CPP
				"\nIL2CPP backed"
#else
				"\nMono backed"
#endif
				);
			
			GUILayout.Box(renderingStateInfo);
		}
	}
}
