namespace QuestPDF.FSharp.PreviewServer

open System

/// The render gate: idle, or rendering with a pending follow-up.
type internal Gate =
    | Idle
    | Rendering of pending: bool

[<RequireQualifiedAccess>]
module internal Schedule =
    /// The gate after a trigger, and whether a render starts.
    let trigger (gate: Gate) : Gate * bool =
        match gate with
        | Idle -> Rendering false, true
        | Rendering _ -> Rendering true, false

    /// The gate after a render finishes, and whether the follow-up render starts.
    let finish (gate: Gate) : Gate * bool =
        match gate with
        | Rendering true -> Rendering false, true
        | Rendering false
        | Idle -> Idle, false

    /// The interval between polls: the poll interval, or four times the last render time when longer.
    let nextPoll (poll: TimeSpan) (lastRender: TimeSpan) : TimeSpan =
        max poll (lastRender * 4.0)

    /// The delay to the next poll tick; None while polling is off or no page is open.
    let pollDelay (poll: TimeSpan option) (clients: int) (lastRender: TimeSpan) : TimeSpan option =
        match poll with
        | Some poll when clients > 0 -> Some (nextPoll poll lastRender)
        | _ -> None

    /// The hint shown for a document function that returns the same document on every call.
    let frozenHint =
        "The document function returns the same document on every call; define it as `let build () = document [...]`"

    /// The frozen-document hint when two consecutive renders get the same document object.
    let frozen (previous: obj) (current: obj) : string option =
        if
            not (isNull previous)
            && obj.ReferenceEquals (previous, current)
        then
            Some frozenHint
        else
            None
