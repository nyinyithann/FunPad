namespace FunPad

open System
open System.Windows.Input

type FunPadCommands() =                    
    static let  clearAllCommand = new RoutedUICommand("Clear Screen", "Clear All", typeof<FunPadCommands>)  
    static let  resetSessionCommand = new RoutedUICommand("Reset Session", "Reset Session", typeof<FunPadCommands>) 
    static let  loadScriptFileCommand = new RoutedUICommand("Load F# Script File", "Load", typeof<FunPadCommands>) 
    static let  clearHistoryCommand = new RoutedUICommand("Clear History", "Clear History", typeof<FunPadCommands>) 
    static let  optionsCommand = new RoutedUICommand("Options", "Options", typeof<FunPadCommands>)
                                            
    static member ClearAllCommand = clearAllCommand
    static member ResetSessionCommand = resetSessionCommand
    static member LoadScriptFileCommand = loadScriptFileCommand 
    static member ClearHistoryCommand = clearHistoryCommand
    static member OptionsCommand = optionsCommand    
    

        