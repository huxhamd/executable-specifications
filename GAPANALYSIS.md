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

## Does Farley's named-parameter style matter in C#?

Mechanically, the `"name: value"` strings make up for Java having no named or optional parameters. But the style carries reasons that are about test design, not syntax:

- A spec states only what is essential to its behaviour. Everything else gets a default in the DSL.
- Specs speak in strings in the language of the domain and never use the system's own types. That keeps the specs decoupled from the system.
- Aliasing happens in the DSL, so specs are isolated from each other without saying so.
- New optional parameters with defaults don't break existing specs.

All of these hold in this project without strings:

- **Decoupling** is enforced by the project references. `acceptance.Specs` references only `acceptance.Dsl`, the drivers are `internal`, and nothing in the harness references `Todo.Api`.
- **Isolation** comes from `Alias`, applied in `TodosDsl` and shared between `Sut.` and `Api.` through `SpecContext`. Each DSL method has to remember to apply it. A method that forgets still passes until two parallel runs collide, so new DSL methods need checking for this.
- **Defaults** aren't needed yet, because a todo has only a title. When it gains more, use optional parameters with named arguments at the call site, such as `Add("Pay rent", due: "yesterday")`. Declare the defaults on the DSL interfaces, such as `ITodosSutDsl`. Specs call through the interfaces, and C# ignores defaults declared only on the class. Keep the parameters in domain language, such as `due: "yesterday"` rather than a `DateTime`, so the DSL does the translating.

So the style doesn't need copying. Its reasons are already in the design.


## Revisiting the gaps (11 November 2026)

Two things have changed since the answers above. This section redefines the open points rather than editing them, so the history stays.

**What counts as a gap.** A gap is a common scenario the harness can't express because the design hasn't considered it. A scenario the harness handles differently from LMAX or Farley's sample is not a gap.

**Where solutions come from.** Solutions are worked out from the principles of Farley's approach, not borrowed from existing implementations. Simple-DSL and Farley's sample are reference material only. `CLAUDE.md` now says to work this way. Two recommendations above were borrowed from LMAX and are withdrawn: the angle-bracket escape in point 2 and the message placeholders in point 3.

### Closed: not gaps

**2. Specs can't control the exact text the system sees.** Turning aliasing off for a value, as LMAX's angle brackets do, breaks isolation: two parallel runs of a trimming spec would both add `"  Buy milk  "`. It also puts a harness mechanism into the spec. Instead, the spec names the behaviour that matters, and the DSL builds a value that shows it while staying aliased. A trimming spec would say `AddWithSurroundingSpaces("Buy milk")`, and the DSL would send `"  Buy milk_abc123  "`. `AttemptToAddWithTitleOfLength` already follows this pattern, so it serves as the example.

**4. Waiting on the API side.** The API keeps todos in memory and answers synchronously, so a read straight after a write always sees it. Waiting would only matter if the system became eventually consistent, and that change would stay inside `ApiTodoDriver`.

**5. A todo's title is also its identity.** Whether two todos can share a title is a product decision, not a harness one. If they can, the spec says how a user tells them apart, such as "the second 'Buy milk'", and a DSL method expresses that. Both todos carry the same alias, so isolation still holds.

### Not a design gap, but needs an example

**3. Error messages that echo a value.** Placeholders in the expected message put a mechanism into the spec, and the user never sees `<title>`. The spec should state the message as the user reads it:

``` csharp
await Sut.Todos.ConfirmErrorIsShown("'Buy milk' is already complete");
```

The DSL translates in the other direction: `ConfirmErrorIsShown` removes this spec's alias suffix from the system's message before comparing. Nothing in the design prevents this, but no current spec shows it. Because the project is a store of examples, the next step is a spec that needs it, such as completing a todo that's already complete. This stays open in `TODO.md` until that spec exists.

### Still open: real gaps

**1. Confirming that something is absent.** No spec can yet say correctly that a todo is no longer shown. Negating `ConfirmShown` waits the full timeout when the todo is gone, and can pass too early while the page is loading. The driver contract needs its own "wait until gone" query, with a `ConfirmNotShown` in the DSL. The example spec would be deleting a todo.

**6. More than one actor.** The harness has no notion of who is acting. `Sut.`'s browser is a single anonymous session, and `Api.` arranges with no identity. Once there are users, the acting user belongs in `SpecContext`, and `Api.` must arrange as the same user `Sut.` is signed in as. Some specs, such as "Alice can't see Bob's todos", also need two actors on the channel under test. This changes `SpecContext` and the channels, not just the DSL.

**7. Controlling the system's boundaries.** Redefined to include the `TODO.md` question about stubs for external systems. Farley's principle is that a spec controls everything at the system's boundaries, so its outcome depends only on what it says. The clock is one such boundary, and any external system the app calls is another. The harness can reach the system only through its UI and API, and has no way to control what the system depends on. Nothing breaks yet, because the app has no such dependencies. This needs designing before the first due date or external call.

**8. Specs written before they are implemented.** A process gap only. With one spec in flight, the current flow works. It matters if several specs are queued at once, or parallel worktrees run on `practice`. The question above still stands: is that a case you expect to hit?

## Revisiting point 4: waiting (12 November 2026)

**4. Waiting on the API side: reopened as "needs an example".** Point 4 was closed above because the API keeps todos in memory. That was the weaker half of the reason. What matters is that the API answers synchronously: a write is committed before the response goes back, so a read straight after it sees the change. Adding a database keeps that true. A system needs waiting when it accepts a change and makes it visible later, through a queue, a background worker, a read replica, a cache or a projection.

The design already handles this. `ITodoDriver` says its queries wait for a yes before answering no, the UI driver does so through Playwright, and polling in `ApiTodoDriver` would change no spec or DSL code. So this is not a design gap. But the project has no example of it, and the project is a store of examples, so it moves alongside point 3.

Working through it settled four design points, written up in `WAITING.md`:

- Drivers wait only on observations, never on commands, so a retry can't do something twice.
- Channels with no waiting of their own use one shared polling helper in `acceptance.Drivers/Core`, not a loop in each method. The UI keeps Playwright's waiting.
- Every channel uses the same timeout. Today's `ACCEPTANCE_UI_TIMEOUT_SECONDS` becomes channel-neutral.
- A long or scheduled delay is not waited for. The spec triggers it through the harness, which makes it part of point 7.

The proposed example is bulk import, returning 202 Accepted and processing the todos in the background. `TODO.md` lists the work.


## Revisiting named parameters: checking calls (12 November 2026)

Two things Farley's `"name: value"` parameters do weren't covered above. The DSL rejects a call with an unknown parameter, too many values or a required one missing, so a spec it can't interpret fails loudly instead of being guessed at. And adding a parameter doesn't break the specs that already exist.

In C#, the compiler covers most of the first point, and does it before anything runs. An unknown argument name, too many arguments or a missing required one won't compile. Optional parameters with defaults cover the second point. That leaves three holes, each now closed:

- **Values.** The compiler can't tell that `due: "yesterdy"` is a typo. The DSL throws on any domain-language value it doesn't recognise, and says the spec is wrong, not the system. This is a rule in `.claude/roles/spec-agent.md`, because only the spec-agent writes DSL methods.
- **Positional arguments.** If a parameter is inserted into a DSL method, an existing positional call can bind a value to the wrong parameter. The spec still compiles and quietly changes meaning. So specs pass every DSL argument by name. A rule that applies only to optional arguments would make each unnamed argument a question for the reader, while a rule with no exceptions makes every unnamed argument a mistake, visible at a glance. It also means new parameters can go anywhere, not just at the end.
- **Defaults on the class.** C# takes a default from the type a call goes through. Specs call through the DSL interfaces, so a default declared only on the class is ignored by specs.

The last two are checked by an analyzer in `acceptance.Analyzers`, which both `acceptance.Specs` and `acceptance.Dsl` use. ACC001 reports an unnamed argument in a call from another project into a DSL namespace, which is how specs reach the DSL. The DSL's calls to itself are harness code and aren't checked. ACC002 reports a default on a DSL class method that implements a DSL interface. Both are errors, and both are marked as not configurable, so `#pragma` and `.editorconfig` can't turn them off. That was tested against the compiler.

Because the analyzer enforces these rules and its messages explain them, `CLAUDE.md` doesn't repeat them. `CLAUDE.md` holds only what can't be checked mechanically. It gains one general rule: never suppress or downgrade an analyzer diagnostic. That covers analyzers added later that may not be marked the same way.

The analyzer references `Microsoft.CodeAnalysis.CSharp` 4.14.0. An analyzer can't use a newer Roslyn than the compiler that loads it, and the .NET 10 SDK's compiler is newer than that.

**Correction, the same day.** The separate `CLAUDE.md` rule against suppressing analyzer diagnostics has been folded into the existing rule about hooks: "If a hook or analyzer blocks you, don't look for another way round it." Marking the rules as not configurable already stops `#pragma` and `.editorconfig`, but it doesn't stop other routes: building with `-p:RunAnalyzers=false`, removing the analyzer's reference from a project file, or changing the analyzer itself. `check-done.ps1` builds normally, so the first is caught before work counts as done, and the others show up in review. What's left is the same instruction already given for hooks, so it shares that bullet rather than adding one.
