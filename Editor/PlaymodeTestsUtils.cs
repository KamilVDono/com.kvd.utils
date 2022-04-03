using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace KVD.Utils.Editor
{
	public static class PlaymodeTestsUtils
	{
		public static IEnumerable LoadTestScene(string sceneName)
		{
			var scenes = AssetsFinder.Find<SceneAsset>();
			var scene  = scenes.First(s => s.name == sceneName);
			EditorSceneManager.LoadSceneInPlayMode(AssetDatabase.GetAssetPath(scene), new(LoadSceneMode.Single));

			yield return null;
		}
	}
}
