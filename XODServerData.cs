using System.Net;

namespace XodMasterServer;

public class ServerIp {
	public IPEndPoint lan;
	public IPEndPoint wlan;
	public IPEndPoint? vpn;
	public double lastUpdate;

	public ServerIp(IPEndPoint lan, IPEndPoint wlan, IPEndPoint? vpn, double lastUpdate) {
		this.lan = lan;
		this.wlan = wlan;
		this.vpn = vpn;
		this.lastUpdate = lastUpdate;
	}
}

public class ServerInfo {
	public string name;
	public byte playerCount;
	public byte maxPlayer;
	public string mode;
	public string map;
	public string fork;

	public ServerInfo(string name, byte playerCount, byte maxPlayer, string mode, string map, string fork) {
		this.name = name;
		this.maxPlayer = maxPlayer;
		this.playerCount = playerCount;
		this.mode = mode;
		this.map = map;
		this.fork = fork;
	}
}

public class SimpleServerData {
	public string? name;
	public string? level;
	public decimal? gameVersion;
	public string? gameChecksum;
	public string? customMapChecksum;
	public string? customMapUrl;

	public SimpleServerData(
		string name, string level, decimal gameVersion,
		string gameChecksum, string customMapChecksum, string customMapUrl
	) {
		this.name = name;
		this.level = level;
		this.gameVersion = gameVersion;
		this.gameChecksum = gameChecksum;
		this.customMapChecksum = customMapChecksum;
		this.customMapUrl = customMapUrl;
	}
}
