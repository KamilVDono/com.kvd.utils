namespace KVD.Utils.GUIs
{
	public interface IImguiTableElements<out T>
	{
		T this[uint index]{ get; }

		uint Count{ get; }
	}
}
