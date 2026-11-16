using System.Text.RegularExpressions;
using Microsoft.Playwright;
using acceptance.Core.Drivers;

namespace acceptance.Todos.Drivers;

// UI contract (data-testid attributes):
//   new-todo-title           text input for a new todo's title
//   new-todo-priority        select for a new todo's priority, with options
//                            low, normal and high
//   add-todo                 submit button; stays enabled, rejections show an error
//   title-too-long-warning   visible while the title being typed is too long,
//                            before the todo is submitted
//   todo-list                the list of todos, with data-loaded="true" once the
//                            todos have been fetched
//   todo-item                one per todo, with data-completed="true" or "false"
//                            and data-priority="low", "normal" or "high"
//   todo-title               the title text inside a todo-item
//   complete-todo            control inside a todo-item that marks it complete
//   delete-todo              control inside a todo-item that deletes it
//   error-message            visible when the last action was rejected

internal class UiTodoDriver(IPage page) : ITodoDriver, ITodoEntryDriver
{
    public async Task<DriverResult> Add(string title)
    {
        await page.GotoAsync("/");
        await page.GetByTestId("new-todo-title").FillAsync(title);
        await page.GetByTestId("add-todo").ClickAsync();
        return await Outcome(LocateTodo(title));
    }

    public async Task<DriverResult> Add(string title, Priority priority)
    {
        await page.GotoAsync("/");
        await page.GetByTestId("new-todo-title").FillAsync(title);
        await page.GetByTestId("new-todo-priority").SelectOptionAsync(ToUi(priority));
        await page.GetByTestId("add-todo").ClickAsync();
        return await Outcome(LocateTodo(title));
    }

    public async Task<DriverResult> Complete(string title)
    {
        await page.GotoAsync("/");
        await LocateTodo(title).GetByTestId("complete-todo").ClickAsync();
        return await Outcome(LocateCompletedTodo(title));
    }

    public async Task<DriverResult> Delete(string title)
    {
        await page.GotoAsync("/");
        var todo = LocateTodo(title);
        await todo.GetByTestId("delete-todo").ClickAsync();
        return await OutcomeOfRemoving(todo);
    }

    public async Task<bool> IsShown(string title)
    {
        await page.GotoAsync("/");
        return await Eventually(LocateTodo(title));
    }

    public async Task<bool> IsComplete(string title)
    {
        await page.GotoAsync("/");
        return await Eventually(LocateCompletedTodo(title));
    }

    public async Task<bool> HasPriority(string title, Priority priority)
    {
        await page.GotoAsync("/");
        return await Eventually(
            LocateTodo(title).And(page.Locator($"[data-priority='{ToUi(priority)}']")));
    }

    public async Task<bool> IsGone(string title)
    {
        await page.GotoAsync("/");
        await page.GetByTestId("todo-list").And(page.Locator("[data-loaded='true']"))
            .WaitForAsync(new() { State = WaitForSelectorState.Attached });
        return await Eventually(LocateTodo(title), WaitForSelectorState.Hidden);
    }

    private static string ToUi(Priority priority) => priority switch
    {
        Priority.Low => "low",
        Priority.Normal => "normal",
        Priority.High => "high",
        _ => throw new ArgumentOutOfRangeException(nameof(priority)),
    };

    // FillAsync fires an input event, which is what the title input listens to.
    public async Task TypeTitle(string title)
    {
        await page.GotoAsync("/");
        await page.GetByTestId("new-todo-title").FillAsync(title);
    }

    public Task<bool> IsTitleTooLongWarningShown() =>
        Eventually(page.GetByTestId("title-too-long-warning"));

    // Waits for a yes, so a no takes the full timeout: to check that something
    // is absent, wait for it to be hidden instead of negating this.
    private static async Task<bool> Eventually(
        ILocator locator, WaitForSelectorState state = WaitForSelectorState.Visible)
    {
        var result = await DriverResult.TryCatchAsync<TimeoutException>(
            () => locator.WaitForAsync(new() { State = state }));
        return !result.IsFailed;
    }

    private ILocator LocateTodo(string title) =>
        page.GetByTestId("todo-item").Filter(new()
        {
            Has = page
                    .GetByTestId("todo-title")
                    .Filter(new()
                    {
                        HasTextRegex = new Regex($"^{Regex.Escape(title)}$")
                    })
        });

    private ILocator LocateCompletedTodo(string title) =>
        LocateTodo(title).And(page.Locator("[data-completed='true']"));

    private async Task<DriverResult> OutcomeOfRemoving(ILocator todo)
    {
        if (await Eventually(todo, WaitForSelectorState.Hidden))
            return DriverResult.Okay;

        var error = page.GetByTestId("error-message");
        return await error.IsVisibleAsync()
            ? DriverResult.Fail(await error.InnerTextAsync())
            : DriverResult.Fail("Timed out waiting for the todo to be removed");
    }

    private async Task<DriverResult> Outcome(ILocator success)
    {
        var error = page.GetByTestId("error-message");
        var result = await DriverResult.TryCatchAsync<TimeoutException>(
            () => success.Or(error).First.WaitForAsync()
        );

        if (result.IsFailed)
            return DriverResult.Fail("Timed out waiting for either success or error");

        return await error.IsVisibleAsync()
            ? DriverResult.Fail(await error.InnerTextAsync())
            : DriverResult.Okay;
    }
}
