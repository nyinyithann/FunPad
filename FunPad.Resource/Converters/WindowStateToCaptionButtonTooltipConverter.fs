namespace FunPad

open System.Windows.Data
open System.Windows
open System.Windows.Input
open System
open System.Globalization

type WindowStateToCaptionButtonTooltipConverter() =
    interface IValueConverter with
        member x.Convert(value : obj, targetType : Type, parameter : obj, culture: System.Globalization.CultureInfo) =
            let state = value :?> WindowState
            match state with
            | WindowState.Maximized -> box "Restore Down"
            | _ -> box "Maximize"

        member x.ConvertBack(value : obj, targetType : Type, parameter : obj, culture: System.Globalization.CultureInfo) =
            DependencyProperty.UnsetValue