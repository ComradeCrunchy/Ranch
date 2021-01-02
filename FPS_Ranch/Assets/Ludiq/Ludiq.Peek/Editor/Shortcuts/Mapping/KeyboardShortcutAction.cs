using Ludiq.Peek;
using Ludiq.PeekCore;
using UnityObject = UnityEngine.Object;

[assembly: InitializeAfterPlugins(typeof(SceneDeselectIntegration))]

namespace Ludiq.Peek
{
	// ReSharper disable once RedundantUsingDirective
	using PeekCore;

	public enum KeyboardShortcutAction
	{
		Press
	}
}