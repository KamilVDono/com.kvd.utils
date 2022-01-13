using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KVD.Utils
{
	[RequireComponent(typeof(EventTrigger))]
	public class UIChecker : MonoBehaviour
	{
		private static readonly HashSet<UIChecker> SelectedCheckers = new();
		public static bool IsPointerOverUI => SelectedCheckers.Count > 0;
		
#nullable disable
		[SerializeField] private EventTrigger _eventTrigger;
		private EventTrigger.Entry _enterUIEntry;
		private EventTrigger.Entry _exitUIEntry;
#nullable enable
 
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
			SelectedCheckers.Add(this);
		}
		private void ExitUI(BaseEventData? _)
		{
			SelectedCheckers.Remove(this);
		}
	}
}
