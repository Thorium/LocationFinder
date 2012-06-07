module Logics
(*
#r "System.Runtime.Serialization" // for interactive
#r "FSharp.PowerPack"
#r "System.Device"
*)
// Reference to assembly System.Runtime.Serialization and System.Xml

open System.IO
open System.Runtime.Serialization.Json
open System.Xml
open System.Text
open System
open System.Runtime.Serialization
open System.Net
open System.Device.Location
open System.Collections.Generic
    
let watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High)
watcher.MovementThreshold <- 20.0

let stringify (pos:GeoCoordinate) = 
    match pos.IsUnknown with
    | false ->
        let cult = System.Globalization.CultureInfo.InvariantCulture
        pos.Latitude.ToString("0.000000", cult), pos.Longitude.ToString("0.000000", cult)
    | true -> "", ""

let registerEvents() subscription = 
    watcher.StatusChanged 
    |> Event.filter(fun f -> f.Status = GeoPositionStatus.Ready || f.Status = GeoPositionStatus.Disabled)
    |> Observable.subscribe(fun c -> watcher.Stop())

let GPSFetch subscription =
    watcher.Start()
    let cult = System.Globalization.CultureInfo.InvariantCulture
    let pos = watcher.Position.Location |> stringify
    subscription(pos)

let locationtextUrl lat lon =  //let url = locationtextUrl "40.714224" "-73.961452";;
    new Uri("http://maps.googleapis.com/maps/api/geocode/json?latlng=" + lat + "," + lon + "&sensor=false", UriKind.Absolute)

[<DataContract>]
type public Result = public {
     [<field: DataMember(Name = "address_components")>] mutable comps : obj array
     [<field: DataMember(Name = "formatted_address")>] mutable address : string
     [<field: DataMember(Name = "geometry")>] mutable geo : obj
     [<field: DataMember(Name = "types")>] mutable types : obj array
     }

[<DataContract>]
type public GPSPositionResponse = public {
      [<field: DataMember(Name = "results")>] mutable results : Result array
     }

/// Object from Json 
let internal unjson<'t> (jsonString:string)  : 't =  
        use ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)) 
        let obj = (new DataContractJsonSerializer(typeof<'t>)).ReadObject(ms) 
        obj :?> 't

let mutable callresult:string option = None

let processed jsondata = 
    match jsondata with
    | None -> ""
    | Some(x) -> 
        let res = (unjson<GPSPositionResponse> x).results
                  |> Seq.map(fun o -> o.address)
        match Seq.length res with
        | x when x>0 -> Seq.nth 0 res
        | _ -> ""

open Microsoft.FSharp.Control.WebExtensions

let internal fetchAsync (url : Uri) trigger = 
        let req = WebRequest.CreateHttp url
        req.CookieContainer <- new CookieContainer()
        let asynccall =
            let request_CallBack (res:IAsyncResult) = 
                let reqq = res.AsyncState :?> HttpWebRequest
                try
                    let resp = reqq.EndGetResponse(res) :?> HttpWebResponse

                    use stream = resp.GetResponseStream()
                    use reader = new StreamReader(stream)
                    let rdata = reader.ReadToEnd()       
                    callresult <- Some(rdata)
                with
                    | _ -> callresult <- None
                trigger "" |> ignore
            req.BeginGetResponse(new AsyncCallback(request_CallBack), req);

        asynccall

// Cache the IAyncResult to give the same response to duplicate calls
let cache = new Dictionary<Uri, _>()
let fetchAsyncCached  (url : Uri) trigger = 
    let succ, v = cache.TryGetValue(url)
    if succ then 
        trigger "" |> ignore
        v 
    else
        let r = fetchAsync url trigger
        cache.Add(url, r)
        r

//fetchAsync (url) (fun a -> ());;
let locationName = processed callresult
//val locationName : string = "Grand St - Bedford Av, Brooklyn, NY 11211, USA"