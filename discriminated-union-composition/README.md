# Discriminated union composition

In case of discriminated union composition the app model holds only the model of a current page:

```f#
type Page =
    | Counter of Counter.State
    | TextInput of InputText.State

type State = { CurrentPage: Page }
```

The moment we switch between pages (when State.CurrentPage changes) - we're losing the internal state of the page that we just left. Note that we need a separate type for defining the URL browser route (as it doesn't correspond 1-to-1 with `Page` type as in [state-field composition](../state-field-composition/README.md)):

```f#
type Route =
    | Counter
    | TextInput
```