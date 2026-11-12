# WAITING

How the harness waits for the system to reach the state a spec checks, and
why. Worked out on 12 November 2026, when gap analysis point 4 was reopened.
Nothing here is implemented yet; `TODO.md` tracks the work.

## What a confirmation assumes

A spec acts and then checks:

``` csharp
await Sut.Todos.Add("Buy milk");
await Sut.Todos.ConfirmShown("Buy milk");
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
- `ApiTodoDriver` meets it only because the API is synchronous. If an API
  feature becomes asynchronous, its queries have to poll.

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

## The example this still needs

No current feature is asynchronous, so nothing in the project shows any of
this yet. Faking it with an artificial delay in an ordinary endpoint would
teach the wrong lesson about where eventual consistency comes from. The
proposed example is bulk import: the API accepts a list of todos with
202 Accepted and processes it in the background, and the spec checks that the
imported todos are shown. That one feature exercises the polling helper, the
shared timeout and the UI's refresh decision.
