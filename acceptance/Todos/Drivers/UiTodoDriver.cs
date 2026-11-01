using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using acceptance.Core.Spec.Drivers;

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

    public async Task ConfirmShown(string title)
    {
        await page.GotoAsync("/");
        await Expect(LocateTodo(title)).ToBeVisibleAsync();
    }

    public async Task ConfirmComplete(string title)
    {
        await page.GotoAsync("/");
        await Expect(LocateCompletedTodo(title)).ToBeVisibleAsync();
    }

    // ITodoEntryDriver

    // FillAsync fires an input event, which is what the title input listens to.
    public async Task TypeTitle(string title)
    {
        await page.GotoAsync("/");
        await page.GetByTestId("new-todo-title").FillAsync(title);
    }

    public Task ConfirmTitleTooLongWarningShown() =>
        Expect(page.GetByTestId("title-too-long-warning")).ToBeVisibleAsync();

    // Helpers

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
