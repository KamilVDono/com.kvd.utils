using UnityEngine;
using UnityEngine.EventSystems;

namespace KVD.Utils.GUIs
{
	[RequireComponent(typeof(EventTrigger))]
	public class UguiHoveredUI : HoveredUIBase
	{
		[SerializeField] private EventTrigger _eventTrigger;
		private EventTrigger.Entry _enterUIEntry;
		private EventTrigger.Entry _exitUIEntry;

		private void Awake()
		{
			_enterUIEntry = new()
			{
				eventID = EventTriggerType.PointerEnter,
			};
			_enterUIEntry.callback.AddListener(EnterUI);
			_eventTrigger.triggers.Add(_enterUIEntry);

			_exitUIEntry = new()
			{
				eventID = EventTriggerType.PointerExit,
			};
			_exitUIEntry.callback.AddListener(ExitUI);
			_eventTrigger.triggers.Add(_exitUIEntry);
		}

		private void OnDestroy()
		{
			_eventTrigger.triggers.Remove(_enterUIEntry);
			_enterUIEntry.callback.RemoveAllListeners();

			_eventTrigger.triggers.Remove(_exitUIEntry);
			_exitUIEntry.callback.RemoveAllListeners();

			ExitUI(null);
		}

		private void EnterUI(BaseEventData _)
		{
			HoveredUIs.Add(this);
		}

		private void ExitUI(BaseEventData? _)
		{
			HoveredUIs.Remove(this);
		}
	}
}
