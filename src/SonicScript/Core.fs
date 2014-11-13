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
    let sbOut = new StringBuilder()
    let sbErr = new StringBuilder()
    let inStream = new StringReader("")
    let outStream = new StringWriter(sbOut)
    let errStream = new StringWriter(sbErr)
    
    // Build command line arguments & start FSI session
    let argv = [| @"C:\Program Files (x86)\Microsoft SDKs\F#\3.1\Framework\v4.0\Fsi.exe" |]
    let allArgs = Array.append argv [|"--gui-"|]
   
//    let fsiConfig =  lazy FsiEvaluationSession.GetDefaultConfiguration()
//    let fsiSession = lazy FsiEvaluationSession.Create(fsiConfig.Value, allArgs, inStream, outStream, errStream)   
//   
    let components = Unchecked.defaultof<_> : System.ComponentModel.IContainer 
    let btnExecute = new Button()
    let splitContainer1 = new SplitContainer()
    let rtfInput = new RichTextBox()    
    let rtfOutput = new RichTextBox()
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
        splitContainer1.Panel1.Controls.Add(rtfInput)
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
        btnExecute.Text <- "button1"
        btnExecute.UseVisualStyleBackColor <- true
        // 
        // rtfInput
        // 
        rtfInput.Dock <- System.Windows.Forms.DockStyle.Fill
        rtfInput.Location <- new System.Drawing.Point(0, 0)
        rtfInput.Name <- "rtfInput"
        rtfInput.Size <- new System.Drawing.Size(551, 227)
        rtfInput.TabIndex <- 1
        rtfInput.Text <- ""
        // 
        // rtfOutput
        // 
        rtfOutput.Dock <- System.Windows.Forms.DockStyle.Fill
        rtfOutput.Location <- new System.Drawing.Point(0, 0)
        rtfOutput.Name <- "rtfOutput"
        rtfOutput.Size <- new System.Drawing.Size(551, 142)
        rtfOutput.TabIndex <- 0
        rtfOutput.Text <- ""
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

        btnExecute.Click 
        |> Observable.subscribe(fun _ -> 
            
            let c =   FsiEvaluationSession.GetDefaultConfiguration()
            
            let s =  FsiEvaluationSession.Create(c, allArgs, inStream, outStream, errStream)   
            //fsiSession.Value.EvalInteraction rtfInput.Text 
            ()
            ) |> ignore
        ()




type PluginEditor(plugin) =
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
        
        member x.ProcessIdle(): unit = 
            ()
        

type PluginPrograms(plugin) =
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
    

type Distortion(plugin:SonicScriptPlugin) =
    let ha = lazy plugin.Host.GetInstance<IVstHostAutomation>()

    let tapMgr =
        let paramInfo = VstParameterInfo()
        paramInfo.Name <- "Tap"
        paramInfo.Label <- "Tap"
        paramInfo.ShortLabel <- "|"
        paramInfo.MinInteger <- 0
        paramInfo.MaxInteger <- 2
        paramInfo.LargeStepFloat <- 1.0f
        paramInfo.SmallStepFloat <- 1.0f
        paramInfo.StepFloat <- 1.0f
        paramInfo.CanRamp <- true
        paramInfo.DefaultValue <- 0.0f
        let prog = plugin.PluginPrograms() : PluginPrograms
        prog.AddParamInfo(paramInfo)
        let m = VstParameterManager paramInfo
        VstParameterNormalizationInfo.AttachTo paramInfo   
        
             
        
        m

    do 
        plugin.Opened 
        |> Observable.subscribe(fun _ ->
            tapMgr.HostAutomation <- ha.Force()
          ) |> ignore

    member __.Process(left:float32, right:float32) =
        left * tapMgr.CurrentValue, right * tapMgr.CurrentValue


and SonicScriptProcessor(plugin:SonicScriptPlugin) =
    let mutable blocksize = 0
    
    let mutable sample = 0.0f
    let mutable center_freq = 0
    let mutable counter = 0
    let mutable counter_limit = 0
    let mutable control = 0 // LFO
    let mutable max_freq = 0
    let mutable min_freq = 0
    let mutable pha_mix = 0
    let mutable f_step = 0
    let mutable dir_mix = 0
    let ph_stages = 0

    let distortion = Distortion(plugin)

    interface IVstPluginAudioProcessor with
        member x.BlockSize
            with get (): int = 
                blocksize
            and set (v: int): unit = 
                blocksize <- v
        
        member x.InputCount: int = 2
        
        member x.OutputCount: int = 2
        
        member x.Process(inChannels: VstAudioBuffer [], outChannels: VstAudioBuffer []): unit = 
            let leftIn = inChannels.[0]
            let rightIn = inChannels.[1]

            let leftOut = outChannels.[0]
            let rightOut = outChannels.[1]

            for i in 0..leftIn.SampleCount-1 do
                let l,r = distortion.Process(leftIn.[i],rightIn.[i])
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
    inherit VstPluginWithInterfaceManagerBase("SonicScript",VstProductInfo("SonicScript","Ross McKinlay - pinksquirrellabs.com",1), VstPluginCategory.Shell, VstPluginCapabilities.NoSoundInStop, 0, 42424242)
    let mutable bypass = false
    override this.CreateAudioProcessor(instance) = 
        match instance with
        | null -> SonicScriptProcessor(this) :> _
        | instance -> base.CreateAudioProcessor(instance)

    override this.CreatePrograms(instance) =
        match instance with        
        | null -> PluginPrograms(this) :> _
        | instance -> base.CreatePrograms(instance)


    override this.CreateEditor instance =
        match instance with
        | null -> PluginEditor(this) :> _
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
//    let handler = System.ResolveEventHandler(fun _ args ->
//        let asmName = AssemblyName(args.Name)
//        // assuming that we reference only dll files
//        let expectedName = asmName.Name + ".dll"
//        let expectedLocation =
//            System.IO.Path.Combine(@"f:\utils\vstplugins\", expectedName)
//        if System.IO.File.Exists expectedLocation then Assembly.LoadFrom expectedLocation else null
//        )
//    do System.AppDomain.CurrentDomain.add_AssemblyResolve handler
    override __.CreatePluginInstance() = 
        System.Diagnostics.Debugger.Break()
        let sbOut = new StringBuilder()
        let sbErr = new StringBuilder()
        let inStream = new StringReader("")
        let outStream = new StringWriter(sbOut)
        let errStream = new StringWriter(sbErr)
    
        // Build command line arguments & start FSI session
        let argv = [| "C:\\fsi.exe" |]
        let allArgs = Array.append argv [|"--noninteractive"|]
        //let c =   FsiEvaluationSession.GetDefaultConfiguration()
//        let s =  FsiEvaluationSession.Create(c, allArgs, inStream, outStream, errStream)   
//   
        new SonicScriptPlugin() :> _

    