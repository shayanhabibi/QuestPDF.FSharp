namespace FSharp.QuestPDF.PreviewServer

open System
open System.IO

/// The debounce of saves: when the next reload is due, whether one is in flight, and whether a save arrived during
/// it.
type internal Debounce =
    {
        /// The time the next reload starts.
        Due: DateTime option
        /// A reload runs.
        InFlight: bool
        /// A save arrived while a reload ran; one follow-up reload is due when it finishes.
        Dirty: bool
    }

/// The file-system events that count as a save of F# source, and the debounce of saves.
[<RequireQualifiedAccess>]
module internal ChangeFilter =
    /// The build and repository folders under a watched root, whose files are ignored.
    let private ignoredFolders = set [ "bin"; "obj"; ".git" ]

    /// Whether a file name is an editor's temporary or backup file.
    let private temporary (name: string) =
        name.EndsWith "~"
        || name.EndsWith "___jb_tmp___"
        || name.EndsWith "___jb_old___"
        || name.StartsWith ".#"
        || name = "4913"

    /// Whether a file-system event under a watched root counts as a save of F# source: a change, creation or rename
    /// onto an .fs or .fsx file, in any letter case, outside bin, obj and .git folders, and not an editor's temporary file.
    let classify (root: string) (change: WatcherChangeTypes) (path: string) : bool =
        let counted =
            change = WatcherChangeTypes.Changed
            || change = WatcherChangeTypes.Created
            || change = WatcherChangeTypes.Renamed

        let name = Path.GetFileName path
        let extension = Path.GetExtension name

        let folders =
            Path
                .GetRelativePath(root, Path.GetDirectoryName path)
                .Split ([| Path.DirectorySeparatorChar; Path.AltDirectorySeparatorChar |], StringSplitOptions.RemoveEmptyEntries)

        counted
        && (String.Equals (extension, ".fs", StringComparison.OrdinalIgnoreCase)
            || String.Equals (extension, ".fsx", StringComparison.OrdinalIgnoreCase))
        && not (temporary name)
        && not (folders |> Array.exists ignoredFolders.Contains)

    /// The state between reloads.
    let idle: Debounce =
        { Due = None
          InFlight = false
          Dirty = false }

    /// The state after a counted event: a reload due a window after the event, or a follow-up marked during a reload.
    let event (window: TimeSpan) (now: DateTime) (state: Debounce) : Debounce =
        if state.InFlight then
            { state with Dirty = true }
        else
            { state with Due = Some (now + window) }

    /// The state at a time, and whether a reload starts.
    let tick (now: DateTime) (state: Debounce) : Debounce * bool =
        match state.Due with
        | Some due when due <= now && not state.InFlight ->
            { state with
                Due = None
                InFlight = true },
            true
        | _ -> state, false

    /// The state after a reload finishes: a follow-up due a window later when a save arrived during the reload.
    let finished (window: TimeSpan) (now: DateTime) (state: Debounce) : Debounce =
        { Due = (if state.Dirty then Some (now + window) else state.Due)
          InFlight = false
          Dirty = false }

    /// The wait until the next reload is due, zero when overdue; None when none is due.
    let wait (now: DateTime) (state: Debounce) : TimeSpan option =
        state.Due
        |> Option.map (fun due -> max TimeSpan.Zero (due - now))
