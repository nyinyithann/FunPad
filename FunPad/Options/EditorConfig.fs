namespace FunPad

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Media
open System.Windows.Media.Imaging
open Microsoft.Win32
open System.IO
open System.Diagnostics
open System.Collections.Concurrent
open System.Collections.Generic
open System.Text.RegularExpressions
open Microsoft.Windows.Controls

type EditorConfig(defaultValue : EditorOption) as self =
        inherit UserControl(SnapsToDevicePixels = true)

        let group = new GroupBox(Header = "Editor Options", FontSize = 11.5, BorderBrush = lineColor)
        let grid = new Grid();
        let fontTypeCombo = new ComboBox(Width = 180., Height = 25., FontWeight = FontWeights.Normal, IsTextSearchEnabled = true, StaysOpenOnEdit = true)                                         
        let fontSizeUpDown = new DoubleUpDown(Minimum = Nullable 1.0, Maximum = Nullable 72., Margin = new Thickness(5.,0.,0.,0.), Width = 70., Height = 25., FontWeight = FontWeights.Normal)
        let fontColorPicker = new ColorPicker(Width = 70., Height = 25.)
        let backgroundColorPicker = new ColorPicker(Width = 70., Height = 25., Margin = new Thickness(5.,0.,0.,0.))
        let identSizeUpDown = new DoubleUpDown(Minimum = Nullable 1., Maximum = Nullable 24., Width = 70., FontWeight = FontWeights.Normal, Height = 25.)
        let wordwrapChkBox = new CheckBox(Content = "Wordwrap", FontWeight = FontWeights.Normal, Margin = new Thickness(0.,10.,5.,5.), HorizontalAlignment = HorizontalAlignment.Left)                                            
        let syntaxHilightingChkBox = new CheckBox(Content = "Syntax highlighting", FontWeight = FontWeights.Normal, Margin = new Thickness(0.,10.,5.,5.), HorizontalAlignment = HorizontalAlignment.Left)
        let showLineNumberChkBox = new CheckBox(Content = "Show line number",  FontWeight = FontWeights.Normal,  Margin = new Thickness(0.,10.,5.,5.), HorizontalAlignment = HorizontalAlignment.Left)
        let convertTabToSpaceChkBox = new CheckBox(Content = "Convert tab to space",  FontWeight = FontWeights.Normal,  Margin = new Thickness(0.,10.,5.,5.), HorizontalAlignment = HorizontalAlignment.Left)
        let showSpaceChkBox = new CheckBox(Content = "Show space", FontWeight = FontWeights.Normal, Margin = new Thickness(0.,10.,5.,5.), HorizontalAlignment = HorizontalAlignment.Left)
        let showTabChkBox = new CheckBox(Content = "Show tab",  FontWeight = FontWeights.Normal, Margin = new Thickness(0.,10.,5.,5.), HorizontalAlignment = HorizontalAlignment.Left)
        let showEndOfLineChkBox = new CheckBox(Content = "Show end of line",  FontWeight = FontWeights.Normal, Margin = new Thickness(0.,10.,5.,5.), HorizontalAlignment = HorizontalAlignment.Left)                

        let defaultFontIndex name =            
            if Fonts.SystemFontFamilies |> Seq.map (fun x -> x.Source) |> Seq.sort |> Seq.exists ((=) name)  then
                Fonts.SystemFontFamilies |> Seq.map (fun x -> x.Source) |> Seq.sort |> Seq.findIndex ((=) name)
            else 0   

        let setDefaultValue() =
            fontTypeCombo.SelectedIndex <- (defaultFontIndex defaultValue.FontName) 
            fontSizeUpDown.Value <- Nullable defaultValue.FontSize
            fontColorPicker.SelectedColor <- defaultValue.FontColor
            backgroundColorPicker.SelectedColor <- defaultValue.BackgroundColor
            identSizeUpDown.Value <- Nullable defaultValue.IdentSize
            wordwrapChkBox.IsChecked <- Nullable(defaultValue.WordWrap)
            syntaxHilightingChkBox.IsChecked <- Nullable(defaultValue.SyntaxHilighting)
            showLineNumberChkBox.IsChecked <- Nullable(defaultValue.ShowLineNumber)
            convertTabToSpaceChkBox.IsChecked <- Nullable(defaultValue.ConvertTabsToSpaces)
            showSpaceChkBox.IsChecked <- Nullable(defaultValue.ShowSpace)
            showTabChkBox.IsChecked <- Nullable(defaultValue.ShowTabs)
            showEndOfLineChkBox.IsChecked <- Nullable(defaultValue.ShowEndOfLine)                    
                                             
        do            
            [1..3] |> List.iter (fun _ -> grid.RowDefinitions.Add(RowDefinition(Height=GridLength(0.0,GridUnitType.Auto))))
            grid.RowDefinitions.Add(RowDefinition(Height=GridLength(1.0,GridUnitType.Star)))

            let fontLbl = new Label(Content = "Font:", Width = 150.0, FontWeight = FontWeights.Normal)            
            fontTypeCombo.ItemsSource <- Fonts.SystemFontFamilies |> Seq.map (fun x -> x.Source) |> Seq.sort         
            let fontSizeLbl = new Label(Content = "Font Size:", Width = 150.0, FontWeight = FontWeights.Normal, HorizontalContentAlignment = HorizontalAlignment.Right)
            let sp1 = new StackPanel(Orientation = Orientation.Horizontal, Margin = new Thickness(5.))
            Grid.SetRow(sp1,0)            
            sp1.Children.Add (fontLbl) |> ignore
            sp1.Children.Add (fontTypeCombo) |> ignore
            sp1.Children.Add (fontSizeLbl) |> ignore
            sp1.Children.Add (fontSizeUpDown) |> ignore
            grid.Children.Add(sp1) |> ignore

            let fontColorLbl = new Label(Content = "Font Color:", Width = 150.0, FontWeight = FontWeights.Normal)
            let backgroundColorLbl = new Label(Content = "Background Color:", Margin = new Thickness(110.,0.,0.,0.), Width = 150.0, FontWeight = FontWeights.Normal, HorizontalContentAlignment = HorizontalAlignment.Right)
            let sp2 = new StackPanel(Orientation = Orientation.Horizontal, Margin = new Thickness(5.))
            Grid.SetRow(sp2,1)            
            sp2.Children.Add (fontColorLbl) |> ignore
            sp2.Children.Add (fontColorPicker) |> ignore
            sp2.Children.Add (backgroundColorLbl) |> ignore
            sp2.Children.Add (backgroundColorPicker) |> ignore
            grid.Children.Add(sp2) |> ignore
            
            let identLbl = new Label(Content = "Identation Size:", Width = 150.0, FontWeight = FontWeights.Normal)
            let sp3 = new StackPanel(Orientation = Orientation.Horizontal, Margin = new Thickness(5.))
            Grid.SetRow(sp3,2)
            sp3.Children.Add (identLbl) |> ignore
            sp3.Children.Add(identSizeUpDown) |> ignore
            grid.Children.Add(sp3) |> ignore

            let sp4 = new StackPanel(Orientation = Orientation.Horizontal, Margin = new Thickness(5.,15.,0.,0.))
            Grid.SetRow(sp4,3)
            let sp4_1 = new StackPanel(Orientation = Orientation.Vertical, Margin = new Thickness(5.,0.,0.,0.), Width = 150.)
            [|wordwrapChkBox; syntaxHilightingChkBox; showLineNumberChkBox|] |> Array.iter (sp4_1.Children.Add >> ignore)
            sp4.Children.Add(sp4_1) |> ignore
            let sp4_2 = new StackPanel(Orientation = Orientation.Vertical, Margin = new Thickness(5.,0.,0.,0.), Width = 150.)
            [|convertTabToSpaceChkBox; showSpaceChkBox; showTabChkBox|] |> Array.iter (sp4_2.Children.Add >> ignore)
            sp4.Children.Add(sp4_2) |> ignore
            let sp4_3 = new StackPanel(Orientation = Orientation.Vertical, Margin = new Thickness(5.,0.,0.,0.), Width = 150.)
            sp4_3.Children.Add(showEndOfLineChkBox) |> ignore            
            sp4.Children.Add(sp4_3) |> ignore
            grid.Children.Add(sp4) |> ignore

            setDefaultValue()
            group.Content <- grid
            self.Content <- group

        member self.Settings 
            with get() = 
                Some({
                        FontName = fontTypeCombo.SelectedItem.ToString()
                        FontSize = if fontSizeUpDown.Value.HasValue then fontSizeUpDown.Value.Value else 12.0
                        FontColor = fontColorPicker.SelectedColor
                        BackgroundColor = backgroundColorPicker.SelectedColor
                        IdentSize = if identSizeUpDown.Value.HasValue then identSizeUpDown.Value.Value else 4.0
                        WordWrap = wordwrapChkBox.IsChecked.Value
                        SyntaxHilighting = syntaxHilightingChkBox.IsChecked.Value
                        ConvertTabsToSpaces = convertTabToSpaceChkBox.IsChecked.Value
                        ShowSpace = showSpaceChkBox.IsChecked.Value
                        ShowTabs = showTabChkBox.IsChecked.Value
                        ShowEndOfLine = showEndOfLineChkBox.IsChecked.Value
                        ShowLineNumber = showLineNumberChkBox.IsChecked.Value                        
                    })
