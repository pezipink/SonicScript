namespace SonicScript

open Jacobi.Vst
open Jacobi.Vst.Core
open Jacobi.Vst.Core.Plugin
open Jacobi.Vst.Framework
open Jacobi.Vst.Framework.Plugin

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
        program;

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
        paramInfo.Name <- "Tap";
        paramInfo.Label <- "Tap";
        paramInfo.ShortLabel <- "|";
        paramInfo.MinInteger <- 0;
        paramInfo.MaxInteger <- 2;
        paramInfo.LargeStepFloat <- 1.0f;
        paramInfo.SmallStepFloat <- 1.0f;
        paramInfo.StepFloat <- 1.0f;
        paramInfo.CanRamp <- true;
        paramInfo.DefaultValue <- 0.0f;
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

    member this.PluginPrograms() = this.GetInstance<PluginPrograms>() : PluginPrograms
    

    interface IVstPluginBypass with
        member x.Bypass
            with get (): bool = 
                bypass
            and set (v: bool): unit = 
                bypass <- v
        
        

type SonicScriptPluginCommandStub() =
    inherit StdPluginCommandStub()

    override __.CreatePluginInstance() = 
   //     System.Diagnostics.Debugger.Break()
        new SonicScriptPlugin() :> _

    