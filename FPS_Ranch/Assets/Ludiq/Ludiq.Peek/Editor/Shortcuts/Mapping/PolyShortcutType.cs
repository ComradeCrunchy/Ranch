using System;
using Ludiq.Peek;
using Ludiq.PeekCore;
using UnityEngine;
using UnityObject = UnityEngine.Object;

[assembly: InitializeAfterPlugins(typeof(SceneDeselectIntegration))]

namespace Ludiq.Peek
{
	// ReSharper disable once RedundantUsingDirective
	using PeekCore;
	
	public enum PolyShortcutType
	{
		Mouse,
		Keyboard
	}
}