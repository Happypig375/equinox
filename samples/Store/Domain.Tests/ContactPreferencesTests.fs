﻿module Samples.Store.Domain.Tests.ContactPreferencesTests

open Domain.ContactPreferences
open Domain.ContactPreferences.Events
open Domain.ContactPreferences.Fold
open Swensen.Unquote

/// Put the aggregate into the state where the command should trigger an event; verify correct events are yielded
let verifyCorrectEventGenerationWhenAppropriate variant command (originState: State) =
    let initialEvents =
        match command, variant with
        // Variant 1: Initial state
        | Update _, Choice1Of3 () -> []
        // Variant 2: Same state
        | Update value, Choice2Of3 () -> [Updated value]
        // Variant 2: Force something to change
        | Update ({ preferences = { quickSurveys = qs } as preferences } as value), Choice3Of3 () ->
            [Updated { value with preferences = { preferences with quickSurveys = not qs}}]
    let state = fold originState initialEvents
    let events = interpret command state
    let state' = fold state events

    match command, events with
    | Update cValue, [| Updated eValue |] ->
        test <@ eValue.preferences = cValue.preferences
                && cValue.preferences = state' @>
    | Update cValue, [||] ->
        test <@ state = cValue.preferences
                && state' = state @>
    | c, e -> failwith $"Invalid result - Command %A{c} yielded Events %A{e} in State %A{state}"

/// Processing should allow for any given Command to be retried at will
let verifyIdempotency (cmd: Command) (originState: State) =
    // Put the aggregate into the state where the command should not trigger an event
    let establish: Event list = cmd |> function
        | Update value ->
            [ Updated value]
    let state = fold originState establish
    let events = interpret cmd state
    // Assert we decided nothing needs to happen
    test <@ Array.isEmpty events @>

[<DomainProperty(MaxTest = 1000)>]
let ``interpret yields correct events, idempotently`` variant (command: Command) (originState: State) =
    verifyCorrectEventGenerationWhenAppropriate variant command originState
    verifyIdempotency command originState
