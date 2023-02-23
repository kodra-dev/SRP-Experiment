using SRP.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SRP.Runtime
{
	public static class GlobalEventSupervisor
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		public static void RegisterSceneEvents()
		{
			SceneManager.activeSceneChanged -= OnSceneChanged;
			Application.quitting -= OnApplicationQuitting;
			Debug.Log("Registering scene events...");
			Application.quitting += OnApplicationQuitting;
			SceneManager.activeSceneChanged += OnSceneChanged;
		}
		
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void InitFreshSceneState()
		{
			SceneCache.Clear();
			Debug.Log("Initialized a fresh scene state.");
		}
		
		// We need this for Play Mode -> Edit Mode
		private static void OnApplicationQuitting()
		{
			InitFreshSceneState();
		}
		
		private static void OnSceneChanged(Scene oldScene, Scene newScene)
		{
			InitFreshSceneState();
		}

	}
}