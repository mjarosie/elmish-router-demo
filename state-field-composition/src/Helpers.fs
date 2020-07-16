module Helpers

open Feliz
open Elmish

let divider = Html.div [ prop.style [ style.margin 10 ] ]

module Cmd =
    let fromAsync (operation: Async<'msg>): Cmd<'msg> =
        let delayedCmd (dispatch: 'msg -> unit) : unit =
            let delayedDispatch = async {
                let! msg = operation
                dispatch msg
            }

            Async.StartImmediate delayedDispatch
        
        Cmd.ofSub delayedCmd