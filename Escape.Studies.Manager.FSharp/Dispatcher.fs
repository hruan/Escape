namespace Escape.Studies.Manager.FSharp

open System
open System.Net.Http
open Escape.Data

module Dispatch =
    type RequestMessage = AppointmentMessage * AsyncReplyChannel<bool>

    type Dispatcher (url : string) as self =
        let mailbox = MailboxProcessor<RequestMessage>.Start(fun inbox ->
            let rec recvLoop () = async {
                printfn "Dispatch [%s]: waiting ..." self.URL
                let! msg, chn = inbox.Receive ()
                use c = new HttpClient()
                let! resp = Async.AwaitTask(c.PostAsXmlAsync(self.URL, msg))
                chn.Reply resp.IsSuccessStatusCode
                
                return! recvLoop ()
            }
            recvLoop ())

        let post item = async {
            try
                let! ok = mailbox.PostAndAsyncReply (fun chn -> (item, chn))
                if not ok then
                    eprintfn "Dispatch [%s]: Send failed" self.URL
                    do! Async.Sleep 1000
                    do! self.Queue item
                else
                    if self.Callback <> null then self.Callback.Invoke(item.Appointment.Identifier)
            with :? HttpRequestException as exn ->
                eprintfn "Dispatch [%s]: Send failed" self.URL
                do! self.Queue item
        }

        member val URL : string = url with get
        member val Callback : Action<string> = null with get, set
        member __.Queue item = async {
            do! post item
        }