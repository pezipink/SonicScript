namespace SonicScript

open System.Text
open System.IO
open System.Drawing
open Jacobi.Vst
open Jacobi.Vst.Core
open System.Windows.Forms
open Jacobi.Vst.Core.Plugin
open Jacobi.Vst.Framework
open Jacobi.Vst.Framework.Common
open Jacobi.Vst.Framework.Plugin
open Microsoft.FSharp.Compiler.Interactive.Shell
open System.Reflection

type PluginEditorView() as this =
    inherit UserControl()
    // Intialize output and input streams
    
    let components = Unchecked.defaultof<_> : System.ComponentModel.IContainer 
    let btnExecute = new Button()
    let splitContainer1 = new SplitContainer()
    
    let rtfOutput = new RichTextBox()
    let rtfInutput = new RichTextBox()
    do

        
        (splitContainer1 :> System.ComponentModel.ISupportInitialize).BeginInit()
        splitContainer1.Panel1.SuspendLayout()
        splitContainer1.Panel2.SuspendLayout()
        splitContainer1.SuspendLayout()
        this.SuspendLayout()
        // 
        // splitContainer1
        // 
        splitContainer1.Dock <- System.Windows.Forms.DockStyle.Fill
        splitContainer1.Location <- new System.Drawing.Point(0, 0)
        splitContainer1.Name <- "splitContainer1"
        splitContainer1.Orientation <- System.Windows.Forms.Orientation.Horizontal
        // 
        // splitContainer1.Panel1
        // 
        splitContainer1.Panel1.Controls.Add(rtfInutput)
        splitContainer1.Panel1.Controls.Add(btnExecute)
        // 
        // splitContainer1.Panel2
        // 
        splitContainer1.Panel2.Controls.Add(rtfOutput)
        splitContainer1.Size <- new System.Drawing.Size(551, 396)
        splitContainer1.SplitterDistance <- 250
        splitContainer1.TabIndex <- 0
        // 
        // btnExecute
        // 
        btnExecute.Dock <- System.Windows.Forms.DockStyle.Bottom
        btnExecute.Location <- new System.Drawing.Point(0, 227)
        btnExecute.Name <- "btnExecute"
        btnExecute.Size <- new System.Drawing.Size(551, 23)
        btnExecute.TabIndex <- 0
        btnExecute.Text <- "Send to Interactive"
        btnExecute.UseVisualStyleBackColor <- true
        // 
        // rtfInput
        // 
        rtfInutput.Dock <- System.Windows.Forms.DockStyle.Fill
        rtfInutput.Text <- "let processAudio (l:float32) (r:float32) = l, r"
        let font = new Font("Consolas", 8.0f)
        rtfInutput.Font <- font
        rtfInutput.BackColor <- Color.Black
        rtfInutput.ForeColor <- Color.SlateBlue
        // rtfOutput
        // 
        rtfOutput.Dock <- System.Windows.Forms.DockStyle.Fill
        rtfOutput.Location <- new System.Drawing.Point(0, 0)
        rtfOutput.Name <- "rtfOutput"
        rtfOutput.Size <- new System.Drawing.Size(551, 142)
        rtfOutput.TabIndex <- 0
        rtfOutput.Text <- ""
        rtfInutput.Font <- font
        rtfOutput.BackColor <- Color.Black
        rtfOutput.ForeColor <- Color.Lime
        // 
        // Form1
        // 
        this.AutoScaleDimensions <- new System.Drawing.SizeF(6.0F, 13.0F)
        this.AutoScaleMode <- System.Windows.Forms.AutoScaleMode.Font
        this.ClientSize <- new System.Drawing.Size(551, 396)
        this.Controls.Add(splitContainer1)
        this.Name <- "SonicScript - pinksquirrelabs.com"
        this.Text <- "SonicScript - pinksquirrelabs.com"
        splitContainer1.Panel1.ResumeLayout(false)
        splitContainer1.Panel2.ResumeLayout(false)
        (splitContainer1 :> System.ComponentModel.ISupportInitialize).EndInit()
        splitContainer1.ResumeLayout(false)
        this.ResumeLayout(false)
        rtfInutput.KeyDown
        |> Observable.subscribe(fun k -> 
            if k.Alt && k.KeyCode = Keys.Enter then
                btnExecute.PerformClick()
        ) |> ignore

    member __.Editor = rtfInutput

    member __.Output = rtfOutput

    member __.ProcessIdle() = ()

    member __.Button = btnExecute

type PluginEditor(plugin:SonicScriptPlugin,fsiSession:FsiEvaluationSession,output:StringBuilder) =
    let view = new WinFormsControlWrapper<PluginEditorView>()
    let mutable kmode = VstKnobMode.CircularMode
    interface IVstPluginEditor with
        member x.Bounds: Rectangle = 
            view.Bounds
        
        member x.Close(): unit = 
            view.Close()
        
        member x.KeyDown(ascii: byte, virtualKey: VstVirtualKey, modifers: VstModifierKeys): bool = 
            
            false
        
        member x.KeyUp(ascii: byte, virtualKey: VstVirtualKey, modifers: VstModifierKeys): bool = 
            false
        
        member x.KnobMode
            with get (): VstKnobMode = 
                kmode
            and set (v: VstKnobMode): unit = 
                kmode <- v
        
        member x.Open(hWnd: nativeint): unit = 
            view.Open hWnd
            view.Instance.Button.Click |> Observable.subscribe(fun _ -> 
                try
                    fsiSession.EvalInteraction(view.Instance.Editor.SelectedText)
                    view.Instance.Output.Text <- output.ToString()
                    view.Instance.Output.SelectionStart <- view.Instance.Output.Text.Length
                    view.Instance.Output.ScrollToCaret()
                    plugin.RequiresUpdate <- true
                with
                | _ -> ()) |> ignore
        
        member x.ProcessIdle(): unit = 
            ()
        

and PluginPrograms(plugin) =
    inherit VstPluginProgramsBase()
    
    let parameterCategories = new VstParameterCategoryCollection()
    let parameterInfos = new VstParameterInfoCollection()
    
    member __.ParameterInfos = parameterInfos 
    member __.ParameterCategories = parameterCategories

    member __.AddParamInfo(info) = parameterInfos.Add info
    member __.GetParameterCategory(categoryName:string) =
        if parameterCategories.Contains(categoryName) then parameterCategories.[categoryName] else
        let paramCategory = new VstParameterCategory()
        paramCategory.Name <- categoryName
        parameterCategories.Add(paramCategory)
        paramCategory
        
    override this.CreateProgramCollection() =
        let programs = new VstProgramCollection()

        // TODO: add a number of programs for your plugin.

        let program = this.CreateProgram(parameterInfos) : VstProgram
        program.Name <- "Default"
        programs.Add(program)

        programs

    // create a program with all parameters.
    member this.CreateProgram(parameterInfos:VstParameterInfoCollection) =
        let program = new VstProgram(parameterCategories)
        this.CreateParameters(program.Parameters, parameterInfos)
        program

    // create all parameters
    member this.CreateParameters(desitnation:VstParameterCollection , parameterInfos:VstParameterInfoCollection ) =
        for paramInfo in parameterInfos do
            desitnation.Add(this.CreateParameter(paramInfo))

    // create one parameter
    member this.CreateParameter(parameterInfo) =
        new VstParameter(parameterInfo)
    

and SonicScriptProcessor(plugin:SonicScriptPlugin,fsiSession:FsiEvaluationSession) =
    let mutable blocksize = 0
    let mutable sample = 0.0f
 
    let recreateFunc() = fsiSession.EvalExpression("processAudio").Value.ReflectionValue :?> float32 -> float32 -> (float32 * float32)

    let mutable processFunc = recreateFunc()

    interface IVstPluginAudioProcessor with
        member x.BlockSize
            with get (): int = 
                blocksize
            and set (v: int): unit = 
                blocksize <- v
        
        member x.InputCount: int = 2
        
        member x.OutputCount: int = 2
        
        member x.Process(inChannels: VstAudioBuffer [], outChannels: VstAudioBuffer []): unit = 
            if plugin.RequiresUpdate then
                processFunc <- recreateFunc()
                plugin.RequiresUpdate <- false

            let leftIn = inChannels.[0]
            let rightIn = inChannels.[1]

            let leftOut = outChannels.[0]
            let rightOut = outChannels.[1]
            
            for i in 0..leftIn.SampleCount-1 do
                let l,r = processFunc leftIn.[i] rightIn.[i]
                leftOut.[i] <- l
                rightOut.[i] <- r
        
        member x.SampleRate
            with get (): float32 = 
                sample
            and set (v: float32): unit = 
                sample <- v
        
        member x.SetPanLaw(``type``: VstPanLaw, gain: float32): bool = 
            false
        
        member x.TailSize: int = 0
        
        
and SonicScriptPlugin() =
    inherit VstPluginWithInterfaceManagerBase("SonicScript",VstProductInfo("SonicScript","Ross McKinlay - pinksquirrellabs.com",1), VstPluginCategory.Effect, VstPluginCapabilities.NoSoundInStop, 0, 42424242)
    let sbOut = new StringBuilder()
    let sbErr = new StringBuilder()
    let inStream = new StringReader("")
    let outStream = new StringWriter(sbOut)
    let errStream = new StringWriter(sbErr)
    
    // Build command line arguments & start FSI session
    let argv = [| @"C:\Fsi.exe" |]
    let allArgs = Array.append argv [|"--noninteractive"|]
   
    let fsiConfig =  FsiEvaluationSession.GetDefaultConfiguration()
    let fsiSession = FsiEvaluationSession.Create(fsiConfig, allArgs, inStream, outStream, errStream)   
    
    let mutable bypass = false

    do
        try
            fsiSession.EvalInteraction("let processAudio (l:float32) (r:float32) = l, r") 
        with _ -> ()
            

    member val RequiresUpdate = false with get, set

    override this.CreateAudioProcessor(instance) = 
        match instance with
        | null -> SonicScriptProcessor(this,fsiSession) :> _
        | instance -> base.CreateAudioProcessor(instance)

    override this.CreatePrograms(instance) =
        match instance with        
        | null -> PluginPrograms(this) :> _
        | instance -> base.CreatePrograms(instance)


    override this.CreateEditor instance =
        match instance with
        | null -> PluginEditor(this,fsiSession,sbOut) :> _
        | instance -> base.CreateEditor instance

    member this.PluginPrograms() = this.GetInstance<PluginPrograms>() : PluginPrograms
    

    interface IVstPluginBypass with
        member x.Bypass
            with get (): bool = 
                bypass
            and set (v: bool): unit = 
                bypass <- v
        
        

type SonicScriptPluginCommandStub() =
    inherit StdPluginCommandStub()
//
//    
    let handler = System.ResolveEventHandler(fun _ args ->
        let asmName = AssemblyName(args.Name)
        // assuming that we reference only dll files
        let expectedName = asmName.Name + ".dll"
        let expectedLocation =              
            System.IO.Path.Combine(@"F:\GIT\SonicScript\src\SonicScript\bin\Debug", expectedName)
        if System.IO.File.Exists expectedLocation then Assembly.LoadFrom expectedLocation else null
        )
    do System.AppDomain.CurrentDomain.add_AssemblyResolve handler
    override __.CreatePluginInstance() = 
    //    System.Diagnostics.Debugger.Launch()|> ignore

//   
        new SonicScriptPlugin() :> _

    