using System;
using System.Configuration;

using Escape.Studies.Manager;

namespace Escape {
	public static class Program {
		static void Main(string[] args) {
			var reqUrl = ConfigurationManager.AppSettings["RequestDispatchURL"];
			var respUrl = ConfigurationManager.AppSettings["ResponseDispatchURL"];
			var manager = new Manager();

			manager.Start(reqUrl, respUrl);
			Console.WriteLine("\nRunning, hit Enter to exit ...");

			Console.ReadLine();
			manager.Dispose();
		}
	}
}
