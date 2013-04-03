namespace Escape.Studies.Manager.FSharp

open System
open Microsoft.FSharp.Collections
open Escape.Data

type ResponseMessage = AppointmentMessage * AsyncReplyChannel<bool>

type Repository (outgoing : Dispatch.Dispatcher, incoming : Dispatch.Dispatcher) = 
    let mutable repo = Map.empty
    let outgoing = outgoing
    let incoming = incoming

    let post (dispatcher : Dispatch.Dispatcher) msg = async {
        printfn "Repository: Dispatching %A to %A" msg dispatcher
        do! dispatcher.Queue msg
    }

    let mailbox = MailboxProcessor<AppointmentMessage>.Start(fun inbox ->
        let rec recvLoop () = async {
            printfn "Repository: waiting ..."
            let! msg = inbox.Receive ()
            match msg.State with
                | State.Created -> do! post outgoing msg
                | State.Conflict | State.Accepted | State.Rejected -> do! post incoming msg
                | _ as m -> eprintfn "Repository: Unknown message discarded: %A" m
            return! recvLoop () 
        }
        recvLoop ())

    let find key =
        match repo.TryFind key with
        | Some v -> v
        | None -> raise <| new ArgumentException "No such key"

    let state (s : State) (req : AppointmentMessage) =
        req.State <- s
        req

    let setStateAndQueue (s : State) = state s >> mailbox.Post

    interface IRepository<string, AppointmentMessage> with
        member __.Create req =
            let m = new AppointmentMessage(req)
            repo <- repo.Add (req.Identifier, m)
            mailbox.Post m

        member __.Pending id =
            find id |> state State.Pending |> ignore

        member __.Conflict id =
            find id |> setStateAndQueue State.Conflict

        member __.Accept id =
            find id |> setStateAndQueue State.Accepted

        member __.Reject id =
            find id |> setStateAndQueue State.Rejected

        member __.GetItem id =
            find id

        member __.Replace id req =
            repo <- repo.Remove id
            let m = new AppointmentMessage(req)
            repo <- repo.Add (req.Identifier, m)

        member __.Appointments () =
            seq { for t in repo |> Map.toSeq -> snd t }