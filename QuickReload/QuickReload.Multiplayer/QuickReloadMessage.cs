using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace QuickReload.Multiplayer;

public struct QuickReloadMessage : INetMessage, IPacketSerializable
{
	public ulong playerId;

	public bool ShouldBroadcast => true;

	public NetTransferMode Mode => (NetTransferMode)2;

	public bool ShouldBuffer => true;

	public LogLevel LogLevel => (LogLevel)0;

	public void Serialize(PacketWriter writer)
	{
		writer.WriteULong(playerId, 64);
	}

	public void Deserialize(PacketReader reader)
	{
		playerId = reader.ReadULong(64);
	}

	public override string ToString()
	{
		return $"[QUICKRELOAD]: QuickReloadMessage: playerId={playerId}";
	}
}
