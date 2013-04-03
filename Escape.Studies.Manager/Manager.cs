using System;
using System.Diagnostics;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Threading;
using System.Web.Http;
using System.Web.Http.SelfHost;
using Autofac;
using Autofac.Integration.WebApi;
using Escape.Data;
using Escape.Studies.Manager.FSharp;

namespace Escape.Studies.Manager {
	public class Manager : IDisposable {
		private readonly HttpSelfHostServer _server;
		private readonly CancellationTokenSource _cts;

		internal HttpConfiguration Config { get; private set; }

		public Manager() {
			var cfg = new HttpSelfHostConfiguration("http://localhost:8888");
			cfg.Routes.MapHttpRoute(
				name: "default",
				routeTemplate: "{controller}/{id}",
				defaults: new {
					controller = "Hello",
					id         = RouteParameter.Optional
				});

			cfg.Formatters.Clear();
			cfg.Formatters.Add(new XmlMediaTypeFormatter { Indent = true });
			cfg.Formatters.Add(new JsonMediaTypeFormatter());

			Debug.WriteLine("Will do content negotiation for:");
			foreach (var f in cfg.Formatters) {
				foreach (var v in f.SupportedMediaTypes) {
					Debug.WriteLine(v.MediaType);
				}
			}

			_server = new HttpSelfHostServer(cfg);
			Config = cfg;

			_cts = new CancellationTokenSource();
			_cts.Token.Register(() => Console.WriteLine("Dispatchers canceled."));
		}
		
		public void Start(string requestUrl, string responseUrl) {
			var outgoing = new Dispatch.Dispatcher(requestUrl);
			var incoming = new Dispatch.Dispatcher(responseUrl);
			IRepository<string, AppointmentMessage> manager = new Repository(outgoing, incoming);

			outgoing.Callback = manager.Pending;
			incoming.Callback = null;

			Console.WriteLine("Manager: New requests will be dispatched to {0}", requestUrl);
			Console.WriteLine("Manager: Responses will be dispatched to {0}", responseUrl);

			// Set up IoC
			var builder = new ContainerBuilder();
			builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
			builder.Register(ctx => manager)
				.As<IRepository<string, AppointmentMessage>>()
				.SingleInstance();

			Config.DependencyResolver = new AutofacWebApiDependencyResolver(builder.Build());

			_server.OpenAsync().Wait();
		}

		public void Dispose() {
			if (_cts != null) _cts.Cancel();
			if (_server != null) _server.Dispose();
		}
	}
}
