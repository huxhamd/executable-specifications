using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace acceptance.Drivers.Todos;

// UI contract (data-testid attributes):
//   new-todo-title     text input for a new todo's title
//   add-todo           submit button; stays enabled, rejections show an error
//   todo-item          one per todo, with data-completed="true" or "false"
//   todo-title         the title text inside a todo-item
//   complete-todo      control inside a todo-item that marks it complete
//   error-message      visible when the last action was rejected

public class UiTodoDriver(IPage page) : ITodoDriver
{
    public async Task<DriverResult> AddTodo(string title)
    {
        await page.GotoAsync("/");
        await page.GetByTestId("new-todo-title").FillAsync(title);
        await page.GetByTestId("add-todo").ClickAsync();
        return await Outcome(LocateTodo(title));
    }

    public async Task<DriverResult> CompleteTodo(string title)
    {
        await page.GotoAsync("/");
        await LocateTodo(title).GetByTestId("complete-todo").ClickAsync();
        return await Outcome(LocateCompletedTodo(title));
    }

    public async Task ConfirmTodoIsShown(string title)
    {
        await page.GotoAsync("/");
        await Expect(LocateTodo(title)).ToBeVisibleAsync();
    }

    public async Task ConfirmTodoIsComplete(string title)
    {
        await page.GotoAsync("/");
        await Expect(LocateCompletedTodo(title)).ToBeVisibleAsync();
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