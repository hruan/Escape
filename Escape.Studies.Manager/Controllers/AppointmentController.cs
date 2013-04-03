using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Escape.Data;
using Escape.Studies.Manager.FSharp;

namespace Escape.Studies.Manager.Controllers {
	public class AppointmentController : ApiController {
		private readonly IRepository<string, AppointmentMessage> _repository;

		public AppointmentController(IRepository<string, AppointmentMessage> repo) {
			_repository = repo;
		}

		public HttpResponseMessage Post(AppointmentRequest appointment) {
			if (appointment == null) {
				return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid or missing data in body.");
			}

			AppointmentMessage msg;
			try {
				msg = _repository.GetItem(appointment.Identifier);
			} catch (ArgumentException) {
				msg = null;
			}

			if (msg != null) {
				if (msg.State == State.Conflict) {
					_repository.Replace(appointment.Identifier, appointment);
				}
			} else {
				_repository.Create(appointment);
				msg = _repository.GetItem(appointment.Identifier);
			}

			var resp = new HttpResponseMessage(HttpStatusCode.Created);
			var ub = new UriBuilder(Request.RequestUri);
			ub.Path = ub.Path.TrimEnd('/') + '/' + msg.Appointment.Identifier;

			resp.Headers.Add("Location", ub.Uri.ToString());
			return resp;
		}

		public HttpResponseMessage GetById(string id) {
			var app = _repository.GetItem(id);
			if (app == null) return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Invalid id");

			return Request.CreateResponse(HttpStatusCode.OK, app);
		}

#if DEBUG
		public IEnumerable<AppointmentMessage> GetAll() {
			return _repository.Appointments();
		}

		public HttpResponseMessage GetTestData(bool debug) {
			if (debug) {
				var appointment = new AppointmentRequest {
					Description  = "This is a description",
					Location     = "Location: Somewhere in the universe",
					Participants = new List<Participant> {
						new Participant { Name = "Pelle", Username = "MP11954" },
						new Participant { Name = "Kalle", Username = "MP10213" }
					},
					Priority   = 10,
					Sender     = Sender.Studies,
					StartTime  = DateTime.Now,
					User       = "MP11954",
					Identifier = Guid.NewGuid().ToString("N"),
					Type       = AppointmentType.SelfStudies
				};

				return Request.CreateResponse(HttpStatusCode.OK, appointment);
			}

			return Request.CreateResponse(HttpStatusCode.OK, GetAll());
		}
#endif
	}
}
