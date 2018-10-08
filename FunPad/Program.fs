namespace FunPad

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Markup
open System.Windows.Threading 

open System.IO
                  
    type App() as self =
        inherit Application()

        [<DefaultValue>]
        val mutable mainWindow : MainWindow      
        
        let handleUnhandledException(e : DispatcherUnhandledExceptionEventArgs) =
            MessageBox.Show(self.mainWindow, "Sorry! FunPad encounters error. The application will shutdown.", "Warning", MessageBoxButton.OK, MessageBoxImage .Exclamation) |> ignore
            (self.mainWindow :> IDisposable).Dispose()
            Application.Current.Shutdown()        

        let loadResource() =
            let source =  new Uri("/FunPad;component/Resources/AppResource.xaml",  UriKind.RelativeOrAbsolute)
            let dict = new ResourceDictionary(Source = source)            
            Application.Current.Resources.MergedDictionaries.Add(dict)

        do            
            Application.Current.DispatcherUnhandledException.Add(handleUnhandledException)                    
            
        member self.InitializeComponent() =             
            loadResource()
            self.mainWindow <- new MainWindow()                        
            makeChrome self.mainWindow                                                    
        
        member self.RunApp() =
            self.Run(self.mainWindow)                
    
    module Program =
          
        [<STAThread; EntryPoint>]    
        let main _ =         
            let app = new App()
            app.InitializeComponent()
            app.RunApp()
            
