using System;
using System.Net;
using System.Collections.Generic;
using Lidgren.Network;
using Newtonsoft.Json;
using System.Threading;

namespace XodMasterServer;

class MasterServer {
	public NetServer netServer;
	public Dictionary<long, ServerIp> serverList = new();
	public Dictionary<long, ServerInfo> serverInfo = new();
	public Dictionary<long, SimpleServerData> serverDataList = new();
	public double lastUpdate = -60;
	public double lastCheck = -60;

	public MasterServer(int port) {
		var config = new NetPeerConfiguration("XOD-P2P");
		config.Port = port;
		config.SetMessageTypeEnabled(NetIncomingMessageType.NatIntroductionSuccess, true);
		config.SetMessageTypeEnabled(NetIncomingMessageType.UnconnectedData, true);
		netServer = new NetServer(config);
	}

	public void update() {
		NetIncomingMessage? msg;
		while ((msg = netServer.ReadMessage()) != null) {
			if (msg.MessageType == NetIncomingMessageType.UnconnectedData) {
				switch (msg.ReadByte()) {
					case (int)MasterServerMsg.HostList:
						sendHostList(msg);
						break;
					case (int)MasterServerMsg.ConnectPeers:
						connectPeersShort(msg);
						break;
					case (int)MasterServerMsg.ConnectPeersLong:
						connectPeersOld(msg);
						break;
					case (int)MasterServerMsg.RequestDetails:
						sendServerDetails(msg);
						break;
					case (int)MasterServerMsg.RegisterHost:
						registerHost(msg);
						break;
					case (int)MasterServerMsg.RegisterDetails:
						registerHostDetails(msg);
						break;
					case (int)MasterServerMsg.RegisterInfo:
						registerInfo(msg);
						break;
					case (int)MasterServerMsg.UpdatePlayerNum:
						updatePlayerNumber(msg);
						break;
					case (int)MasterServerMsg.DeleteHost:
						deleteHost(msg);
						break;
					default:
						Console.WriteLine("Error Type2!");
						Console.WriteLine(msg.ToString());
						break;
				}
			} else {
				Console.WriteLine("Error!");
				Console.WriteLine(msg.ToString());
			}
		}
		// We do these checks each second.
		if (NetTime.Now < lastCheck + 1) {
			Thread.Sleep(16);
			return;
		}
		//Console.WriteLine("Staring server flush.");
		// Remove old servers.
		List<long> toDelete = new();
		foreach (var kvp in serverList) {
			if (NetTime.Now <= kvp.Value.lastUpdate + 5) {
				continue;
			}
			toDelete.Add(kvp.Key);
		}
		foreach (long key in toDelete) {
			Console.WriteLine("Deleted server id: " + key);
			serverList.Remove(key);
		}
		lastCheck = Math.Ceiling(NetTime.Now);
		Thread.Sleep(16);
	}

	public void registerHost(NetIncomingMessage msg) {
		long serverId = msg.ReadInt64();
		Console.WriteLine("Got registration for host " + serverId);
		if (msg.SenderEndPoint == null) {
			Console.WriteLine("Error: Null endpoint.");
			return;
		}
		serverList[serverId] = new ServerIp(
			msg.ReadIPEndPoint(),
			msg.SenderEndPoint,
			NetTime.Now
		);
	}

	// Send server details to connect.
	public void registerHostDetails(NetIncomingMessage msg) {
		long serverId = msg.ReadInt64();
		string jsonData = msg.ReadString();
		SimpleServerData? tempData = JsonConvert.DeserializeObject<SimpleServerData>(jsonData);
		if (tempData != null) {
			serverDataList[serverId] = tempData;
		}
	}

	public void connectPeersOld(NetIncomingMessage msg) {
		long serverId = msg.ReadInt64();
		IPEndPoint clientInternalIP = msg.ReadIPEndPoint();
		Console.WriteLine("Using UDP punch through for " + serverId);
		if (!serverDataList.ContainsKey(serverId)) {
			Console.WriteLine("UDP Conction: Server requested does not exist");
			return;
		}
		Console.WriteLine("Info:");
		Console.WriteLine("  SV.intr: " + serverList[serverId].intr);
		Console.WriteLine("  SV.extr: " + serverList[serverId].extr);
		Console.WriteLine("  CL.intr: " + clientInternalIP);
		Console.WriteLine("  CL.extr: " + msg.SenderEndPoint);

		if (msg.SenderEndPoint == null) {
			Console.WriteLine("ERROR: Endpoint is null");
			return;
		}

		netServer.Introduce(
			serverList[serverId].intr,
			serverList[serverId].extr,
			clientInternalIP,
			msg.SenderEndPoint,
			serverList[serverId].extr.Address.ToString() + ":" + serverList[serverId].extr.Port.ToString()
		);
	}
	
	public void connectPeersShort(NetIncomingMessage msg) {
		long serverId = msg.ReadInt64();
		IPEndPoint clientInternalIP = msg.ReadIPEndPoint();
		Console.WriteLine("Using UDP punch through for " + serverId);
		if (!serverDataList.ContainsKey(serverId)) {
			Console.WriteLine("UDP Conction: Server requested does not exist");
			return;
		}
		Console.WriteLine("Info:");
		Console.WriteLine("  SV.intr: " + serverList[serverId].intr);
		Console.WriteLine("  SV.extr: " + serverList[serverId].extr);
		Console.WriteLine("  CL.intr: " + clientInternalIP);
		Console.WriteLine("  CL.extr: " + msg.SenderEndPoint);

		if (msg.SenderEndPoint == null) {
			Console.WriteLine("ERROR: Endpoint is null");
			return;
		}

		netServer.Introduce(
			serverList[serverId].intr,
			serverList[serverId].extr,
			clientInternalIP,
			msg.SenderEndPoint,
			"p"
		);
	}

	public void sendServerDetails(NetIncomingMessage msg) {
		if (msg.SenderEndPoint == null) { return; }
		// It's a client wanting a list of registered hosts.
		//Console.WriteLine("Sending server info.");
		long serverId = msg.ReadInt64();
		// Send registered host to client.
		if (!serverDataList.ContainsKey(serverId)) {
			return;
		}
		NetOutgoingMessage outMsg = netServer.CreateMessage();
		outMsg.Write((byte)101);
		string jsonString = JsonConvert.SerializeObject(serverDataList[serverId]);
		outMsg.Write(serverId);
		outMsg.Write(jsonString);
		outMsg.Write(serverList[serverId].extr);
		netServer.SendUnconnectedMessage(outMsg, msg.SenderEndPoint);
	}

	public void sendHostList(NetIncomingMessage msg) {
		if (msg.SenderEndPoint == null) {
			return;
		}
		// It's a client wanting a list of registered hosts
		NetOutgoingMessage outMsg = netServer.CreateMessage();
		outMsg.Write((byte)100);
		foreach (var kvp in serverList) {
			var info = serverInfo[kvp.Key];
			// Send registered host to client
			outMsg.Write((byte)1);
			outMsg.Write(kvp.Key);
			outMsg.Write(kvp.Value.intr);
			outMsg.Write(kvp.Value.extr);
			outMsg.Write(info.name);
			outMsg.Write(info.maxPlayer);
			outMsg.Write(info.playerCount);
			outMsg.Write(info.mode);
			outMsg.Write(info.map);
			outMsg.Write(info.fork);
		}
		outMsg.Write((byte)0);
		netServer.SendUnconnectedMessage(outMsg, msg.SenderEndPoint);
	}

	public void registerInfo(NetIncomingMessage msg) {
		long serverId = msg.ReadInt64();
		Console.WriteLine("Got registration for host info " + serverId);

		if (serverList.ContainsKey(serverId)) {
			serverList[serverId].lastUpdate = NetTime.Now;
		}
		string name = msg.ReadString();
		byte maxPlayer = msg.ReadByte();
		byte playerCount = msg.ReadByte();
		string mode = msg.ReadString();
		string map = msg.ReadString();
		string fork = msg.ReadString();

		serverInfo[serverId] = new ServerInfo(
			name,
			maxPlayer,
			playerCount,
			mode,
			map,
			fork
		);
	}

	public void updatePlayerNumber(NetIncomingMessage msg) {
		long serverId = msg.ReadInt64();
		//Console.Write("Updated server info");

		if (serverList.ContainsKey(serverId)) {
			//Console.Write(" and time");
			serverList[serverId].lastUpdate = NetTime.Now;
		}
		if (serverInfo.ContainsKey(serverId)) {
			//Console.Write(" and players");
			serverInfo[serverId].playerCount = msg.ReadByte();
		}
		//Console.WriteLine(".");
	}

	public void deleteHost(NetIncomingMessage msg) {
		long serverId = msg.ReadInt64();
		Console.WriteLine("Host with ID \"" + serverId + "\" was closed.");
		serverList.Remove(serverId);
	}
}

public enum MasterServerMsg {
	HostList,
	ConnectPeersLong,
	RequestDetails,
	RegisterHost,
	RegisterDetails,
	RegisterInfo,
	UpdatePlayerNum,
	DeleteHost,
	ConnectPeers,
}
