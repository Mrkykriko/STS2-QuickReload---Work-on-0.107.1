using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Platform.Steam;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using QuickReload.Multiplayer;
using Steamworks;

namespace QuickReload;

internal static class QuickReloadRunner
{
	public static Task RestartAsync()
	{
		return RestartAsync(null);
	}

	public static async Task RestartAsync(NPauseMenu? pauseMenu)
	{
		if (RunManager.Instance.IsSingleplayerOrFakeMultiplayer)
		{
			Log.Info("[QUICKRELOAD]: Single player run detected, restarting.");
			if (pauseMenu != null)
			{
				DisablePauseMenuButtons(pauseMenu);
			}
			if (await RestartSinglePlayer() && pauseMenu != null)
			{
				Log.Warn("[QUICKRELOAD]: RestartSinglePlayer returned early with ret=true, enabling pause menu buttons.");
				EnablePauseMenuButtons(pauseMenu);
			}
		}
		else
		{
			Log.Info("[QUICKRELOAD]: Multiplayer run detected, restarting.");
			if (pauseMenu != null)
			{
				DisablePauseMenuButtons(pauseMenu);
			}
			if (await RestartMultiPlayer() && pauseMenu != null)
			{
				Log.Warn("[QUICKRELOAD]: RestartMultiPlayer returned early with ret=true, enabling pause menu buttons.");
				EnablePauseMenuButtons(pauseMenu);
			}
		}
	}

	private static async Task<bool> RestartSinglePlayer()
	{
		await WaitForPendingSave();
		SerializableRun serializableRun;
		RunState runState;
		try
		{
			ReadSaveResult<SerializableRun> readSaveResult = SaveManager.Instance.LoadRunSave();
			serializableRun = readSaveResult.SaveData ?? throw new InvalidOperationException("[QUICKRELOAD]: Run save data was null.");
			runState = RunState.FromSerializable(serializableRun);
		}
		catch (Exception value)
		{
			Log.Error($"[QUICKRELOAD]: Save validation failed: {value}");
			return true;
		}
		NGame game = NGame.Instance ?? throw new InvalidOperationException("NGame.Instance was null during quick restart.");
		NRunMusicController.Instance?.StopMusic();
		await game.Transition.FadeOut(0.3f);
		RunManager.Instance.CleanUp();
		try
		{
			RunManager.Instance.SetUpSavedSingleplayer(runState, serializableRun);
			SfxCmd.Play(runState.Players[0].Character.CharacterTransitionSfx);
			game.ReactionContainer.InitializeNetworking(new NetSingleplayerGameService());
			await game.LoadRun(runState, serializableRun.PreFinishedRoom);
		}
		catch (Exception value2)
		{
			Log.Error($"[QUICKRELOAD]: Run load failed after cleanup: {value2}");
			await game.ReturnToMainMenu();
		}
		return false;
	}

	private static async Task<bool> RestartMultiPlayer()
	{
		NGame game = NGame.Instance ?? throw new InvalidOperationException("[QUICKRELOAD]: NGame.Instance was null during quick restart.");
		INetGameService netService = RunManager.Instance.NetService;
		if (netService != null && netService.IsConnected)
		{
			ulong num = (CommandLineHelper.HasArg("fastmp") ? 1003 : SteamUser.GetSteamID().m_SteamID);
			netService.SendMessage(new QuickReloadMessage
			{
				playerId = num
			});
			Log.Info($"[QUICKRELOAD]: Sent QuickReloadMessage before cleanup. playerId={num}");
		}
		else
		{
			Log.Warn("[QUICKRELOAD]: Net service not connected before cleanup, skipping QuickReloadMessage.");
		}
		await game.Transition.FadeOut(0.3f);
		RunManager.Instance.CleanUp();
		await WaitForPendingSave();
		NMainMenu nMainMenu = NMainMenu.Create(openTimeline: false);
		game.RootSceneContainer.SetCurrentScene(nMainMenu);
		NMultiplayerSubmenu nMultiplayerSubmenu = nMainMenu.OpenMultiplayerSubmenu();
		ulong localPlayerId = PlatformUtil.GetLocalPlayerId((SteamInitializer.Initialized && !CommandLineHelper.HasArg("fastmp")) ? PlatformType.Steam : PlatformType.None);
		ReadSaveResult<SerializableRun> readSaveResult = SaveManager.Instance.LoadAndCanonicalizeMultiplayerRunSave(localPlayerId);
		if (!readSaveResult.Success || readSaveResult.SaveData == null)
		{
			Log.Warn("[QUICKRELOAD]: Broken multiplayer run save detected, big problem");
			return true;
		}
		QuickReloadState.SetAutoReady(autoReady: true);
		nMultiplayerSubmenu.StartHost(readSaveResult.SaveData);
		return false;
	}

	private static async Task WaitForPendingSave()
	{
		Task currentRunSaveTask = SaveManager.Instance.CurrentRunSaveTask;
		if (currentRunSaveTask != null)
		{
			Log.Info("[QUICKRELOAD]: Saving in progress, waiting for it to be finished before quick restart.");
			try
			{
				await currentRunSaveTask;
			}
			catch (Exception value)
			{
				Log.Error($"[QUICKRELOAD]: Save task failed while waiting to quick restart: {value}");
			}
		}
	}

	private static void DisablePauseMenuButtons(NPauseMenu pauseMenu)
	{
		foreach (Node child in pauseMenu.GetNode<VBoxContainer>("PanelContainer/ButtonContainer").GetChildren())
		{
			if (child is NPauseMenuButton nPauseMenuButton)
			{
				nPauseMenuButton.Disable();
			}
		}
	}

	private static void EnablePauseMenuButtons(NPauseMenu pauseMenu)
	{
		foreach (Node child in pauseMenu.GetNode<VBoxContainer>("PanelContainer/ButtonContainer").GetChildren())
		{
			if (child is NPauseMenuButton nPauseMenuButton)
			{
				nPauseMenuButton.Enable();
			}
		}
	}
}
