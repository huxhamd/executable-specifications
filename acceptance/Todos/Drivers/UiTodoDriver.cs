using System.Text.RegularExpressions;
using Microsoft.Playwright;
using acceptance.Core.Drivers;

namespace acceptance.Todos.Drivers;

// UI contract (data-testid attributes):
//   new-todo-title           text input for a new todo's title
//   add-todo                 submit button; stays enabled, rejections show an error
//   title-too-long-warning   visible while the title being typed is too long,
//                            before the todo is submitted
//   todo-item                one per todo, with data-completed="true" or "false"
//   todo-title               the title text inside a todo-item
//   complete-todo            control inside a todo-item that marks it complete
//   error-message            visible when the last action was rejected

// One driver for every todo capability the browser offers. UiChannel hands
// the same instance out once per capability.
public class UiTodoDriver(IPage page) : ITodoDriver, ITodoEntryDriver
{
    // ITodoDriver

    public async Task<DriverResult> Add(string title)
    {
        await page.GotoAsync("/");
        await page.GetByTestId("new-todo-title").FillAsync(title);
        await page.GetByTestId("add-todo").ClickAsync();
        return await Outcome(LocateTodo(title));
    }

    public async Task<DriverResult> Complete(string title)
    {
        await page.GotoAsync("/");
        await LocateTodo(title).GetByTestId("complete-todo").ClickAsync();
        return await Outcome(LocateCompletedTodo(title));
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

    // ITodoEntryDriver

    // FillAsync fires an input event, which is what the title input listens to.
    public async Task TypeTitle(string title)
    {
        await page.GotoAsync("/");
        await page.GetByTestId("new-todo-title").FillAsync(title);
    }

    public Task<bool> IsTitleTooLongWarningShown() =>
        Eventually(page.GetByTestId("title-too-long-warning"));

    // Helpers

    // Whether the locator becomes visible within the page's default timeout.
    // Waits for a yes, so a no takes the full timeout: to check that something
    // is absent, wait for it to be hidden instead of negating this.
    private static async Task<bool> Eventually(ILocator locator)
    {
        var result = await DriverResult.TryCatchAsync<TimeoutException>(
            () => locator.WaitForAsync());
        return !result.IsFailed;
    }

    // Exact match title, so "" only matches a todo whose title is really blank.
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

    // Wait for either the expected change, or the error.
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
