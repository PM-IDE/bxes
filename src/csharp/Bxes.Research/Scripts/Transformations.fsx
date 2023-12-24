open System.IO
open Bxes.Research.Core

#load "../Core/Transformations.fs"

let logsTopLevelDirectory = fsi.CommandLineArgs[1]
let outputDirectory = fsi.CommandLineArgs[2]


let processLogsDirectory (directory: string) =
    let currentOutputDirectory = Path.Combine(outputDirectory, Path.GetDirectoryName(directory))
    
    Directory.GetFiles directory
    |> Seq.filter (fun dir -> dir.EndsWith(".xes"))
    |> Seq.map (fun logPath ->
        let logOutputDirectory = Path.Combine(Path.GetFileNameWithoutExtension(logPath), logPath)
        Transformations.processEventLog logPath)


match Directory.Exists(logsTopLevelDirectory) with
    | true ->
        Directory.GetDirectories(logsTopLevelDirectory)
        |> Seq.map processLogsDirectory
        |> ignore

        ()
    | false ->
        printfn "The top level logs directory does not exist"
        ()
        