#r "System.Globalization"
#r "System.Net.Primitives"
#r "packages\FSharp.Data\lib\portable-net45+netcore45\FSharp.Data.dll"

open FSharp.Data
open System.Net

let getTags (doc : HtmlDocument) (tag : string) =
    doc.Descendants tag

let inputTags doc =
    getTags doc "input"

let paragraphTags doc =
    getTags doc "p"

let isStolenCar plate =
    let cc = CookieContainer()

    let getInputTags (doc : HtmlDocument) =
        doc.Descendants "input"

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
        | Binary bin -> bin.Length.ToString()
    
    let searchUrl = 
        sprintf "http://www.crimnet.dcpc.interno.gov.it/servpub/ver2/SCAR/ricerca_targa.asp?NumeroTarga1=%s&NumeroTelaio1=%s&transport=%s" plate "" transport
    
    let response = Http.RequestString(searchUrl, httpMethod = "GET", headers = [ "Referer", "http://www.crimnet.dcpc.interno.gov.it/servpub/ver2/SCAR/cerca_targhe.asp" ], cookieContainer = cc)
    let paragraphs = 
        response
        |> HtmlDocument.Parse
        |> paragraphTags
        |> Seq.map (fun t -> t.InnerText())
    
    if Seq.length paragraphs > 10 then
        paragraphs |> Seq.item 11
    else
        paragraphs |> Seq.item 1

isStolenCar "AB074LS"

isStolenCar "BV479SB"