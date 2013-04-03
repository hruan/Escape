using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

using Escape.Data;

namespace Escape.Studies.Manager.Controllers {
	public class BookController : ApiController {
		public HttpResponseMessage Post(AppointmentMessage msg) {
			return Request.CreateResponse(HttpStatusCode.OK);
		}
	}
}
