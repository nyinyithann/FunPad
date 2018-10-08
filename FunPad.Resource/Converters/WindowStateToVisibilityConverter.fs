namespace FunPad

open System.Windows.Data
open System.Windows
open System

type WindowStateToVisibilityConverter() =
    interface IValueConverter with
        member x.Convert(value : obj, targetType : Type, parameter : obj, culture: System.Globalization.CultureInfo) =
            if targetType <> typeof<Visibility> then
                raise <| new InvalidOperationException()

            let state = value :?> WindowState
            let param = parameter :?> string

            match param with
            | "Maximize" -> 
                if state = WindowState.Maximized then box Visibility.Collapsed 
                else box Visibility.Visible
            | "Restore" ->
                if state <> WindowState.Maximized then box Visibility.Collapsed 
                else box Visibility.Visible
            | _ -> box Visibility.Collapsed

        member x.ConvertBack(value : obj, targetType : Type, parameter : obj, culture: System.Globalization.CultureInfo) =
            DependencyProperty.UnsetValue
