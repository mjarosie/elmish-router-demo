module Main

open Browser.Types
open Elmish
open Elmish.React
open Elmish.UrlParser
open Elmish.Navigation
open Feliz
open Helpers

module Counter =

    type State = { Count: int }

    type Msg =
        | Increment
        | Decrement
        | IncrementDelayed

    let init () = { Count = 0 }

    let update (msg: Msg) (state: State): State * Cmd<Msg> =
        match msg with
        | Increment -> { state with Count = state.Count + 1 }, Cmd.none
        | Decrement -> { state with Count = state.Count - 1 }, Cmd.none
        | IncrementDelayed ->
            let delayedIncrement =
                async {
                    do! Async.Sleep 1000
                    return Increment
                }

            state, Cmd.fromAsync delayedIncrement

    let render (state: State) (dispatch: Msg -> unit) =
        Html.div
            [ prop.children
                [ Html.button
                    [ prop.onClick (fun _ -> Increment |> dispatch)
                      prop.text "Increment" ]

                  Html.button
                      [ prop.onClick (fun _ -> Decrement |> dispatch)
                        prop.text "Decrement" ]

                  Html.button
                      [ prop.onClick (fun _ -> IncrementDelayed |> dispatch)
                        prop.text "Increment Delayed" ]

                  Html.h1 state.Count ] ]

module InputText =

    type State =
        { InputText: string
          IsUpperCase: bool }

    type Msg =
        | InputTextChanged of string
        | UppercaseToggled of bool

    let init () = { InputText = ""; IsUpperCase = false }

    let update (msg: Msg) (state: State): State =
        match msg with
        | InputTextChanged text -> { state with InputText = text }
        | UppercaseToggled upperCase -> { state with IsUpperCase = upperCase }

    let render (state: State) (dispatch: Msg -> unit) =
        Html.div
            [ prop.children
                [ Html.input
                    [ prop.valueOrDefault state.InputText
                      prop.onChange (InputTextChanged >> dispatch) ]

                  divider

                  Html.input
                      [ prop.id "uppercase-checkbox"
                        prop.type'.checkbox
                        prop.isChecked state.IsUpperCase
                        prop.onChange (UppercaseToggled >> dispatch) ]

                  Html.label
                      [ prop.htmlFor "uppercase-checkbox"
                        prop.text "Uppercase" ]

                  Html.h3 (if state.IsUpperCase then state.InputText.ToUpper() else state.InputText) ] ]


module App =

    [<RequireQualifiedAccess>]
    type Page =
        | Counter of Counter.State
        | TextInput of InputText.State

    [<RequireQualifiedAccess>]
    type Route =
        | Counter
        | TextInput

    type State = { CurrentPage: Page }

    type Msg =
        | CounterMsg of Counter.Msg
        | InputTextMsg of InputText.Msg
        | SwitchToCounter
        | SwitchToTextInput

    let toHash (route: Route) =
        match route with
        | Route.Counter -> "#/counter"
        | Route.TextInput -> "#/text"

    let init (initialRoute: Option<Route>): State * Cmd<Msg> =
        match initialRoute with
        | Some Route.Counter ->
            { CurrentPage = Page.Counter(Counter.init ()) }, Navigation.modifyUrl (toHash Route.Counter)
        | Some Route.TextInput ->
            { CurrentPage = Page.TextInput(InputText.init ()) }, Navigation.modifyUrl (toHash Route.TextInput)
        | None -> { CurrentPage = Page.Counter(Counter.init ()) }, Navigation.modifyUrl (toHash Route.Counter)

    let update (msg: Msg) (state: State): State * Cmd<Msg> =
        match state.CurrentPage, msg with
        | Page.Counter counterState, CounterMsg msg ->
            let counterUpdatedState, counterUpdatedCommand = Counter.update msg counterState
            { state with
                  CurrentPage = Page.Counter counterUpdatedState },
            Cmd.map CounterMsg counterUpdatedCommand
        | Page.TextInput textState, InputTextMsg msg ->
            { state with
                  CurrentPage = Page.TextInput(InputText.update msg textState) },
            Cmd.none
        | _, SwitchToCounter ->
            let counterState = Counter.init ()
            { state with
                  CurrentPage = Page.Counter counterState },
            Navigation.modifyUrl (toHash Route.Counter)
        | _, SwitchToTextInput ->
            let textInputState = InputText.init ()
            { state with
                  CurrentPage = Page.TextInput textInputState },
            Navigation.modifyUrl (toHash Route.TextInput)
        | _, _ -> state, Cmd.none

    let render (state: State) (dispatch: Msg -> unit) =
        match state.CurrentPage with
        | Page.Counter counterState ->
            Html.div
                [ Html.button
                    [ prop.text "Show Text Input"
                      prop.onClick (fun _ -> dispatch (SwitchToTextInput)) ]

                  divider

                  Html.p [ prop.text ("state: " + string state) ]

                  Counter.render counterState (CounterMsg >> dispatch) ]

        | Page.TextInput textInputState ->
            Html.div
                [ Html.button
                    [ prop.text "Show counter"
                      prop.onClick (fun _ -> dispatch (SwitchToCounter)) ]

                  divider

                  Html.p [ prop.text ("state: " + string state) ]

                  InputText.render textInputState (InputTextMsg >> dispatch) ]

    let route (state: State<Route -> Route>): State<Route> list =
        printf "route: %A" state

        let result =
            oneOf
                [ map Route.Counter (s "counter")
                  map Route.TextInput (s "text") ] state

        printf "route result: %A" result
        result

    // let urlUpdate (result: Option<Route>) (state: State): State * Cmd<Msg> =
    //     printf "urlUpdate: %A" result
    //     match result with
    //     | Some Route.Counter ->
    //         state, Cmd.ofMsg SwitchToCounter
    //     | Some Route.TextInput ->
    //         state, Cmd.ofMsg SwitchToTextInput
    //     | None ->
    //         state, Cmd.ofMsg SwitchToCounter // Default

    let urlUpdate (result: Option<Route>) (state: State): State * Cmd<Msg> =
        printf "urlUpdate: %A" result
        match result with
        | Some Route.Counter -> state, Cmd.none
        | Some Route.TextInput -> state, Cmd.none
        | None -> state, Cmd.none

Program.mkProgram App.init App.update App.render
|> Program.withReactSynchronous "elmish-app"
|> Program.toNavigable (parsePath App.route) App.urlUpdate
|> Program.run
