# Elmish router demo

This repository contains two single-page applications with exactly the same functionality: showing a counter (which can be decreased, increased or increased after a delay) or a text field (of which the content is shown below it). It was inspired by the content of [The Elmish Book](https://zaid-ajaj.github.io/the-elmish-book/#/chapters/scaling/splitting-programs). I've decided not to split modules into files not to overcomplicate things.

The focus of this simple application is to show the ability to react to the URL changes by modifying the application's state.
For example - when you run the application (run `npm install && npm start` in either of two directories), go to <http://localhost:8080/#/counter> and click "Show Text Input" button - the URL should change to <http://localhost:8080/#/text>.
Also - when you change `text` to `counter` in the URL manually - the application should go back to the previous screen.
This functionality makes it more user-friendly to reach the exact "sub-page" in your SPA that the user wants to reach (for instance when saving the link for later or when sharing it with other users).

## Routing

[The documentation](https://elmish.github.io/browser/index.html) doesn't make it obvious how exactly does the routing work. There are two modules of the library that routing depends on:

- [Elmish.Navigation](https://elmish.github.io/browser/navigation.html)
- [Elmish.UrlParser](https://elmish.github.io/browser/parser.html)

The [routing tutorial](https://elmish.github.io/browser/routing.html) goes into much details of how to parse routes without explaining the general idea behind it in the first place. To make your app "navigable" you need to change the chain of calls that lead to `Program.run`:

```f#
// Pseudocode

// You need to import Navigation in order to leverage routing as that's where `Program.toNavigable` is defined.
open Elmish.Navigation

let route =
    ...

let urlUpdate =
    ...

Program.mkProgram init update view
|> Program.toNavigable (parseHash route) urlUpdate  // <- That's the additional line.
|> Program.run
```

`Program.toNavigable` requires two functions as parameters:

- parser
- urlUpdate

Let's look at both of these in detail.

### Parser

The first parameter of `Program.toNavigable` is a `parser` function. In brief: **it's invoked when the page is initially loaded and when the URL changes**, and its responsibility is simply to map between URLs and application sub-pages (defined by the programmer).

The type of `parser` (defined in `Elmish.Navigation` module) is `Parser<'a>` where:

```f#
type Parser<'a> = Location -> 'a
```

You can think of `Location` as the URL in your browser. What's the `'a` type? This will be the `Optional` of the type that needs to be defined by the developer and which describes all possible routes that the application can take. Say I want to have an application which shows me a text input field when the user visits `/text` subpage, and shows me a counter when `/counter` is visited.

I'll define the following type:

```f#
type Route =
    | Counter
    | TextInput
```

Hence, the type of the first parameter to `Program.toNavigable` becomes of type `Parser<Option<Route>>` (which, as I've shown above, translates to `Location -> Option<Route>` - so what it's basically doing is translating the URL address in your browser to one of the values of your predefined type, or `None` if the mapping isn't possible).

To repeat what we just went throught: you can think of a `parser` as a function which takes a URL address and produces an optional single value representing the application route, defined by you!

How do you define function of type `Parser<Option<Route>>`, you might ask. This is where things get confusing, so bear with me.

The naïve implementation of a function having that type would look like something along these lines:

```f#
// Pseudocode

let urlUpdate =
    ...

let myParser (url:Location) : Option<Route> =
    // Extract the part of url you're interested in. In this case I'd extract whatever is after
    // the first "#" sign in the URL and return the Route based on this.
    let urlPart = ...
    match urlPart with
    | "text" -> Some Route.TextInput
    | "counter" -> Some Route.Counter
    | _ -> None

// Now we can feed our `myParser` function into the `toNavigable`:
Program.mkProgram init update view
|> Program.toNavigable myParser urlUpdate
|> Program.run
```

Extracting the `urlPart` bit might be cumbersome. Also - what happens when you have "non-hardcoded" parameters, such as an ID or an additional [query string](https://en.wikipedia.org/wiki/Query_string) after `?`.

That's where the [Elmish.UrlParser](https://elmish.github.io/browser/parser.html) module steps in.

First - it defines two "URL helper" functions which take a function (defined by you) and result in the function of type `Parser<'a>` (so the one which returns an optional `Route` based on the URL). As the ["routing tutorial"](https://elmish.github.io/browser/routing.html#Working-with-full-HTML5-and-hash-based-URLs) mentions, we've got [two functions](https://elmish.github.io/browser/parser.html#Parsers) to choose from:

- parsePath
- parseHash

They differ between which part of the URL is fed into your actual parser (remember the `urlPart` bit extracted in the pseudocode above?).

Looking at type definitions of these functions (using "Haskellish" type notation) we can see that they take a value of type `Parser<_,_>`, a `Location` and return an optional value:

```f#
parsePath :: Parser<('a -> 'a), 'a> -> Location -> Option<'a>
parseHash :: Parser<('a -> 'a), 'a> -> Location -> Option<'a>
```

But wait, `Parser` was a generic type with one type argument, and here we've got two! It turns out there's another `Parser` type defined in `Elmish.UrlParser`:

```f#
type Parser<'a,'b> = State<'a> -> State<'b> list
```

Where State is defined as:

```f#
type State<'v> =
  { visited : string list
    unvisited : string list
    args : Map<string,string>
    value : 'v }
```

Another functionality that `parser` module defines are "placeholder" functions which handle different URL parameters, both variable and constant:

Quoting the [documentation](https://elmish.github.io/browser/routing.html#Parsing-routes), we've got:

- `s` combinator for a static string we expect to find in the URL,
- `</>` combinator to capture a slash in the URL,
- `str` combinator to extract a string,
- `i32` combinator to attempt to parse an int.
- `top` combinator that takes nothing.

Looking at the example implementation of function that's supposed to be fed into one of the "URL helper" functions (I'll choose `parseHash` for now) will make it easier to understand what are functions mentioned above useful for:

```f#
let route =
    oneOf
        [ map Route.Counter (s "counter")
          map Route.TextInput (s "text") ]
```

Here I've just used `s` function which expects a constant string to be present in a given URL.

The type of `route` is (again, in "Haskellish" notation):

```haskell
route :: State<Route -> Route> -> State<Route> list
```

`route` is [point-free](https://docs.microsoft.com/en-us/dotnet/fsharp/style-guide/conventions#partial-application-and-point-free-programming). Written out with the argument explicitly mentioned, that'd be:

```f#
let route (state: State<Route -> Route>): State<Route> list =
    oneOf
        [ map Route.Counter (s "counter")
          map Route.TextInput (s "text") ] state
```

After we feed it into `parseHash` (which, again - has type `Parser<_,_> -> Location -> Optional<_>`, and to be more specific, in this case: `Parser<(Route -> Route),Route> -> Location -> Optional<Route>`), we get:

```haskell
parseHash route :: Location -> Optional<Route>
```

Which makes sense, since as we've initially found out - the first parameter of `Program.toNavigable` is supposed to "map" the Location (URL) into an optional Route.

#### Difference between parseHash and parsePath

Let's take a look at how an example [URL](https://developer.mozilla.org/en-US/docs/Learn/Common_questions/What_is_a_URL) is built:

```
http://www.example.com:80/path/to/myfile.html?key1=value1&key2=value2#SomewhereInTheDocument
```

We've got a protocol (`http`), the domain name (`www.example.com`), port (`80`), path (`/path/to/myfile.html`), parameters (`key1=value1&key2=value2`) and an anchor (`#SomewhereInTheDocument`).

For [single page applications](https://en.wikipedia.org/wiki/Single-page_application) it's quite frequent to leverage the anchor for routing between "sub-pages" of your single-page application. It's helpful for "jumping" directly into the sub-page when you open the app in your browser (for instance when someone else shared a link to the sub-page with you). It also makes your page "navigable" in terms of the browser history (you'd expect to see the previous screen when hitting the "back" button).

`parseHash` feeds the content from after the anchor to your `route` function. Say you browse to `http://localhost:8080/#/counter`. `parseHash` will pass the `counter` bit to the `route` function.

`parsePath` on the other hand uses the path bit of the URL. Say you browse to `http://localhost:8080/counter`. `parseHash` will pass the `counter` bit to the `route` function.

### Url Update

We've covered `parser` function, which is responsible for mapping between URL and the type describing all possible pages of the application that you've defined (`Route` in case of previously shown examples). But all it does is just maps - it doesn't actually affect the state of our application. To tell the application to change its state based on the output of the `parser` function - you'll have to use `urlUpdate`. It's always invoked after `parser` and the output of `parser` is fed into the `urlUpdate` by the `elmish-router` library.

## Forms of pages composition

### State-field composition

You're using [state-field composition](https://zaid-ajaj.github.io/the-elmish-book/#/chapters/scaling/composition-forms#state-field-composition) when you're holding multiple initialised models of different pages at the same time. In case of the app present in this repository, we're storing both `CounterState` and `TextState` at once:

```f#
type Page =
    | Counter
    | TextInput

type State =
    { CounterState: Counter.State
      TextState: InputText.State
      CurrentPage: Page }
```

It works only in simple scenarios where pages don't have "sequential dependency" on each other. We'll run into a problem when one state depends on some action (perhaps user input) on another page. We could use `option` but [illegal states should be unrepresentable](https://tarmil.fr/article/2019/12/17/elmish-page-model#page-model-in-the-main-model).

[Here's](state-field-composition/README.md) the explanation of how routing works in this case.

### Discriminated union composition

[Here's](discriminated-union-composition/README.md) the explanation of how routing works in this case.
