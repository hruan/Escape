using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Escape.Data
{
	[DataContract]
	public enum Sender {
		[EnumMember] Studies,
		[EnumMember] Kronox
	}

	[DataContract]
	public enum AppointmentType {
		[EnumMember] Lecture,
		[EnumMember] SelfStudies,
		[EnumMember] Seminar,
		[EnumMember] Lab,
		[EnumMember] Exam
	}

	[KnownType(typeof(SelfStudies))]
	[DataContract]
	public class AppointmentRequest : IEquatable<AppointmentRequest> {
		[DataMember] public Sender            Sender          { get; set; }
		[DataMember] public AppointmentType   Type            { get; set; }
		[DataMember] public string            User            { get; set; }
		[DataMember] public int               Priority        { get; set; }
		[DataMember] public DateTime          StartTime       { get; set; }
		[DataMember] public DateTime          EndTime         { get; set; }
		[DataMember] public DateTime          LastUpdate      { get; set; }
		[DataMember] public int               NumberOfChanges { get; set; }
		[DataMember] public string            Description     { get; set; }
		[DataMember] public string            Location        { get; set; }
		[DataMember] public string            Identifier      { get; set; }
		[DataMember] public List<Participant> Participants    { get; set; }

		public bool Equals(AppointmentRequest other) {
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Sender == other.Sender && Type == other.Type && string.Equals(Identifier, other.Identifier);
		}

		public override int GetHashCode() {
			unchecked {
				int hashCode = (int) Sender;
				hashCode = (hashCode * 397) ^ (int) Type;
				hashCode = (hashCode * 397) ^ (Identifier != null ? Identifier.GetHashCode() : 0);
				return hashCode;
			}
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;

			var ar = obj as AppointmentRequest;
			return ar != null && Equals(ar);
		}
	}

	[DataContract]
	public class Participant {
		[DataMember] public string Name { get; set; }
		[DataMember] public string Username { get; set; }
	}
}
