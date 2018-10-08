namespace FunPad

open System
open System.Windows.Media
open System.IO
open System.Xml.Linq


[<AutoOpen>] 
module Options =   

    type DialogResult =
        | OK        = 0
        | Cancel    = 1 
        | Reset     = 2  

    let lineColor = new SolidColorBrush(ColorConverter.ConvertFromString("#44587C") :?> Color)

    type EditorOption = 
            {
                 FontName               : string
                 FontSize               : float
                 FontColor              : Color
                 BackgroundColor        : Color
                 IdentSize              : float
                 WordWrap               : bool
                 SyntaxHilighting       : bool
                 ConvertTabsToSpaces    : bool
                 ShowSpace              : bool
                 ShowTabs               : bool
                 ShowEndOfLine          : bool
                 ShowLineNumber         : bool             
            }    

    let getFont name =
        let f = (fun (x:FontFamily) -> x.Source = name)
        if Fonts.SystemFontFamilies |> Seq.exists f  then
            Fonts.SystemFontFamilies |> Seq.find f
        else Fonts.SystemFontFamilies  |> Seq.item 0

    let creatEditorElement (option : EditorOption) =
        let editorElement = new XElement(XName.Get("Editor"))
        editorElement.Add(new XElement(XName.Get("FontName"), option.FontName))
        editorElement.Add(new XElement(XName.Get("FontSize"), option.FontSize))
        editorElement.Add(new XElement(XName.Get("FontColor"), option.FontColor.ToString()))
        editorElement.Add(new XElement(XName.Get("BackgroundColor"), option.BackgroundColor.ToString()))
        editorElement.Add(new XElement(XName.Get("IdentSize"), option.IdentSize))
        editorElement.Add(new XElement(XName.Get("WordWrap"), option.WordWrap))
        editorElement.Add(new XElement(XName.Get("SyntaxHilighting"), option.SyntaxHilighting))
        editorElement.Add(new XElement(XName.Get("ConvertTabsToSpaces"), option.ConvertTabsToSpaces))
        editorElement.Add(new XElement(XName.Get("ShowSpace"), option.ShowSpace))
        editorElement.Add(new XElement(XName.Get("ShowTabs"), option.ShowTabs))
        editorElement.Add(new XElement(XName.Get("ShowEndOfLine"), option.ShowEndOfLine))
        editorElement.Add(new XElement(XName.Get("ShowLineNumber"), option.ShowLineNumber))
        editorElement

    (*:- Borrowed from http://fsharpnews.blogspot.com/2011/01/patterns-are-everywhere.html -:*)
    let defaultOrValue (_, Some x | x, None) = x

    let getValue (element:XElement option) (name:string) = 
        match element with
        | Some (ele) ->
            match ele.Elements() |> Seq.tryPick (fun x -> if x.Name.LocalName = name then Some(x) else None ) with
            | Some(a) -> Some(a.Value)
            | _ -> None
        | _ -> None

    let tryParseFloat (str : string option) =
        match str with
        | Some(a) -> 
            match Double.TryParse(a) with
            | true, x -> Some(x)
            | _ -> None
        | _ -> None
            
    let tryParseColor (str : string option) =
        match str with
        | Some(a) ->
            match System.Text.RegularExpressions.Regex("^#([A-Fa-f0-9]{8}|[A-Fa-f0-9]{3})$").IsMatch(a) with
            | true -> Some(ColorConverter.ConvertFromString(a) :?> Color)
            | _ -> None
        | _ -> None

    let tryParseBool (str : string option) =
        match str with
        | Some(a) ->
            match Boolean.TryParse(a) with
            | true,x -> Some(x)
            | _ -> None
        | _ -> None
    
    let createEditorOption (rootElement : XElement option) =
        let editorElement = match rootElement with  
                            | Some(a) -> a.Elements() |> Seq.tryPick (fun x -> if x.Name.LocalName = "Editor" then Some(x) else None)
                            | _ -> None  
        {
            FontName = defaultOrValue("Consolas", getValue editorElement "FontName")
            FontSize = defaultOrValue(14.0, tryParseFloat(getValue editorElement "FontSize"))
            FontColor = defaultOrValue (Colors.Black, tryParseColor(getValue editorElement "FontColor"))
            BackgroundColor = defaultOrValue (Colors.White, tryParseColor(getValue editorElement "BackgroundColor"))
            IdentSize = defaultOrValue(4.0, tryParseFloat(getValue editorElement "IdentSize"))
            WordWrap = defaultOrValue(true, tryParseBool(getValue editorElement "WordWrap"))
            SyntaxHilighting = defaultOrValue(true, tryParseBool(getValue editorElement "SyntaxHilighting"))
            ConvertTabsToSpaces = defaultOrValue(true, tryParseBool(getValue editorElement "ConvertTabsToSpaces"))
            ShowSpace = defaultOrValue(true, tryParseBool(getValue editorElement "ShowSpace"))
            ShowTabs = defaultOrValue(true, tryParseBool(getValue editorElement "ShowTabs"))
            ShowEndOfLine = defaultOrValue(true, tryParseBool(getValue editorElement "ShowEndOfLine"))
            ShowLineNumber = defaultOrValue(true, tryParseBool(getValue editorElement "ShowLineNumber"))                                
        }
                
    type InteractiveOption =
        {
                FSIOptions     : string (* comma seperated value *)
                FSIPath        : string (* full path of fsi.exe *)
                EditorOption   : EditorOption
        }
        static member private InteractiveSettingsFilePath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FunPad"),"FunPadSettings.xml")
        static member private DefaultFsiPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)+ @"\Microsoft F#\v4.0\Fsi.exe")
        static member Default =           
            {
                FSIOptions = "--optimize"
                FSIPath = InteractiveOption.DefaultFsiPath
                EditorOption = 
                {
                    FontName = "Consolas"
                    FontSize = 14.0
                    FontColor = Colors.Black
                    BackgroundColor = Colors.White
                    IdentSize = 4.0
                    WordWrap = true
                    SyntaxHilighting = true
                    ConvertTabsToSpaces = true
                    ShowSpace = false
                    ShowTabs = false
                    ShowEndOfLine = false
                    ShowLineNumber = false                                
                }
            }
        member self.Save () =
            let addValues (rootElement:XElement) =
                rootElement.Add(new XElement(XName.Get("FsiOption"), self.FSIOptions))
                rootElement.Add(new XElement(XName.Get("FsiPath"), self.FSIPath))
                let editorElement = creatEditorElement(self.EditorOption)
                rootElement.Add(editorElement)
            
            if not <| File.Exists(InteractiveOption.InteractiveSettingsFilePath) then
                let dirPath = Path.GetDirectoryName(InteractiveOption.InteractiveSettingsFilePath)
                if not <| Directory.Exists(dirPath) then Directory.CreateDirectory(dirPath) |> ignore
                let xdoc = new XDocument(Declaration = XDeclaration("1.0", "utf-8", "yes"));
                let rootElement = new XElement(XName.Get("Settings")) 
                addValues rootElement           
                xdoc.Add(rootElement)
                xdoc.Save(InteractiveOption.InteractiveSettingsFilePath)
            else            
                let xdoc = XDocument.Load(InteractiveOption.InteractiveSettingsFilePath)
                let rootElement = xdoc.Root
                rootElement.RemoveNodes()
                addValues rootElement   
                xdoc.Save(InteractiveOption.InteractiveSettingsFilePath)  
        
        static member Load () : InteractiveOption =             
            if not <| File.Exists(InteractiveOption.InteractiveSettingsFilePath) then InteractiveOption.Default
            else
                try
                    let xdoc = XDocument.Load(InteractiveOption.InteractiveSettingsFilePath)                   
                    let rootElement = xdoc.Elements() |> Seq.tryPick (fun x -> if x.Name.LocalName = "Settings" then Some(x) else None)                                  
                    {
                        FSIOptions = defaultOrValue("--optimize", getValue rootElement "FsiOption")
                        FSIPath = defaultOrValue (InteractiveOption.DefaultFsiPath, getValue rootElement "FsiPath")
                        EditorOption = createEditorOption rootElement                        
                    }
                with
                    | _ -> 
                    try
                        File.Delete(InteractiveOption.InteractiveSettingsFilePath)
                    with _ -> ()
                    InteractiveOption.Default