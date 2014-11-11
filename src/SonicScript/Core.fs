namespace SonicScript

open Jacobi.Vst
open Jacobi.Vst.Core
open Jacobi.Vst.Core.Plugin
open Jacobi.Vst.Framework
open Jacobi.Vst.Framework.Plugin


type SonicScriptProcessor() =
    let mutable blocksize = 0
    let mutable sample = 0.0f
    interface IVstPluginAudioProcessor with
        member x.BlockSize
            with get (): int = 
                blocksize
            and set (v: int): unit = 
                blocksize <- v
        
        member x.InputCount: int = 2
        
        member x.OutputCount: int = 2
        
        member x.Process(inChannels: VstAudioBuffer [], outChannels: VstAudioBuffer []): unit = 
            (inChannels,outChannels)
            ||> Array.iter2(fun input output -> 
                for i in 0..output.SampleCount-1 do
                    output.[i] <- input.[i]*0.2f)
        
        member x.SampleRate
            with get (): float32 = 
                sample
            and set (v: float32): unit = 
                sample <- v
        
        member x.SetPanLaw(``type``: VstPanLaw, gain: float32): bool = 
            false
        
        member x.TailSize: int = 0
        
        
type SonicScriptPlugin() =
    inherit VstPluginWithInterfaceManagerBase("SonicScript",VstProductInfo("SonicScript","Ross McKinlay - pinksquirrellabs.com",1), VstPluginCategory.Synth, VstPluginCapabilities.NoSoundInStop, 0, 42424242)
    let mutable bypass = false
    override this.CreateAudioProcessor(instance) = 
        
        match instance with
        | null -> SonicScriptProcessor() :> _
        | instance -> base.CreateAudioProcessor(instance)

    interface IVstPluginBypass with
        member x.Bypass
            with get (): bool = 
                bypass
            and set (v: bool): unit = 
                bypass <- v
        
        

type SonicScriptPluginCommandStub() =
    inherit StdPluginCommandStub()

    override __.CreatePluginInstance() = 
        //System.Diagnostics.Debugger.Break()
        new SonicScriptPlugin() :> _

    