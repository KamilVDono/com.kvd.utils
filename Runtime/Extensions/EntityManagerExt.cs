using Unity.Entities;

namespace KVD.Utils.Extensions
{
	public static class EntityManagerExt
	{
		public static void AddOrSetComponentData<T>(this EntityManager manager, Entity entity, T componentData) where T : unmanaged, IComponentData
		{
			if (manager.HasComponent<T>(entity))
			{
				manager.SetComponentData(entity, componentData);
			}
			else
			{
				manager.AddComponentData(entity, componentData);
			}
		}

		public static T GetComponentDataOrDefault<T>(this EntityManager manager, Entity entity) where T : unmanaged, IComponentData
		{
			return manager.HasComponent<T>(entity) ? manager.GetComponentData<T>(entity) : default;
		}
	}
}
