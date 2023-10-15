using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace KVD.Utils.GUIs
{
	[RequireComponent(typeof(UIDocument))]
	public class UIToolkitHoveredUI : HoveredUIBase
	{
		private UIDocument _uiDocument;
		private VisualElement _root;

		private void Start()
		{
			_uiDocument = GetComponent<UIDocument>();
			_root = _uiDocument.rootVisualElement;
		}

		private void Update()
		{
			var mousePosition = Mouse.current.position.ReadValue();
			mousePosition.y = Screen.height - mousePosition.y;
			var localPoint    = RuntimePanelUtils.ScreenToPanel(_root.panel, mousePosition);
			var hasLocalPoint = false;
			for (var i = 0; !hasLocalPoint && i < _root.childCount; i++)
			{
				var child = _root[i];
				hasLocalPoint = child.layout.Contains(localPoint);
			}

			if (hasLocalPoint)
			{
				HoveredUIs.Add(this);
			}
			else
			{
				HoveredUIs.Remove(this);
			}
		}
	}
}
