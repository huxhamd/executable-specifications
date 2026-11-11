# GAP ANALYSIS

## Did you mention that LMAX or Farley have an open source DSL library?

It wasn’t this session; I haven’t mentioned one here. But there are two public repos, and one of them bears directly on what we’ve been discussing.

**LMAX Simple-DSL** ([LMAX-Exchange/Simple-DSL](https://github.com/LMAX-Exchange/Simple-DSL)) is a Java library under Apache 2.0, last updated in 2023. The library itself is small. It parses and validates the `"name: value"` string arguments you see in Farley’s slides, such as `placeOrder("side: buy", "quantity: 10")`, and nothing more. The useful part is its [wiki](https://github.com/LMAX-Exchange/Simple-DSL/wiki), which describes how LMAX built the layers around it. A lot of it will look familiar from your design:

- A `DslTestCase` base class exposes one field per gateway: `publicAPI`, `adminAPI`, `registrationAPI`, `tradingUI`.
- A shared `TestContext` holds aliases, much like your `SpecContext`.
- Expected failures are passed to the DSL as a parameter (`"expect: invalid username"`) rather than as separate methods like your `AttemptTo…`.

The wiki also has a published example of a confirmation through a different channel from the one that acted:

``` java
adminUI.activityLog.waitForActivityEvent("message: Created new user: <user>", "user: Bob");
```

The user is created through `registrationAPI`, and the outcome, an audit-log entry, is confirmed through `adminUI`, which is a different gateway. It’s the same kind of case the new `CLAUDE.md` rule allows: an outcome the acting channel’s users don’t see. It’s also the audit-trail example I suggested yesterday for a first pinned `Confirm…`. One caveat: this is LMAX’s documentation rather than something Farley wrote himself, although it’s the system he describes in his talks.

**Farley’s own sample code** ([davef77/acceptance-testing](https://github.com/davef77/acceptance-testing)) is a small Java bookshop example from 2016–17. It shows the four layers and a `@Channel` annotation, but it’s thin. The driver just uses the first channel listed, and it has nothing about arranging through one channel and testing through another. Your implementation is well beyond it.

Neither is worth adopting in C#. The parameter-string style mainly makes up for Java lacking named arguments. But the Simple-DSL wiki is worth reading for patterns, and it’s free.

Sources:

- [LMAX-Exchange/Simple-DSL](https://github.com/LMAX-Exchange/Simple-DSL)
- [Simple-DSL wiki](https://github.com/LMAX-Exchange/Simple-DSL/wiki)
- [davef77/acceptance-testing](https://github.com/davef77/acceptance-testing)
- [LMAX technology blog: testing series](https://technology.lmax.com/series/testinglmax/)


## Do either projects highlight any gaps in my design?

Yes, a few. Your patch has landed on `template` (0cc4bf2), so I compared the wiki and Farley’s sample against that. Most of what they describe you already have: aliases, a shared context, lazy channels, per-spec isolation, and grouping specs by functional area. The gaps are in the order I’d expect you to hit them.

**Likely to matter soon**

1. **No way to confirm something is absent.** Your drivers wait for a yes, and the wiki’s audit example checks both ways: `ensureEntryExists` and `ensureEntryDoesNotExist`. Your first spec for deleting a todo, or hiding completed ones, will need `ConfirmNotShown`. Negating `IsShown` is wrong both ways: it takes the full timeout when the todo is absent, and it can pass too early while the page is still loading. The driver contract needs its own “wait until gone”. Your `UiTodoDriver` comment already points at this, but the contract and DSL have no method for it yet.
2. **Specs can’t control the exact text the system sees.** Every title gets `_abc123` appended. So a spec for “titles are trimmed” can’t be written: `"  Buy milk  "` becomes `"  Buy milk  _abc123"`, and the trailing spaces are no longer trailing. The same goes for case and for special characters at the end of a title. `OfLength` solves one case with its own method. LMAX has a general escape: wrapping a value in `<angle brackets>` bypasses aliasing. The C# version could be a DSL method or parameter meaning “use this title exactly”.
3. **Error messages that echo a value.** `ConfirmErrorIsShown` uses `Assert.Equal`. The first rejection that quotes a title, such as “‘Buy milk’ already exists”, will contain the aliased title and fail. LMAX’s fix is to put placeholders in the expected message and resolve them through the aliases: `"message: Created new user: <user>", "user: Bob"`.
4. **Waiting on the API side.** The wiki’s rule is that every wait lives in the DSL or driver and has a timeout, including implicit ones. Now that pinned confirmations are allowed, `ApiTodoDriver.IsShown`/`IsComplete` should poll until true, as the UI driver does. This is the same point as before, but it’s live now.

## When the app grows

5. **A todo’s title is also its identity.** `Find` takes the first todo with a matching title, and the UI locator would match several. Duplicate titles or renaming a todo would break. LMAX’s RememberAs pattern separates the handle from the values (`"order: order1"`, then `cancelOrder("order1")`). You’d only need it when a spec makes the title ambiguous.
6. **There’s no notion of who is acting.** Todos are global. Once there are users, `Api.` has to arrange as the same user `Sut.`’s browser is signed in as, so the user belongs in `SpecContext`. Specs like “Alice can’t see Bob’s todos” also need two actors on the channel under test in one spec. LMAX’s answer was `registrationAPI.createUser("Bob")` followed by `login("Bob")` on each channel, plus a blunt `publicAPI2`.
7. **Controlling time.** LMAX has a “time machine” (a controllable clock) for anything date-based. It isn’t needed until todos have due dates.

## A process question rather than a code gap

8. **Specs that are committed but not yet implemented.** Farley wrote that his team used a custom annotation to mark acceptance tests as work in progress, “so that we can track test that are present but not being run”. LMAX’s analysts mark them `@Ignored` until a developer picks them up. In your flow, a committed failing spec turns `check-done` red until the app-agent finishes it. That’s fine with one spec in flight, but not with several queued, or with parallel worktrees on `practice`. A `[Fact, Pending]` attribute could make `check-done` report those specs without failing on them. Is that a case you expect to hit?

Neither project has anything you’re missing on channels. Farley’s sample just takes the first channel listed. On some points you’ve gone further. Typed methods are safer to refactor than LMAX’s string parameters, which their wiki admits are hard to rename. `AttemptTo…` plus `ConfirmErrorIsShown` reads better than an `"expect:"` flag. `ArrangementFailedException` and `[Channels]` reporting a skip rather than dropping the spec have no equivalent in either project.

Sources:

[Simple-DSL wiki: Patterns](https://github.com/LMAX-Exchange/Simple-DSL/wiki/Patterns)
[Simple-DSL wiki: Miscellaneous notes](https://github.com/LMAX-Exchange/Simple-DSL/wiki/MiscNotes)
[Simple-DSL wiki: Showcase](https://github.com/LMAX-Exchange/Simple-DSL/wiki/Showcase)
[Simple-DSL wiki: Layers](https://github.com/LMAX-Exchange/Simple-DSL/wiki/Layers)
[davef77/acceptance-testing](https://github.com/davef77/acceptance-testing)
[Dave Farley on the Continuous Delivery Google group](https://groups.google.com/g/continuousdelivery/c/ajrKzJU5DgA)