﻿namespace Flips.SliceMap

open System.Collections.Generic


type SliceType<'a when 'a : comparison> =
    | All
    | Equals of 'a
    | GreaterThan of 'a
    | GreaterOrEqual of 'a
    | LessThan of 'a
    | LessOrEqual of 'a
    | Between of 'a * 'a
    | In of Set<'a>
    | NotIn of Set<'a>
    | Where of ('a -> bool)


module internal Utilities =

    //let inline internal mergeAddition (lhs:Map<_,_>) (rhs:Map<_,_>) =
    //    /// The assumption is that the LHS Map has more entries than the RHS Map
    //    let newRhsValues = rhs |> Map.filter (fun k _ -> not (lhs.ContainsKey k)) |> Map.toSeq

    //    lhs
    //    |> Map.map (fun k lhsV -> match Map.tryFind k rhs with 
    //                              | Some rhsV -> lhsV + rhsV 
    //                              | None -> lhsV)
    //    |> fun newLhs -> Seq.fold (fun m (k, v) -> Map.add k v m) newLhs newRhsValues


    let inline internal filterKeys (f:SliceType<'a>) (keys:SliceSet<'a>) : SliceSet<'a> =
        match f with
        | All -> keys
        | Equals k -> match keys.Contains k with | true -> SliceSet [k] | false -> SliceSet []
        | GreaterThan k -> keys.GreaterThan k
        | GreaterOrEqual k -> keys.GreaterOrEqual k
        | LessThan k -> keys.LessThan k
        | LessOrEqual k -> keys.LessOrEqual k
        | Between (lowerBound, upperBound) -> keys.Between lowerBound upperBound
        | In set -> keys.Intersect (SliceSet set)
        | NotIn set -> keys.Minus (SliceSet set)
        | Where f -> keys.Filter f

    let inline sum< ^a, ^b when ^a: (static member Sum: ^a -> ^b)> (k1: ^a) = 
        ((^a) : (static member Sum: ^a -> ^b) k1)

    let inline sumAll< ^a, ^b when ^a: (static member Sum: ^a -> ^b) 
                              and ^a: (static member (+): ^a * ^a -> ^a)
                              and ^a: (static member Zero: ^a)> (k1: ^a seq) = 
        let r = Seq.sum k1
        ((^a) : (static member Sum: ^a -> ^b) r)

type TryFind<'Key, 'Value> = 'Key -> 'Value option

module TryFind =

    let ofDictionary (d:Dictionary<'Key,'Value>) : TryFind<'Key, 'Value> =
        fun k -> 
          match d.TryGetValue(k) with
          | true, value -> Some value
          | false, _ -> None

    let ofSeq (s:seq<'Key * 'Value>) : TryFind<'Key, 'Value> =
        s 
        |> Seq.map (fun (k, v) -> KeyValuePair(k, v)) 
        |> Dictionary
        |> ofDictionary

    let toSeq (keys:seq<_>) (s:TryFind<_,_>) =
        let lookup k = s k |> Option.map (fun v -> k, v)
        
        keys
        |> Seq.choose lookup

    let toMap keys (s:TryFind<_,_>) =
        s |> (toSeq keys) |> Map.ofSeq


    let inline scale coef keys (tryFind:TryFind<_,_>) =
        let newDict = new Dictionary<_,_>()
      
        for k in keys do
            match tryFind k with
            | Some v ->
                newDict.[k] <- v * coef
            | None -> ()

        ofDictionary newDict


    let inline mergeAdd (keys:seq<_>) (aTryFind:TryFind<_,_>) (bTryFind:TryFind<_,_>) =
        let newDict = new Dictionary<_,_>()

        for key in keys do
            match aTryFind key, bTryFind key with
            | Some lValue, Some rValue -> 
                newDict.Add(key, lValue + rValue)
            | Some lValue, None -> 
                newDict.Add(key, lValue)
            | None, Some rValue ->
                newDict.Add(key, rValue)
            | None, None->
                ()

        ofDictionary newDict


    let inline multiply (keys:seq<_>) (aTryFind:TryFind<_,_>) keyBuilder (bTryFind:TryFind<_,_>) =
        let newDict = new Dictionary<_,_>()

        for key in keys do
            match aTryFind key, bTryFind (keyBuilder key) with
            | Some lValue, Some rValue -> 
                newDict.Add(key, lValue * rValue)
            | _, _ ->
                ()

        ofDictionary newDict


    let inline sum keys (tryFind:TryFind<_,_>) =
        let mutable acc = LanguagePrimitives.GenericZero

        for k in keys do
            match tryFind k with
            | Some v -> 
                acc <- acc + v
            | None -> ()

        acc

    let sumLinearExpressions keys (tryFind:TryFind<_,Flips.Types.LinearExpression>) =
        let mutable acc = LanguagePrimitives.GenericZero
        
        for k in keys do
            match tryFind k with
            | Some v ->
                acc <- acc + (1.0 * v)
            | None -> ()

        acc

    let sumDecisions keys (tryFind:TryFind<_,Flips.Types.Decision>) =
        let mutable acc = LanguagePrimitives.GenericZero
        
        for k in keys do
            match tryFind k with
            | Some v ->
                acc <- acc + (1.0 * v)
            | None -> ()

        acc


    let sumDecisionsWithUnits keys (tryFind:TryFind<_,Flips.UnitsOfMeasure.Types.Decision<_>>) =
        let mutable acc = LanguagePrimitives.GenericZero
      
        for k in keys do
            match tryFind k with
            | Some v ->
                acc <- acc + (1.0 * v)
            | None -> ()

        acc











