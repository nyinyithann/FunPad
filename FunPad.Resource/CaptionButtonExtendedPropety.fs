namespace FunPad

open System.Windows

type CaptionButtonExtendedProperty() =
        inherit DependencyObject()

        static let ownerWindowStateProperty =            
                DependencyProperty.RegisterAttached("OwnerWindowState", typeof<WindowState>, typeof<CaptionButtonExtendedProperty>)

        static member public SetOwnerWindowState(dpObj : DependencyObject, windowState : WindowState) =
            dpObj.SetValue(ownerWindowStateProperty, windowState)

        static member public GetOwnerWindowState(dpObj : DependencyObject) : WindowState =
            dpObj.GetValue(ownerWindowStateProperty) :?> WindowState
        