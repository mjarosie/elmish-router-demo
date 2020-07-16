module Main

open Elmish
open Elmish.React
open Elmish.UrlParser
open Elmish.Navigation
open Feliz
open Helpers

module Counter =

    type State = {
        Count: int
    }

    type Msg = 
    | Increment
    | Decrement
    | IncrementDelayed

    let init() = {
        Count = 0
    }

    let update (msg: Msg) (state: State): State * Cmd<Msg> =
        match msg with
        | Increment -> { state with Count = state.Count + 1 }, Cmd.none
        | Decrement -> { state with Count = state.Count - 1 }, Cmd.none
        | IncrementDelayed -> 
          let delayedIncrement = async {
            do! Async.Sleep 1000
            return Increment
          }

          state, Cmd.fromAsync delayedIncrement

    let render (state: State) (dispatch: Msg -> unit) =
        Html.div [
          prop.children [
            Html.button [
              prop.onClick (fun _ -> Increment |> dispatch)
              prop.text "Increment"
            ]

            Html.button [
              prop.onClick (fun _ -> Decrement |> dispatch)
              prop.text "Decrement"
            ]

            Html.button [
              prop.onClick (fun _ -> IncrementDelayed |> dispatch)
              prop.text "Increment Delayed"
            ]

            Html.h1 state.Count
          ]
        ]

module InputText =

    type State = {
        InputText: string
        IsUpperCase: bool
    }

    type Msg =
        | InputTextChanged of string
        | UppercaseToggled of bool

    let init() = {
        InputText = ""
        IsUpperCase = false
    }

    let update (msg: Msg) (state: State): State =
        match msg with
        | InputTextChanged text -> { state with InputText = text }
        | UppercaseToggled upperCase -> { state with IsUpperCase = upperCase }

    let render (state: State) (dispatch: Msg -> unit) =
        Html.div [
          prop.children [
              Html.input [
                prop.valueOrDefault state.InputText
                prop.onChange (InputTextChanged >> dispatch)
              ]

              divider

              Html.input [
                prop.id "uppercase-checkbox"
                prop.type'.checkbox
                prop.isChecked state.IsUpperCase
                prop.onChange (UppercaseToggled >> dispatch)
              ]

              Html.label [
                prop.htmlFor "uppercase-checkbox"
                prop.text "Uppercase"
              ]

              Html.h3 (if state.IsUpperCase then state.InputText.ToUpper() else state.InputText)
          ]
        ]

module App =
    type Page =
        | Counter
        | TextInput

    type State =
        { CounterState: Counter.State
          TextState: InputText.State
          CurrentPage: Page }

    type Msg =
        | CounterMsg of Counter.Msg
        | InputTextMsg of InputText.Msg
        | SwitchPage of Page

    let init (initialRoute: Option<Page>): State * Cmd<Msg> =
        { CounterState = Counter.init ()
          TextState = InputText.init ()
          CurrentPage = initialRoute |> Option.defaultValue Page.Counter },
        Cmd.none

    let toHash (page: Page) =
        match page with
        | Page.Counter -> "#/counter"
        | Page.TextInput -> "#/text"

    let update (msg: Msg) (state: State): State * Cmd<Msg> =
        match msg with
        | CounterMsg msg ->
            let counterUpdatedState, counterUpdatedCommand = Counter.update msg state.CounterState
            { state with
                  CounterState = counterUpdatedState },
            Cmd.map CounterMsg counterUpdatedCommand
        | InputTextMsg msg ->
            { state with
                  TextState = InputText.update msg state.TextState },
            Cmd.none
        | SwitchPage page -> { state with CurrentPage = page }, Navigation.modifyUrl (toHash page)

    let render (state: State) (dispatch: Msg -> unit) =
        match state.CurrentPage with
        | Counter ->
            Html.div
                [ Html.button
                    [ prop.text "Show Text Input"
                      prop.onClick (fun _ -> dispatch (SwitchPage TextInput)) ]

                  divider

                  Counter.render state.CounterState (CounterMsg >> dispatch) ]

        | TextInput ->
            Html.div
                [ Html.button
                    [ prop.text "Show counter"
                      prop.onClick (fun _ -> dispatch (SwitchPage Counter)) ]

                  divider
                  InputText.render state.TextState (InputTextMsg >> dispatch) ]


let route (state: State<App.Page -> App.Page>) =
    oneOf
        [ map App.Page.Counter (s "counter")
          map App.Page.TextInput (s "text") ] state

let urlUpdate (result:Option<App.Page>) (state: App.State): App.State * Cmd<App.Msg> =
  match result with
  | Some App.Page.Counter ->
      { state with CurrentPage = App.Page.Counter }, Cmd.none
  | Some App.Page.TextInput ->
      { state with CurrentPage = App.Page.TextInput }, Cmd.none
  | None ->
      state, Navigation.modifyUrl "#"

Program.mkProgram App.init App.update App.render
|> Program.withReactSynchronous "elmish-app"
|> Program.toNavigable (parseHash route) urlUpdate
|> Program.run