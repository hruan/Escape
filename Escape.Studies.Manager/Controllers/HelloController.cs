using System.Web.Http;

namespace Escape.Studies.Manager.Controllers {
	public class HelloController : ApiController {
		public string Get() {
			return "Hello, world!";
		}
	}
}
