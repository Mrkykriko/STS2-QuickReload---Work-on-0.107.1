using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;

namespace QuickReload;

[HarmonyPatch(typeof(NRun), "_Process")]
internal static class GameplayHotkeyPatch
{
	private static int _restartInProgress;

	private static bool _f5WasPressed;

	private static void Postfix()
	{
		if (!Input.IsKeyPressed(Key.F5))
		{
			_f5WasPressed = false;
		}
		else if (!_f5WasPressed)
		{
			_f5WasPressed = true;
			NGame? instance = NGame.Instance;
			if (instance != null && instance.Transition?.InTransition == true)
			{
				Log.Info("[QUICKRELOAD]: Ignoring F5 quick reload while a transition is in progress.");
				return;
			}
			if (QuickReloadState.IsRunStartupNetGuardActive())
			{
				Log.Info("[QUICKRELOAD]: Ignoring F5 quick reload while startup net guard is active.");
				return;
			}
			if (RunManager.Instance.NetService.Type == NetGameType.Client)
			{
				Log.Info("[QUICKRELOAD]: Ignoring F5 quick reload on client.");
				return;
			}
			if (Interlocked.CompareExchange(ref _restartInProgress, 1, 0) != 0)
			{
				Log.Info("[QUICKRELOAD]: F5 quick reload ignored because another restart is already in progress.");
				return;
			}
			Log.Info("[QUICKRELOAD]: F5 pressed during gameplay, triggering quick reload.");
			TaskHelper.RunSafely(RestartFromHotkey());
		}
	}

	private static async Task RestartFromHotkey()
	{
		try
		{
			await QuickReloadRunner.RestartAsync();
		}
		finally
		{
			Interlocked.Exchange(ref _restartInProgress, 0);
		}
	}
}
