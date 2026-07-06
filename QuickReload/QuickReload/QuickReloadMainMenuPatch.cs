using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Connection;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace QuickReload;

[HarmonyPatch(typeof(NMainMenu), "_Ready")]
internal static class QuickReloadMainMenuPatch
{
	private static void Postfix(NMainMenu __instance)
	{
		if (!QuickReloadState.TryConsumePendingRestart(out var playerId))
		{
			Log.Info("[QUICKRELOAD]: Tried to consume pending restart on main menu, but none was pending.");
			return;
		}
		Log.Info($"[QUICKRELOAD]: Consumed pending restart on main menu. playerId={playerId}");
		if (CommandLineHelper.HasArg("fastmp") || CommandLineHelper.HasArg("clientId"))
		{
			QuickReloadState.SetAutoReady(autoReady: true);
			__instance.OpenMultiplayerSubmenu().OnJoinFriendsPressed();
			Log.Info("[QUICKRELOAD]: Using fastmp quick-restart join flow via Join Friends screen.");
			return;
		}
		SteamClientConnectionInitializer steamClientConnectionInitializer = SteamClientConnectionInitializer.FromPlayer(playerId);
		if (steamClientConnectionInitializer == null)
		{
			Log.Warn("[QUICKRELOAD]: Failed to create Steam connection initializer from player ID, aborting quick restart.");
			return;
		}
		QuickReloadState.SetAutoReady(autoReady: true);
		TaskHelper.RunSafely(__instance.JoinGame(steamClientConnectionInitializer));
	}
}
