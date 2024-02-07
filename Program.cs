using System;
using System.IO;

namespace XodMasterServer;

class Program {
	public const int WindowWidth = 480;
	public const int WindowHeight = 270;

	static void Main(string[] args) {
		int port = 17788;
		if (File.Exists("./serverport.txt")) {
			string fileText = File.ReadAllText("./serverport.txt");
			fileText.Trim();
			port = Int32.Parse(fileText);
		}
		MasterServer masterServer = new MasterServer(port);
		masterServer.netServer.Start();
		Console.WriteLine("XOD MasterServer started on port " + port + ".");

		while (true) {
			masterServer.update();
		}
	}
}
