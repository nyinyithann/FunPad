namespace FunPad

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Markup
open System.Windows.Input
open Microsoft.Windows.Shell
open System.Windows.Media.Imaging
open ICSharpCode.AvalonEdit
open ICSharpCode.AvalonEdit.Document
open ICSharpCode.AvalonEdit.Editing
open System.Windows.Media
open System.Collections.Concurrent
open System.Diagnostics
open System.Text.RegularExpressions
open System.IO
open System.Text
open Microsoft.Win32

type MainWindow() as self =
    inherit Window(Title = "F# Interactive Pad", WindowStartupLocation = WindowStartupLocation.CenterScreen)

    interface IDisposable with
        member self.Dispose() =
            self.CloseFSIProcess()

    [<DefaultValue>]
    val mutable rootLayout : Grid 

    [<DefaultValue>]
    val mutable editorContainer : Border 

    [<DefaultValue>]
    val mutable editor : ICSharpCode.AvalonEdit.TextEditor

    let fsiProcess = new Process()
    let outputQueue = new ConcurrentQueue<string>()  
    
    [<Literal>]                             
    let PROMPT = "> "    

    let mutable cleared = false
    let history = new ResizeArray<string>()
    let mutable historyPointer = 0
    let mutable expectedPrompts = 0
    let mutable canScriptFileLoaded = true

    [<DefaultValue>]
    val mutable setting : InteractiveOption

    let makeCommandBinding (command:ICommand, execute :ExecutedRoutedEventHandler, canExecute : CanExecuteRoutedEventHandler) =
        self.CommandBindings.Add(new CommandBinding(command, execute, canExecute)) |> ignore

    let setAppIcon() =
        let ibd = new IconBitmapDecoder(new Uri(@"pack://application:,,/Resources/Images/AppIcon.ico", UriKind.RelativeOrAbsolute),
                        BitmapCreateOptions.None, BitmapCacheOption.Default)
        self.Icon <- ibd.Frames.[0]
    
    let checkFileExtension fileName  =
        let f = new FileInfo(fileName)
        f.Extension = ".fsx" || f.Extension = ".fs"         
   
    let onDrop (e:DragEventArgs) =
        let files = e.Data.GetData(DataFormats.FileDrop,true) :?> string array
        files 
            |> Array.filter (fun x -> File.Exists(x) && checkFileExtension(x))
            |> Array.iter (fun x -> self.AcceptCommand("#load @\"" + x + "\";;") |> ignore)        
        e.Handled <- true
        
    let initializeEditor() = 
        self.editor <- new ICSharpCode.AvalonEdit.TextEditor(                       
                        SyntaxHighlighting= Highlighting.HighlightingManager.Instance.GetDefinition("F#"),     
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,                    
                        FontFamily = new FontFamily("Consolas"), Padding = Thickness(5.0),
                        FontSize = 14.0, IsTabStop = false,
                        FontWeight = FontWeights.Normal,
                        Margin = new Thickness(2.0))  
        self.editor.ContextMenu <- self.rootLayout.Resources.["editorContextMenu"] :?> ContextMenu                                              
        self.editor.Options.ConvertTabsToSpaces <- true
        self.editor.Options.EnableHyperlinks <- true
        self.editor.Options.EnableEmailHyperlinks <- true
        self.editor.Options.RequireControlModifierForHyperlinkClick <- false          
        self.editor.Options.CutCopyWholeLine <- false
        self.editor.WordWrap <- true
        self.editor.TextArea.ReadOnlySectionProvider <- new BeginReadOnlySectionProvider()        
        self.editor.PreviewDrop.Add(onDrop)
                    
    let dataOrErrorhandler = 
        new DataReceivedEventHandler(fun _ e -> 
            outputQueue.Enqueue(e.Data)
            self.Dispatcher.BeginInvoke(Threading.DispatcherPriority.Send,Action(fun () -> self.ReadAll())) |> ignore)           
    do                              
        setAppIcon()         
        self.rootLayout <- Application.LoadComponent(new System.Uri("/FunPad;component/RootLayout.xaml", UriKind.Relative)) :?> Grid   
        self.Content <- self.rootLayout
        self.rootLayout.DataContext <- self
        initializeEditor()               
        self.editorContainer <- self.rootLayout.FindName("editorContainer") :?> Border
        self.editorContainer.Child <- self.editor
        self.editor.TextArea.PreviewKeyDown.Add ( fun e -> e.Handled <- self.HandleInput(e.Key))
        self.AppendPrompt()
        self.SetSetting(InteractiveOption.Load())
        self.InitFSIProcess()                
        self.BindCommands()   
        FocusManager.SetFocusedElement(self,self.editor) 

    member self.Editor with get() = self.editor
                                      
    member private self.SetSetting(options : InteractiveOption ) =           
        self.setting <- options       
        self.editor.FontFamily <- getFont self.setting.EditorOption.FontName
        self.editor.FontSize <- self.setting.EditorOption.FontSize            
        self.editor.Options.IndentationSize <- int self.setting.EditorOption.IdentSize
        self.editor.WordWrap <- self.setting.EditorOption.WordWrap
        if self.setting.EditorOption.SyntaxHilighting then
            self.editor.SyntaxHighlighting <- Highlighting.HighlightingManager.Instance.GetDefinition("F#")
        else
            self.editor.SyntaxHighlighting <- null
        self.editor.Foreground <- new SolidColorBrush(self.setting.EditorOption.FontColor)                  
        self.editor.Background <- new SolidColorBrush(self.setting.EditorOption.BackgroundColor)          
        self.editor.ShowLineNumbers <- self.setting.EditorOption.ShowLineNumber
        self.editor.Options.ConvertTabsToSpaces <- self.setting.EditorOption.ConvertTabsToSpaces        
        self.editor.Options.ShowEndOfLine <- self.setting.EditorOption.ShowEndOfLine
        self.editor.Options.ShowSpaces <- self.setting.EditorOption.ShowSpace
        self.editor.Options.ShowTabs <- self.setting.EditorOption.ShowTabs
        self.setting.Save();
                       
    member self.AcceptCommand (cmd:string) =        
        let mutable cmdText = cmd
        if cmd.StartsWith("#") then
            cmdText <- Regex.Replace(cmd, @"\s", String.Empty)

        let check (c:string) (txt:string) = c.ToLower().Contains(txt.ToLower()) && c.EndsWith(";;")
        
        if  check cmdText "#quit" || check cmdText "#q" || check cmdText "#reset" then            
            self.RestartFSIProcess()
            true
        elif check cmdText "#cls" then
            self.ClearConsole()
            true
        elif cmdText.EndsWith(";;", StringComparison.Ordinal) then
            expectedPrompts <- expectedPrompts + 1
            fsiProcess.StandardInput.WriteLine(cmdText)            
            true
        else false 
                                            
    member private self.InitFSIProcess() =        
        let fsiPath = if String.IsNullOrWhiteSpace(self.setting.FSIPath) then InteractiveOption.Default.FSIPath                        
                      else self.setting.FSIPath                                  
        if File.Exists(fsiPath) then
            fsiProcess.StartInfo.FileName <- fsiPath
            fsiProcess.StartInfo.UseShellExecute <- false
            fsiProcess.StartInfo.CreateNoWindow <- true
            fsiProcess.StartInfo.RedirectStandardError <- true
            fsiProcess.StartInfo.RedirectStandardInput <- true
            fsiProcess.StartInfo.RedirectStandardOutput <- true
            fsiProcess.EnableRaisingEvents <- true;         
            fsiProcess.StartInfo.Arguments <- self.setting.FSIOptions               
            fsiProcess.OutputDataReceived.AddHandler(dataOrErrorhandler)
            fsiProcess.ErrorDataReceived.AddHandler(dataOrErrorhandler)     
            try
                fsiProcess.Start() |> ignore        
                fsiProcess.BeginErrorReadLine()
                fsiProcess.BeginOutputReadLine()                 
                canScriptFileLoaded <- true
            with
            | :? System.ComponentModel.Win32Exception as e -> 
                self.editor.SyntaxHighlighting <- null
                self.editor.Document.Text <- String.Empty
                self.editor.Document.Insert(0, e.Message + Environment.NewLine + "Please use suitable fsi.exe to run on this machine." + Environment.NewLine +
                                            "Please locate fis.exe in Options dialog")                            
        else           
            canScriptFileLoaded <- false
            self.editor.SyntaxHighlighting <- null  
            self.editor.Document.Text <- String.Empty
            self.editor.Document.Insert(0,"FunPad cannot locate " + self.setting.FSIPath  + " in your machine." + Environment.NewLine + 
                                        "Please locate fsi.exe in Options dialog." + Environment.NewLine +
                                        "Or make sure to install fsharp. Please visit http://www.fsharp.org for more info. ")            
    
    member private self.CloseFSIProcess() =
        try
            if not fsiProcess.HasExited then            
                fsiProcess.CancelErrorRead()
                fsiProcess.CancelOutputRead()
                fsiProcess.OutputDataReceived.RemoveHandler(dataOrErrorhandler)
                fsiProcess.ErrorDataReceived.RemoveHandler(dataOrErrorhandler)
                fsiProcess.Kill()
                fsiProcess.WaitForExit(5000) |> ignore
        with    
            | _ -> printfn "Please locate fsi.exe"
    
    member private self.RestartFSIProcess() =                                                 
        self.CloseFSIProcess()        
        self.ClearConsole()
        self.InitFSIProcess()
    
    member private self.EndOffset 
        with get() = self.editor.TextArea.ReadOnlySectionProvider?EndOffset
        and set v = self.editor.TextArea.ReadOnlySectionProvider?EndOffset <- v

    member private self.ClearConsole() =
        self.editor.Document.Text <- String.Empty
        cleared <- true
        self.AppendPrompt()

    member private self.CommandText 
        with get() = self.editor.Document.GetText( new TextSegment(StartOffset = self.EndOffset, EndOffset = self.editor.Document.TextLength))
        and set v = self.editor.Document.Replace(new TextSegment(StartOffset = self.EndOffset, EndOffset = self.editor.Document.TextLength), v)

    member private self.HandleInput (key : Key) : bool =
        match key with
        | Key.Back
        | Key.Delete ->
            if self.editor.SelectionStart = 0  && self.editor.SelectionLength = self.editor.Document.TextLength then
                self.ClearConsole()
                true
            else false
        | Key.Down ->
            try
                if self.CommandText.Contains("\n") then false
                else
                    historyPointer <- min (historyPointer + 1)  history.Count
                    if historyPointer = history.Count then
                        self.CommandText <- String.Empty
                    else
                        self.CommandText <- history.[historyPointer]
                    self.editor.ScrollToEnd()
                    true
            with |_ -> false // Sometimes IndexOutOfRangeException throws and ignore it.
        | Key.Up ->
            try
                if self.CommandText.Contains("\n") then false
                else
                    historyPointer <- max (historyPointer - 1) 0
                    if historyPointer = history.Count then
                        self.CommandText <- String.Empty
                    else
                        self.CommandText <- history.[historyPointer]
                    self.editor.ScrollToEnd()
                    true
            with |_ -> false // Sometimes IndexOutOfRangeException throws and ignore it.
        | Key.Return ->
            if Keyboard.Modifiers = ModifierKeys.Shift then false
            else
                let caretOffset = self.editor.CaretOffset
                let cmdText = self.CommandText
                cleared <- false
                if self.AcceptCommand(cmdText) then
                    let document = self.editor.Document
                    if not cleared then 
                        if not (document.GetCharAt(document.TextLength - 1) = '\n') then
                            document.Insert(document.TextLength,Environment.NewLine)
                        self.AppendPrompt()
                        self.editor.Select(document.TextLength, 0)
                    else
                        self.CommandText <- String.Empty
                    cleared <- false
                    history.Add(cmdText)
                    historyPointer <- history.Count
                    self.editor.ScrollToEnd()
                    true
                else false
        | _ -> false

    member private self.ReadAll() =
        let sb = new StringBuilder()
        let appendLine = sb.AppendLine >> ignore
        while outputQueue.Count > 0 do
            let _, v = outputQueue.TryDequeue()
            appendLine v
        let offset = ref 0
        for i = 0 to expectedPrompts do
            if (!offset + 1) < sb.Length && sb.[!offset] = '>' && sb.[!offset + 1] = ' ' then
                offset := !offset + 2                    
        expectedPrompts <- 0                
        let text = sb.ToString(!offset, sb.Length - !offset)                                           
        self.editor.Document.Insert(self.EndOffset - PROMPT.Length, text)
        self.editor.ScrollToEnd()
        self.EndOffset <- self.EndOffset + text.Length    
        self.editor.Document.UndoStack.ClearAll() 

    member private self.AppendPrompt() =
        self.editor.AppendText(PROMPT)
        self.SetReadonly()
        self.editor.Document.UndoStack.ClearAll()        
    
    member private self.SetReadonly() =
        self.editor.TextArea.ReadOnlySectionProvider?EndOffset <- self.editor.Document.TextLength
                                                                                                
    member private self.BindCommands() =
        makeCommandBinding(SystemCommands.CloseWindowCommand,
                ExecutedRoutedEventHandler(fun _ _ -> 
                                               self.CloseFSIProcess()
                                               SystemCommands.CloseWindow(self)),                    
                CanExecuteRoutedEventHandler(fun _ e -> e.CanExecute <- true ))        
        makeCommandBinding(SystemCommands.MinimizeWindowCommand,
                ExecutedRoutedEventHandler(fun _ _ -> SystemCommands.MinimizeWindow(self)),                                                                                                                    
                CanExecuteRoutedEventHandler(fun _ e -> e.CanExecute <- true ))                       
        makeCommandBinding(SystemCommands.MaximizeWindowCommand,
                ExecutedRoutedEventHandler(fun _ e ->
                                              match (e.Parameter :?> WindowState) with
                                                | WindowState.Maximized -> SystemCommands.RestoreWindow(self)                                                
                                                | _ -> SystemCommands.MaximizeWindow(self)),                    
                CanExecuteRoutedEventHandler(fun _ e -> e.CanExecute <- true ))
        makeCommandBinding(ApplicationCommands.Undo,
                ExecutedRoutedEventHandler(fun _ e -> self.Editor.Undo() |> ignore),                    
                CanExecuteRoutedEventHandler(fun _ e -> e.CanExecute <- false && self.editor.CanUndo ))       
        makeCommandBinding(ApplicationCommands.Redo,
                ExecutedRoutedEventHandler(fun _ e -> self.Editor.Redo() |> ignore),                    
                CanExecuteRoutedEventHandler(fun _ e -> e.CanExecute <- self.editor.CanRedo ))   
        makeCommandBinding(FunPadCommands.ClearHistoryCommand,
                ExecutedRoutedEventHandler(fun s (e:ExecutedRoutedEventArgs) -> history.Clear()),                
                CanExecuteRoutedEventHandler(fun s (e:CanExecuteRoutedEventArgs) -> e.CanExecute <- history.Count > 0 ))    
        makeCommandBinding(FunPadCommands.ClearAllCommand,                
                ExecutedRoutedEventHandler(fun s (e:ExecutedRoutedEventArgs) -> self.ClearConsole()),                
                CanExecuteRoutedEventHandler(fun s (e:CanExecuteRoutedEventArgs) -> e.CanExecute <- true ))  
        makeCommandBinding(FunPadCommands.ResetSessionCommand,
                ExecutedRoutedEventHandler(fun s (e:ExecutedRoutedEventArgs) -> self.RestartFSIProcess()),                
                CanExecuteRoutedEventHandler(fun s (e:CanExecuteRoutedEventArgs) -> e.CanExecute <- true ))          
    
        makeCommandBinding(FunPadCommands.LoadScriptFileCommand,
                ExecutedRoutedEventHandler(fun s (e:ExecutedRoutedEventArgs) -> 
                    let dialog = new OpenFileDialog()                    
                    dialog.DefaultExt <- ".fsx"
                    dialog.Filter <- "F# Script File |*.fsx|F# Source File |*.fs"
                    dialog.RestoreDirectory <- true
                    let r = dialog.ShowDialog()
                    if r.HasValue && r.Value then
                        self.AcceptCommand("#load @\"" + dialog.FileName + "\";;") |> ignore
                    ),              
                CanExecuteRoutedEventHandler(fun s (e:CanExecuteRoutedEventArgs) -> e.CanExecute <- canScriptFileLoaded))

        makeCommandBinding
            (ApplicationCommands.Save,
            ExecutedRoutedEventHandler(fun s (e:ExecutedRoutedEventArgs ) ->  
                    let dialog = new SaveFileDialog()
                    dialog.DefaultExt <- ".txt"
                    dialog.Filter <- "Text File |*.txt" 
                    dialog.FileName <- "FSharp"
                    dialog.RestoreDirectory <- true                        
                    let r = dialog.ShowDialog()
                    if r.HasValue && r.Value then    
                        try                     
                            use f = new FileStream(dialog.FileName, FileMode.Create)
                            use writer = new StreamWriter(f)
                            writer.Write(self.editor.Text)
                            writer.Flush()                                                         
                        with
                        | _ -> MessageBox.Show("Sorry, there was an error saving the file.\nPlease try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error) |> ignore),
            CanExecuteRoutedEventHandler(fun s (e:CanExecuteRoutedEventArgs ) -> e.CanExecute <- self.editor.Text.Length > 0))

        makeCommandBinding(FunPadCommands.OptionsCommand,
                ExecutedRoutedEventHandler(fun s (e:ExecutedRoutedEventArgs) -> 
                    let dialog = new OptionDialog(self.setting)     
                    dialog.Owner <- self                
                    Utility. makeChrome dialog  
                    dialog.ShowDialog() |> ignore
                    match dialog.DialogResult with
                    | DialogResult.OK -> 
                        self.SetSetting(dialog.Settings)                        
                    | DialogResult.Reset -> 
                        self.SetSetting(InteractiveOption.Default)                                          
                    | _ -> ()
                    ),                
                CanExecuteRoutedEventHandler(fun s (e:CanExecuteRoutedEventArgs) -> e.CanExecute <- true )) 

        makeCommandBinding(ApplicationCommands.Help,
                ExecutedRoutedEventHandler(fun s (e:ExecutedRoutedEventArgs) -> 
                        Process.Start( ProcessStartInfo(e.Parameter :?> string)) |> ignore
                    ),                
                CanExecuteRoutedEventHandler(fun s (e:CanExecuteRoutedEventArgs) -> e.CanExecute <- true )) 