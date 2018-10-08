namespace FunPad

open System
open System.Text
open System.Windows
open System.Windows.Controls
open System.Windows.Input
open System.Windows.Data
open System.Windows.Media
open System.Windows.Media.Imaging
open Microsoft.Win32
open System.IO
open System.Diagnostics
open System.Collections.Concurrent
open System.Collections.Generic
open System.Text.RegularExpressions

type OptionDialog (defaultValue : InteractiveOption) as self=
    inherit Window(Title = "Options", WindowStartupLocation = WindowStartupLocation.CenterScreen, Width = 650., Height = 470.,
                    ShowInTaskbar = false, SnapsToDevicePixels = true, ResizeMode = ResizeMode.NoResize, FontSize = 12.)
                       
    let rootGrid = new Grid(Margin=Thickness(5.,5.,5.,5.))        
    let fsiOptionsText = new TextBox(MaxLength = 500, Width = 400.0, Height = 25., FontWeight = FontWeights.Normal, TabIndex = 1,
                                        ToolTip = "The command line arguments passed to fsi.exe.")
    let fsiPathText = new TextBox(MaxLength = 500, Width = 400.0, Height = 25., FontWeight = FontWeights.Normal, TabIndex = 2,
                                    ToolTip="The path to fsi.exe.")
    let errorLabel = new Label(FontSize = 10.0, Height = 25., FontWeight = FontWeights.Normal, HorizontalAlignment = HorizontalAlignment.Left, Margin = Thickness(5.,0.,10.,0.), Foreground = Brushes.Red)
    let fsiPathButton = new Button(Content="...", Height = 25., Width = 30., Margin = Thickness(5.,0.,0.,0.), FontWeight = FontWeights.Normal,
                                    TabIndex = 3, Command = ApplicationCommands.Open)
    let resetButton = new Button(Content = "Reset", Height = 25., Width = 80., HorizontalAlignment = HorizontalAlignment.Left)
    let okButton = new Button(Content = "OK", Height = 25., Width = 80., HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(5.,0.,0.,0.), IsDefault = true )
    let cancelButton = new Button(Content = "Cancel", Height = 25., Width = 80., HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(5.,0.,0.,0.), IsCancel = true)
    let editorConfigPad = new EditorConfig(defaultValue.EditorOption)
    let mutable setting = defaultValue
    let mutable optionDialogResult = DialogResult.Cancel    
      
    let buildFsiOptionGruop () =
        let group = new GroupBox(Header = "Interactive Options", FontSize = 11.5, BorderBrush = lineColor)        
        let grid = new Grid()
        [1..3] |> List.iter (fun _ -> grid.RowDefinitions.Add(RowDefinition(Height=GridLength(0.0,GridUnitType.Auto))))            
            
        let sp1 = new StackPanel(Orientation = Orientation.Horizontal, Margin = new Thickness(5.,5.,0.,0.))
        Grid.SetRow(sp1,0)            
        sp1.Children.Add(Label(Content="F# Interactive Startup:", Width = 150.0, FontWeight = FontWeights.Normal) :> UIElement) |> ignore
        sp1.Children.Add(fsiOptionsText :> UIElement) |> ignore            
        grid.Children.Add(sp1) |> ignore

        let sp2 = new StackPanel(Orientation = Orientation.Horizontal, Margin = new Thickness(5.,0.,0.,0.))
        Grid.SetRow(sp2,1)            
        sp2.Children.Add(Label(Content="F# Interactive Path:", Width = 150.0, FontWeight = FontWeights.Normal) :> UIElement) |> ignore
        sp2.Children.Add(fsiPathText :> UIElement) |> ignore
        sp2.Children.Add(fsiPathButton) |> ignore
        grid.Children.Add(sp2) |> ignore            

        let lbl = new Label(Content="Changing Interactive Options requires restarting fsi seesion", Margin = Thickness(150.,0.,0.,0.),
                            FontWeight = FontWeights.Normal, Foreground = Brushes.Red)
        Grid.SetRow(lbl,2)
        grid.Children.Add(lbl) |> ignore            
        group.Content <- grid
        group

    let getFsiPath() =            
        let dialog = new OpenFileDialog() 
        dialog.DefaultExt <- ".exe"
        dialog.Filter <- "fsi|fsi.exe"             
        dialog.RestoreDirectory <- true
        let r = dialog.ShowDialog()
        if r.HasValue && r.Value = true  then  
            fsiPathText.Text <- dialog.FileName                                 
       
    do                                
        rootGrid.RowDefinitions.Add(RowDefinition(Height=GridLength(0.0,GridUnitType.Auto)))
        rootGrid.RowDefinitions.Add(RowDefinition(Height=GridLength(0.0,GridUnitType.Auto)))
        rootGrid.RowDefinitions.Add(RowDefinition(Height=GridLength(1.0,GridUnitType.Star)))
        rootGrid.RowDefinitions.Add(RowDefinition(Height=GridLength(0.0,GridUnitType.Auto)))
        rootGrid.RowDefinitions.Add(RowDefinition(Height=GridLength(0.0,GridUnitType.Auto)))

        let titlePanel = new StackPanel(Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin= Thickness(0.,-10.,0.,10.))
        let titleTextBlock = new TextBlock(Text = "Options", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center)
        titleTextBlock.Style <- Application.Current.Resources.["AppTitleStyle"] :?> Style
        titlePanel.Children.Add(titleTextBlock) |> ignore
        Grid.SetRow(titlePanel,0)
        rootGrid.Children.Add(titlePanel) |> ignore

        let closeCaptionButton = new Button(ToolTip="Close", Margin=Thickness(0.,-10.,0.,10.), Width = 35., Height = 25.,HorizontalAlignment = HorizontalAlignment.Right)
        closeCaptionButton.Style <- Application.Current.Resources.["CloseCaptionButtonStyle"] :?> Style
        closeCaptionButton.SetValue(Microsoft.Windows.Shell.WindowChrome.IsHitTestVisibleInChromeProperty, true)
        closeCaptionButton.Command <- Microsoft.Windows.Shell.SystemCommands.CloseWindowCommand
        Grid.SetRow(closeCaptionButton,0)
        rootGrid.Children.Add(closeCaptionButton) |> ignore

        let fsiGroup = buildFsiOptionGruop()
        Grid.SetRow(fsiGroup,1)
        rootGrid.Children.Add(fsiGroup) |> ignore
            
        Grid.SetRow(editorConfigPad,2)
        rootGrid.Children.Add(editorConfigPad) |> ignore

        let rectangle = new System.Windows.Shapes.Rectangle(StrokeThickness = 1., Stroke = lineColor, Margin = Thickness(0.,7.,0.,5.))
        Grid.SetRow(rectangle,3)
        rootGrid.Children.Add(rectangle) |> ignore

        let sp = new StackPanel(Orientation = Orientation.Horizontal, Margin = new Thickness(0.,5.,0.,0.), HorizontalAlignment = HorizontalAlignment.Right)
        Grid.SetRow(sp,4)
        sp.Children.Add(errorLabel) |> ignore
        resetButton.Click.Add(fun e ->
                                optionDialogResult <- DialogResult.Reset
                                self.Close()
                                )
        sp.Children.Add(resetButton) |> ignore
        okButton.Click.Add(fun e -> 
                            if File.Exists(fsiPathText.Text.Trim()) then
                                setting <- {
                                                FSIOptions = fsiOptionsText.Text.Trim()
                                                FSIPath = fsiPathText.Text.Trim()
                                                EditorOption = Option.get editorConfigPad.Settings
                                            }
                                optionDialogResult <- DialogResult.OK
                                self.Close()
                            else
                                errorLabel.Content <- String.Format("The given F# interactive path {0} is not found!", fsiPathText.Text))
        sp.Children.Add(okButton) |> ignore
        cancelButton.Click.Add(fun e ->
                                optionDialogResult <- DialogResult.Cancel
                                self.Close()
                                )
        sp.Children.Add(cancelButton) |> ignore
        rootGrid.Children.Add(sp) |> ignore

        self.Content <- rootGrid

        fsiOptionsText.Text <- defaultValue.FSIOptions
        fsiPathText.Text <- defaultValue.FSIPath

        self.CommandBindings.Add(
            new CommandBinding(
                ApplicationCommands.Open,
                (fun s (e:ExecutedRoutedEventArgs ) ->  getFsiPath()),                
                (fun s (e:CanExecuteRoutedEventArgs ) -> e.CanExecute <- true))) |> ignore
        self.CommandBindings.Add(
            new CommandBinding(
                Microsoft.Windows.Shell.SystemCommands.CloseWindowCommand,
                (fun s (e:ExecutedRoutedEventArgs ) ->  Microsoft.Windows.Shell.SystemCommands.CloseWindow(self)),                
                (fun s (e:CanExecuteRoutedEventArgs ) -> e.CanExecute <- true))) |> ignore
                        
    member self.Settings with get() = setting
    member self.DialogResult with get() = optionDialogResult           
                     