using System;
using System.Runtime.Serialization;

namespace Escape.Data {
	public interface IAppointmentMessage : IEquatable<IAppointmentMessage> {
		State              State       { get; set; }
		AppointmentRequest Appointment { get; }
	}

	[DataContract]
	public class AppointmentMessage : IAppointmentMessage {
		[DataMember] public State              State       { get; set; }
		[DataMember] public AppointmentRequest Appointment { get; private set; }

		public AppointmentMessage(AppointmentRequest request) {
			Appointment = request;
			State = State.Created;
		}

		public bool Equals(IAppointmentMessage other) {
			if (ReferenceEquals(other, null)) return false;
			if (ReferenceEquals(this, other)) return true;

			return Appointment != null && State == other.State && Appointment.Equals(other.Appointment);
		}

		public override int GetHashCode() {
			unchecked {
				return ((int) State * 397) ^ (Appointment != null ? Appointment.GetHashCode() : 0);
			}
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;

			var m = obj as AppointmentMessage;
			return m != null && Equals(m);
		}
	}

	[Flags]
	[DataContract]
	public enum State {
		[EnumMember] Created  = 1 << 0,
		[EnumMember] Pending  = 1 << 1,
		[EnumMember] Conflict = 1 << 2,
		[EnumMember] Accepted = 1 << 3,
		[EnumMember] Rejected = 1 << 4
	}
}
