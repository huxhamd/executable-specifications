# WAITING

How the harness waits for the system to reach the state a spec checks, and
why. Worked out on 12 November 2026, when gap analysis point 4 was reopened,
and extended on 16 November with what checking for absence needs. Deleting a
todo shows waiting for absence; polling isn't implemented yet, and `TODO.md`
tracks it.

## What a confirmation assumes

A spec acts and then checks:

``` csharp
await Sut.Todos.Add(title: "Buy milk");
await Sut.Todos.ConfirmShown(title: "Buy milk");
```

Checking once is only safe if the system guarantees that, once it has
accepted an action, a read sees the result. This is called read-your-writes.

## Synchronous and eventually consistent systems

**Synchronous.** The request arrives, the change is committed, and only then
does the response go back. By the time the driver sees success, the change is
visible. The API works this way today. Adding a database doesn't change this,
as long as the write is committed before the response is sent.

**Eventually consistent.** The request arrives, the system accepts it and
responds straight away, often with 202 Accepted, and the change becomes
visible later. A driver that reads immediately sees the old state. The system
isn't wrong; the check came too early.

The delay comes in two forms, which look the same to a spec:

- the write itself is deferred, for example on a queue that a background
  worker hasn't processed yet;
- the write has happened, but reads come from somewhere that lags behind,
  such as a read replica, a cache or a CQRS projection.

## How long the delay is decides the approach

- **Short and bounded**, from milliseconds to a few seconds: the driver waits.
  It repeats the observation until it holds or the timeout passes. Specs and
  the DSL don't change.
- **Long or scheduled**, such as a nightly batch or a reconciliation job: the
  spec doesn't wait. A test can't sensibly wait hours, and long timeouts make
  failures slow and the suite flaky. The spec triggers the process through the
  harness instead, the same way it would control the clock. That belongs to
  gap analysis point 7, controlling the system's boundaries.

## Waiting belongs to the drivers

In the four layers, the spec says what should be true, the DSL translates and
judges, and the driver knows how to talk to a channel. How and when a channel
shows a change is part of that "how", so:

- Specs and the DSL never mention timing: no delays, retries or timeouts.
- The driver contract already states the rule. `ITodoDriver` says its queries
  wait for a yes before answering no.
- `UiTodoDriver` meets the rule through Playwright. Its private `Eventually`
  waits on a locator, up to the timeout set on the browser context.
- `ApiTodoDriver` meets it by checking once, because the API promises
  read-your-writes and its contract says so. If an API feature drops that
  promise, its queries have to poll.

## Wait on observations, never on commands

Repeating a read is safe. Repeating a POST can do the thing twice, such as
adding a todo twice. So waiting goes in:

- driver queries, such as `IsShown` and `IsComplete`;
- lookups a command needs before it acts, such as `Complete` finding the
  todo's id.

It never goes in the command itself, and never in a retry policy on the
`HttpClient`, which would apply to writes as well.

## One polling helper for channels that don't wait by themselves

A polling loop is mechanism. Written separately in each driver method, it
drifts: different timeouts, different intervals, a forgotten delay that
hammers the API. Instead, one helper in shared driver infrastructure,
`acceptance.Drivers/Core`, does the waiting.

It returns a `bool`, like the driver queries, so the DSL still makes the
assertion and writes the failure message in domain language. The UI driver
already has a private method called `Eventually`, so the shared helper needs
a different name. `Poll.Until` is used here:

``` csharp
internal static class Poll
{
    public static async Task<bool> Until(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            if (await condition()) return true;
            if (DateTime.UtcNow >= deadline) return false;
            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }
    }
}
```

A driver query then says only what it checks:

``` csharp
public Task<bool> IsShown(string title) =>
    Poll.Until(async () => await Find(title) is not null, timeout);
```

Checking that something is absent is the same helper with the opposite
condition. It waits for the absence itself, which is why negating "wait until
shown" is wrong (gap analysis point 1):

``` csharp
public Task<bool> IsGone(string title) =>
    Poll.Until(async () => await Find(title) is null, timeout);
```

Waiting alone isn't enough for absence, though. "Absence needs a view you can
trust", below, explains why, and what this check would need as well.

## One timeout, a waiting mechanism per channel

**The timeout is shared.** It is how long the system is allowed to take, which
is a property of the system, not of a channel. If channels used different
timeouts, a spec could pass on one and fail on another for reasons that have
nothing to do with behaviour. Today the only timeout is
`ACCEPTANCE_UI_TIMEOUT_SECONDS` (default 5 seconds), and only the UI channel
uses it. When the API driver starts waiting, it becomes a channel-neutral
setting, such as `ACCEPTANCE_TIMEOUT_SECONDS`, that `AcceptanceTestBase` passes
to every channel.

**The mechanism isn't.** The UI keeps Playwright's waiting, which understands
the DOM, visibility and loading and is better than a generic loop. Wrapping
Playwright calls in `Poll.Until` would wait twice or replace a smart wait with
a weaker one. The polling helper is for channels with no waiting of their own:
the API now, and later perhaps a message bus, a command line or a direct check
for a boundary spec.

## The UI and pages that don't update by themselves

Playwright's retries only help if the page changes. `UiTodoDriver.IsShown`
opens `/` once and then waits. If the backend lags and the page doesn't update
by itself, it waits on a stale page until the timeout.

That makes a product question explicit:

- If users are expected to refresh to see the change, the driver reloads
  inside the wait, using the polling helper.
- If the page should update by itself, the driver must not reload, because
  reloading would hide a missing live update.

Decide this per feature and record the choice in the driver.

## Checking once states a promise

It's tempting to poll every query, on every channel, for consistency: a
synchronous system passes on the first attempt, and an asynchronous one is
covered too. But checking once is itself a check of a guarantee.

The API promises read-your-writes: once a write's response arrives, a read sees
it. Real API clients rely on that, for example by adding a todo and then
listing todos. If a cache or a read replica later made the API eventually
consistent by accident, a driver that checks once would turn the specs red, and
rightly, because a promise to clients was broken. A driver that always polled
would wait out the lag and stay green while those clients broke. It's the same
reasoning as not reloading a page that should update by itself.

So whether a query waits follows from what its channel promises, and the
promise is written in the driver's contract. The consistency the suite needs is
one rule (wait in queries, never in commands), one helper and one timeout, not
the same mechanism everywhere.

## Waiting for something to go

Every wait for something to appear has a mirror image: waiting for it to go.
Negating a wait for presence doesn't give you that:

``` csharp
Assert.False(await Todos.IsShown(title));   // wrong
```

- If the todo really is gone, `IsShown` waits the full timeout before
  answering no, so the spec passes, slowly.
- If the view is a moment behind and still shows the todo, `IsShown` answers
  yes at once, so the spec fails even though deleting works.

The driver contract needs its own query that waits for the absence itself:
`IsGone`. In the UI it waits for the element to be hidden, through the same
`Eventually` with a different wait state. Through the API, which promises
read-your-writes, it checks once.

## Absence needs a view you can trust

Every absence check answers two separate questions:

1. **Waiting: how long do I keep looking?** That depends on what the channel
   promises, as above.
2. **Trust: can I believe what I'm looking at?** Is this view of the system
   complete and up to date?

The second matters only when the answer you hope for is "nothing there". A view
that is empty or out of date looks exactly like "gone". For a presence check,
an untrustworthy view only means "not found yet", which costs time but can
never cause a wrong pass. For an absence check, it can pass against a broken
system, and waiting doesn't help, because the wait succeeds at once.

The two questions combine independently:

| | View trustworthy | View not trustworthy |
|---|---|---|
| **Check once** | The API today: one synchronous read returns the complete, current list | An API reading from a lagging copy: "gone" can be wrong |
| **Wait** | The UI with a loaded marker: wait for the list, then for the todo to go | The UI without one: waits, but "gone" succeeds at once on the empty list |

So an absence check first needs positive evidence that its view can be
trusted, made against that same view:

- **Through the API today**, a synchronous read is the evidence: the response
  is the complete, current list.
- **In the UI**, a page shows an empty list until it has fetched the todos.
  The list carries a loaded marker, and `IsGone` waits for it before waiting
  for the todo to go.
- **In an eventually consistent system**, the driver can't tell on its own
  whether a read has caught up. The system has to make that observable, for
  example by returning a version with each write that the read side can be
  asked about. That's a testability requirement to design in.

This evidence belongs in the driver, not the spec. A spec that confirms a todo
is shown before deleting it puts timing into the spec, and doesn't help
anyway: the later absence check makes a fresh observation, such as a new page
load, which the earlier sighting says nothing about.

## The example this still needs

Deleting a todo shows waiting for absence and trusting the view. No current
feature is asynchronous, though, so nothing shows polling yet. Faking it with an artificial delay in an ordinary endpoint would
teach the wrong lesson about where eventual consistency comes from. The
proposed example is bulk import: the API accepts a list of todos with
202 Accepted and processes it in the background, and the spec checks that the
imported todos are shown. That one feature exercises the polling helper, the
shared timeout and the UI's refresh decision.
