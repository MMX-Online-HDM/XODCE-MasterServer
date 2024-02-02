using System;

namespace XodMasterServer;

class Program {
	public const int WindowWidth = 480;
	public const int WindowHeight = 270;

	static void Main(string[] args) {
		MasterServer masterServer = new MasterServer();
		masterServer.netServer.Start();
		Console.WriteLine("Server Started!");

		while (true) {
			masterServer.update();
		}
	}
}
