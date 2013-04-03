using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using Escape.Data;
using Escape.Studies.Manager.FSharp;

namespace Escape.Studies.Manager.Controllers {
	public class ResponseController : ApiController {
		private readonly IRepository<string, AppointmentMessage> _repository;
		public ResponseController(IRepository<string, AppointmentMessage> repository) {
			_repository = repository;
		}

		public HttpResponseMessage Post(AppointmentMessage msg) {
			if (msg == null
				|| msg.Appointment == null
				|| String.IsNullOrEmpty(msg.Appointment.Identifier)) {
				return Request.CreateResponse(HttpStatusCode.BadRequest, "Require appointment or appointment properties missing");
			}

			var cur = _repository.GetItem(msg.Appointment.Identifier);
			if (cur == null) {
				return Request.CreateResponse(HttpStatusCode.NotFound, "Response to unknown appointment");
			}

			if (msg.State == cur.State) return Request.CreateResponse(HttpStatusCode.NotModified);

			try {
				switch (msg.State) {
				case State.Accepted:
					_repository.Accept(msg.Appointment.Identifier);
					break;
				case State.Conflict:
					_repository.Conflict(msg.Appointment.Identifier);
					break;
				case State.Rejected:
					_repository.Reject(msg.Appointment.Identifier);
					break;
				default:
					return Request.CreateResponse(HttpStatusCode.BadRequest, "Message state is invalid");
				}
			} catch (ArgumentException) {
				return Request.CreateResponse(HttpStatusCode.InternalServerError);
			}

			return Request.CreateResponse(HttpStatusCode.OK);
		}
	}
}
