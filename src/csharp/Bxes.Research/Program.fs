open System.IO
open Bxes.Research.Core


[<EntryPoint>]
let main args =
    let logsTopLevelDirectory = args[1]
    let outputDirectory = args[2]

    let processLogsDirectory (directory: string) =
        let currentOutputDirectory = Path.Combine(outputDirectory, Path.GetDirectoryName(directory))
        
        Directory.GetFiles directory
        |> Seq.filter (fun dir -> dir.EndsWith(".xes"))
        |> Seq.map (fun logPath ->
            let logOutputDirectory = Path.Combine(currentOutputDirectory, Path.GetFileNameWithoutExtension(logPath))
            Transformations.processEventLog logPath logOutputDirectory)


    match Directory.Exists(logsTopLevelDirectory) with
        | true ->
            Directory.GetDirectories(logsTopLevelDirectory)
            |> Seq.map processLogsDirectory
            |> ignore

            ()
        | false ->
            printfn "The top level logs directory does not exist"
            ()

    0