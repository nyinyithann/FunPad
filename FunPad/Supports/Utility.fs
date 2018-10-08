namespace FunPad

open System.Reflection
open System.Windows

[<AutoOpen>]
module Utility = 

    (*: Expert F# 2nd Edition Page 243 :*)
    let (?) (o:obj) (name:string) : 'T =
            o.GetType().InvokeMember(name, BindingFlags.GetProperty, null, o, [||]) |> unbox<'T>
    let (?<-) (o:obj) (name:string) (v:obj) : unit =
            o.GetType().InvokeMember(name, BindingFlags.SetProperty, null, o, [|v|]) |> ignore

    let makeChrome (win : Window) =
            win.Style <-  Application.Current.Resources.["ChromeWindowStyle"] :?> Style
            let chrome = new Microsoft.Windows.Shell.WindowChrome()
            chrome.ResizeBorderThickness <- new Thickness(7.0)
            chrome.CaptionHeight <- 40.0        
            chrome.GlassFrameThickness <- new Thickness(0.0)
            chrome.CornerRadius <- new CornerRadius(10.0)        
            Microsoft.Windows.Shell.WindowChrome.SetWindowChrome(win,chrome)