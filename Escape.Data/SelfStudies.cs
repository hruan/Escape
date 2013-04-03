using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Escape.Data
{
	[DataContract]
	public class SelfStudies : AppointmentRequest
	{
		[DataMember] public AppointmentRequest Parent { get; set; }
	}
}
