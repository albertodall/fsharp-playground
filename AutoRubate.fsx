#r "System.Globalization"
#r "System.Net.Primitives"
#r "packages\FSharp.Data\lib\portable-net45+netcore45\FSharp.Data.dll"

open System
open System.Net
open System.Text.RegularExpressions
open FSharp.Data

type StolenCarData = {
    When : DateTime
    Where : string
}

type StolenCarCheckResult =
    | NoStealData of string
    | StolenCar of StolenCarData

let (|MatchNotStolenMessage|_|) msg =
    let pattern = Regex("non risulta")
    let matches = pattern.Match msg
    Some matches.Success

let (|MatchStolenMessage|_|) msg =
    let pattern = Regex("data  (\d+\/\d+\/\d+) presso  (.*)$")
    let matches = pattern.Match msg
    if matches.Success then
        Some { When = DateTime.Parse matches.Groups.[0].Value; Where = matches.Groups.[1].Value }
    else
        None
        
let getTags (doc : HtmlDocument) (tag : string) =
    doc.Descendants tag

let getInputTags doc =
    getTags doc "input"

let getParagraphTags doc =
    getTags doc "p"
let parseHtmlNode (node : HtmlNode) =
    { Where = node.InnerText(); When = DateTime.Today }

let searchStolenCarByPlate plate =
    let cc = CookieContainer()
    let response = Http.Request("http://www.crimnet.dcpc.interno.gov.it/servpub/ver2/SCAR/cerca_targhe.asp", cookieContainer = cc)
    let transport = 
        match response.Body with
        | Text text ->
            text
            |> HtmlDocument.Parse
            |> getInputTags
            |> Seq.filter (fun node -> node.AttributeValue("id") = "transport")
            |> Seq.head
            |> HtmlNode.attributeValue("value")
        | Binary bin -> 
            bin.Length.ToString()
    
    let searchUrl = 
        sprintf "http://www.crimnet.dcpc.interno.gov.it/servpub/ver2/SCAR/ricerca_targa.asp?NumeroTarga1=%s&NumeroTelaio1=%s&transport=%s" plate "" transport
    
    let response = 
        Http.RequestString(
            searchUrl, 
            httpMethod = "GET", 
            headers = [ "Referer", "http://www.crimnet.dcpc.interno.gov.it/servpub/ver2/SCAR/cerca_targhe.asp" ], 
            cookieContainer = cc)

    let paragraphs = 
        response
        |> HtmlDocument.Parse
        |> getParagraphTags

    let result =
        match Seq.length paragraphs with
        | l when l > 10 -> 
            paragraphs |> Seq.item 11 |> parseHtmlNode |> StolenCar
        | _ ->
            paragraphs |> Seq.item 1 |> HtmlNode.innerText |> NoStealData

    result

// Examples:
// searchStolenCarByPlate "AB074LS"
// searchStolenCarByPlate "BV479SB"