namespace FunPad

open System
open System.Windows.Media.Effects
open System.Windows
open System.Windows.Media

type GrayScaleEffect() as self=
    inherit ShaderEffect() 
    
    static let  pixelShader =  new PixelShader( UriSource = new Uri(@"pack://application:,,,/FunPad.XamlResource;component/Effects/GrayscaleEffect.ps") )    
    static let mutable inputProperty = null;
    static let mutable desaturationFactorProperty = null;
            
    static do  pixelShader.Freeze()

    do
        self.PixelShader <- pixelShader
        self.UpdateShaderValue(GrayScaleEffect.InputProperty)
        self.UpdateShaderValue(GrayScaleEffect.DesaturationFactorProperty)

    static let CoerceDesaturationFactor(d : DependencyObject, v : obj)=
        let effect = d :?> GrayScaleEffect
        let newFactor = v :?> double

        if (newFactor < 0.0) || (newFactor > 1.0) then
            box effect.DesaturationFactor
        else
            box newFactor;

    static member InputProperty = 
            match inputProperty with
            | null -> 
                inputProperty <- ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof<GrayScaleEffect>,0)
                inputProperty
            | _ -> inputProperty

    static member DesaturationFactorProperty = 
            match desaturationFactorProperty with
            | null -> 
                desaturationFactorProperty <- DependencyProperty.Register("DesaturationFactor", typeof<double>, typeof<GrayScaleEffect>, 
                                                     new UIPropertyMetadata(0.0, ShaderEffect.PixelShaderConstantCallback(0), (fun d e -> CoerceDesaturationFactor(d,e))))
                desaturationFactorProperty
            | _ -> desaturationFactorProperty
            
    member self.Input 
        with get() = self.GetValue(GrayScaleEffect.InputProperty) :?> Brush
        and set (v : Brush) = self.SetValue(GrayScaleEffect.InputProperty,v)

    member self.DesaturationFactor 
        with get() = self.GetValue(GrayScaleEffect.DesaturationFactorProperty) :?> double
        and set (v : double) = self.SetValue(GrayScaleEffect.DesaturationFactorProperty,v)
    

