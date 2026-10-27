using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace acceptance.Drivers;

// UI contract (data-testid attributes):
//   new-todo-title           text input for a new todo's title (see UiTodoDriver)
//   title-too-long-warning   visible while the title being typed is too long,
//                            before the todo is submitted

public class UiTodoTextInputDriver(IPage page) : ITodoTextInputDriver
{
    // FillAsync fires an input event, which is what the title input listens to.
    public async Task TypeTodoTitle(string title)
    {
        await page.GotoAsync("/");
        await page.GetByTestId("new-todo-title").FillAsync(title);
    }

    public Task ConfirmTitleTooLongWarningIsShown() =>
        Expect(page.GetByTestId("title-too-long-warning")).ToBeVisibleAsync();
}
