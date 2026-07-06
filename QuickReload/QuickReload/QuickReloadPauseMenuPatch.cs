using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;
using MegaCrit.Sts2.Core.Runs;

namespace QuickReload;

[HarmonyPatch(typeof(NPauseMenu), "_Ready")]
internal static class QuickReloadPauseMenuPatch
{
	private const string QuickReloadNodeName = "QuickReload_QuickReloadButton";

	private static readonly LocString RestartLoc = new LocString("gameplay_ui", "PAUSE_MENU.RESTART");

	private static void Postfix(NPauseMenu __instance)
	{
		if (RunManager.Instance.NetService.Type == NetGameType.Client)
		{
			Log.Info("[QUICKRELOAD]: Quick Restart: client run detected, skipping button addition.");
			return;
		}
		VBoxContainer nodeOrNull = __instance.GetNodeOrNull<VBoxContainer>("PanelContainer/ButtonContainer");
		if (nodeOrNull == null)
		{
			Log.Warn("[QUICKRELOAD]: Quick Restart: couldn't find button container.");
			return;
		}
		if (nodeOrNull.GetNodeOrNull<Node>("QuickReload_QuickReloadButton") != null)
		{
			Log.Warn("[QUICKRELOAD]: Quick Restart: button already exists, skipping.");
			return;
		}
		NPauseMenuButton node = nodeOrNull.GetNode<NPauseMenuButton>("SaveAndQuit");
		if (!(node.Duplicate(14) is NPauseMenuButton nPauseMenuButton))
		{
			Log.Warn("[QUICKRELOAD]: Quick Restart: failed to duplicate template button.");
			return;
		}
		nPauseMenuButton.Name = "QuickReload_QuickReloadButton";
		nPauseMenuButton.GetNode<MegaLabel>("Label").SetTextAutoSize(RestartLoc.GetFormattedText());
		MakeVisualsUnique(nPauseMenuButton);
		Node parent = node.GetParent();
		parent.AddChild(nPauseMenuButton, forceReadableName: false, Node.InternalMode.Disabled);
		parent.MoveChild(nPauseMenuButton, node.GetIndex());
		ConnectFocusNeighbors(nodeOrNull, nPauseMenuButton);
		nPauseMenuButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(delegate
		{
			OnQuickReloadPressed(__instance);
		}));
		Log.Info("[QUICKRELOAD]: Quick Restart button added.");
	}

	private static void ConnectFocusNeighbors(VBoxContainer buttonContainer, NPauseMenuButton restartButton)
	{
		List<NPauseMenuButton> list = new List<NPauseMenuButton>();
		foreach (Node child in buttonContainer.GetChildren())
		{
			if (child is NPauseMenuButton { Visible: not false } nPauseMenuButton)
			{
				list.Add(nPauseMenuButton);
			}
		}
		int num = list.IndexOf(restartButton);
		if (num <= 0 || num >= list.Count - 1)
		{
			Log.Warn("[QUICKRELOAD]: Quick Restart: unexpected button ordering, skipping focus neighbor update.");
			return;
		}
		NPauseMenuButton nPauseMenuButton2 = list[num - 1];
		NPauseMenuButton nPauseMenuButton3 = list[num + 1];
		nPauseMenuButton2.FocusNeighborBottom = restartButton.GetPath();
		restartButton.FocusNeighborTop = nPauseMenuButton2.GetPath();
		nPauseMenuButton3.FocusNeighborTop = restartButton.GetPath();
		restartButton.FocusNeighborBottom = nPauseMenuButton3.GetPath();
	}

	private static void MakeVisualsUnique(NPauseMenuButton button)
	{
		TextureRect nodeOrNull = button.GetNodeOrNull<TextureRect>("ButtonImage");
		if (nodeOrNull?.Material != null)
		{
			nodeOrNull.Material = nodeOrNull.Material.Duplicate() as Material;
		}
		CanvasItem nodeOrNull2 = button.GetNodeOrNull<CanvasItem>("Label");
		if (nodeOrNull2?.Material != null)
		{
			nodeOrNull2.Material = nodeOrNull2.Material.Duplicate() as Material;
		}
	}

	private static void OnQuickReloadPressed(NPauseMenu pauseMenu)
	{
		Log.Info("[QUICKRELOAD]: Quick Restart pressed.");
		TaskHelper.RunSafely(QuickReloadRunner.RestartAsync(pauseMenu));
	}
}
