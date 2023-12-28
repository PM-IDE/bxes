namespace Bxes.Research.Core

open System
open System.Diagnostics
open System.IO
open System.IO.Compression
open Bxes.Utils
open Bxes.Xes

module Transformations =
    type TransformationResult =
        { TransformationName: string
          OriginalFilePath: string
          OriginalFileSize: int64
          TransformedFileSize: int64 }

    let createOutputFilePath (xesFilePath: string) outputDirectory extension =
        let bxesLogFileName = Path.GetFileNameWithoutExtension(xesFilePath) + extension
        Path.Combine(outputDirectory, bxesLogFileName)

    let executeTransformation logPath outputDirectory extension transformation =
        let outputPath = createOutputFilePath logPath outputDirectory extension
        let outputDirectory = Path.GetDirectoryName outputPath

        match Directory.Exists outputDirectory with
        | true -> ()
        | false ->
            Directory.CreateDirectory outputDirectory |> ignore
            ()

        printfn $"Started executing transformation {extension} for file {logPath}"
        transformation (outputPath)

        { TransformationName = extension
          OriginalFilePath = logPath
          OriginalFileSize = FileInfo(logPath).Length
          TransformedFileSize = FileInfo(outputPath).Length }

    let bxesTransformation (logPath: string) outputDirectory =
        executeTransformation logPath outputDirectory ".bxes" (fun outputPath ->
            let logger = BxesDefaultLoggerFactory.Create()
            XesToBxesConverter().Convert(logPath, outputPath, logger))

    let zipTransformation logPath outputDirectory =
        executeTransformation logPath outputDirectory ".zip" (fun outputPath ->
            use fs = File.OpenWrite(outputPath)
            use archive = new ZipArchive(fs, ZipArchiveMode.Create)

            let fileName = Path.GetFileNameWithoutExtension(logPath)

            archive.CreateEntryFromFile(logPath, fileName, CompressionLevel.SmallestSize)
            |> ignore

            ())
        
    let bxesToXesTransformation (logPath: string) outputDirectory =
        let mutable bxesOutputPath = ""
        executeTransformation logPath outputDirectory ".bxes" (fun outputPath ->
            bxesOutputPath <- outputPath) |> ignore
        
        executeTransformation logPath outputDirectory ".xes" (fun outputPath ->
            let logger = BxesDefaultLoggerFactory.Create()
            BxesToXesConverter().Convert(bxesOutputPath, outputPath, logger))

    let private executeExternalTransformation command (arguments: string list) =
        let transformationProcess = Process.Start(command, arguments)
        transformationProcess.WaitForExit()
        
        match transformationProcess.ExitCode with
        | 0 -> ()
        | _ -> raise (Exception())
        
    let private executeExternalJavaTransformation jarPath logPath outputFilePath =
        executeExternalTransformation "java" [ "-jar"; jarPath; logPath; outputFilePath ]
        
    let exiTransformation (logPath: string) outputDirectory =
        executeTransformation logPath outputDirectory ".exi" (fun outputPath ->
            let exiTransformerJarPath = Environment.GetEnvironmentVariable("EXI_JAR_PATH")
            executeExternalJavaTransformation exiTransformerJarPath logPath outputPath)
    
    let transformations = [ bxesTransformation; zipTransformation; bxesToXesTransformation; exiTransformation; ]

    let processEventLog logPath outputDirectory =
        transformations
        |> List.map (fun transformation -> transformation logPath outputDirectory)
