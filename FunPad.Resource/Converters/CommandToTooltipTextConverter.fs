namespace FunPad

open System.Windows.Data
open System.Windows
open System.Windows.Input
open System
open System.Globalization

type CommandToTooltipTextConverter() =
    interface IValueConverter with
        member x.Convert(value : obj, targetType : Type, parameter : obj, culture: System.Globalization.CultureInfo) =
            
            match value with
            | :? RoutedUICommand as command ->         
                let text = command.Text

                let keyGesture = ((command.InputGestures |> Seq.cast) : seq<InputGesture>)                                
                                    |> Seq.tryPick (function :? KeyGesture as kg-> Some(kg) | _ -> None)

                match keyGesture with
                | Some(kg) -> box <| String.Format("{0} ({1})", text, kg.GetDisplayStringForCulture(CultureInfo.CurrentUICulture))
                | _ -> box text

            | _ -> box String.Empty

        member x.ConvertBack(value : obj, targetType : Type, parameter : obj, culture: System.Globalization.CultureInfo) =
            DependencyProperty.UnsetValue