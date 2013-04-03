namespace Escape.Studies.Manager.FSharp

open Escape.Data

type IRepository<'Key, 'Value> =
    abstract Create   : AppointmentRequest -> unit
    abstract Pending  : 'Key -> unit
    abstract Accept   : 'Key -> unit
    abstract Reject   : 'Key -> unit
    abstract Conflict : 'Key -> unit
    abstract GetItem  : 'Key -> 'Value
    abstract Replace  : 'Key -> AppointmentRequest -> unit
    abstract Appointments : unit -> seq<'Value>