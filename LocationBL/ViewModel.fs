namespace ViewModel

//#r "System.Runtime.Serialization" // for interactive
//#r "FSharp.PowerPack"
// Reference to assembly System.Runtime.Serialization and System.Xml
//#load "Logics.fs"

open System
open System.IO
open System.Runtime.Serialization.Json
open System.Xml
open System.Text
open System.ComponentModel
//open Logics

type MainViewModel() =

    let mutable latitude, longitude, place = "", "", ""
    let mutable notLoading = true

    let event = new Event<_,_>()
    
    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member x.PropertyChanged = event.Publish
    
    member x.TriggerPropertyChanged(name)=
        event.Trigger(x, new PropertyChangedEventArgs(name))
    
    member x.Latitude 
        with get() = latitude
        and set t = 
                latitude <- t
                x.TriggerPropertyChanged "Latitude"

    member x.Longitude 
        with get() = longitude
        and set t = 
                longitude <- t
                x.TriggerPropertyChanged "Longitude"

    member x.Place
        with get() = place
        and set t = 
                place <- t
                x.TriggerPropertyChanged "Place"

    member x.IsNotLoading
        with get() = notLoading
        and set b = 
                notLoading <- b 
                x.TriggerPropertyChanged "IsNotLoading"

    member x.Refresh() =
        x.IsNotLoading <- false
        let updateCoordinates (lat,lon) = 
            x.Latitude <- lat
            x.Longitude <- lon

            let trigger _ = 
                let update _ = 
                    x.Place <- Logics.processed Logics.callresult
                    x.IsNotLoading <- true
                //UI-thread syncronization with Dispatcher
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(new Action(update)) |> ignore
                //update() |> ignore
            match lat, lon with
            | null, _ | _, null | "", _ | _, "" -> 
                x.IsNotLoading <- true
                x.Place <- "Don't know, try again..."
                "" |> ignore
            | _, _ -> 
                let url = Logics.locationtextUrl lat lon
                Logics.fetchAsyncCached url trigger |> ignore

        Logics.GPSFetch(updateCoordinates) |> ignore
        
    member x.Login() = x.Refresh()
    member x.Save() = x.Refresh()