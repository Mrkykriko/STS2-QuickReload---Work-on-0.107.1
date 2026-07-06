using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace QuickReload;

[HarmonyPatch]
internal static class AutoReadyPatch
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return AccessTools.Method(typeof(NMultiplayerLoadGameScreen), "OnSubmenuOpened");
		yield return AccessTools.Method(typeof(NDailyRunLoadScreen), "OnSubmenuOpened");
		yield return AccessTools.Method(typeof(NCustomRunLoadScreen), "OnSubmenuOpened");
	}

	private static void Postfix(object __instance)
	{
		if (!QuickReloadState.TryConsumePendingAutoReady())
		{
			Log.Info("[QUICKRELOAD]: AutoReady postfix called, but no pending restart or autoReady is false.");
			return;
		}
		QuickReloadState.ResetRunStartupNetGuard();
		if (!(__instance is Node node))
		{
			Log.Warn("[QUICKRELOAD]: AutoReady postfix called, but instance is not a Godot.Node.");
			return;
		}
		NButton nodeOrNull = node.GetNodeOrNull<NButton>("ConfirmButton");
		if (nodeOrNull == null)
		{
			Log.Warn("[QUICKRELOAD]: AutoReady postfix called, but ConfirmButton not found.");
		}
		else
		{
			TaskHelper.RunSafely(WaitForLobbyAndAutoReady(node, nodeOrNull, __instance));
		}
	}

	private static async Task WaitForLobbyAndAutoReady(Node node, NButton confirm, object screen)
	{
		SceneTree tree = node.GetTree();
		if (tree == null)
		{
			Log.Warn("[QUICKRELOAD]: AutoReady aborted because SceneTree is null.");
			return;
		}
		LoadRunLobby runLobby = GetRunLobby(screen);
		for (int _ = 0; _ < 600; _++)
		{
			if (!GodotObject.IsInstanceValid(node) || !GodotObject.IsInstanceValid(confirm))
			{
				Log.Warn("[QUICKRELOAD]: AutoReady aborted because screen/button became invalid.");
				return;
			}
			if (AreAllLobbyPlayersConnected(runLobby))
			{
				QuickReloadState.MarkPendingRunStartupNetGuard();
				confirm.EmitSignal(NClickableControl.SignalName.Released, confirm);
				NModalContainer.Instance?.Clear();
				QuickReloadState.Clear();
				Log.Info("[QUICKRELOAD]: AutoReady confirm fired after lobby connectivity check passed.");
				return;
			}
			await node.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
		}
		Log.Warn("[QUICKRELOAD]: AutoReady timed out waiting for all lobby players to connect; skipping auto-ready.");
		QuickReloadState.Clear();
	}

	private static bool AreAllLobbyPlayersConnected(LoadRunLobby? runLobby)
	{
		if (runLobby == null)
		{
			return true;
		}
		return runLobby.Run.Players.All((SerializablePlayer player) => runLobby.ConnectedPlayerIds.Contains(player.NetId));
	}

	private static LoadRunLobby? GetRunLobby(object screen)
	{
		return Traverse.Create(screen).Field<LoadRunLobby>("_runLobby").Value;
	}
}
