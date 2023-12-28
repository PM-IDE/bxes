open System.Globalization
open System.IO
open System.Threading
open Bxes.Research.Core


[<EntryPoint>]
let main args =
    let logsTopLevelDirectory = args[0]
    let outputDirectory = args[1]

    Thread.CurrentThread.CurrentCulture <- CultureInfo("en-US")

    let processLogsDirectory (directory: string) =
        let currentOutputDirectory =
            Path.Combine(outputDirectory, Path.GetFileName(directory))

        Directory.GetFiles directory
        |> Seq.filter (fun dir -> dir.EndsWith(".xes"))
        |> Seq.map (fun logPath ->
            let logOutputDirectory =
                Path.Combine(currentOutputDirectory, Path.GetFileNameWithoutExtension(logPath))

            Transformations.processEventLog logPath logOutputDirectory)
        |> Seq.toList


    match Directory.Exists(logsTopLevelDirectory) with
    | true ->
        let results =
            Directory.GetDirectories(logsTopLevelDirectory)
            |> Seq.map processLogsDirectory
            |> Seq.toList

        ()
    | false ->
        printfn "The top level logs directory does not exist"
        ()

    0
