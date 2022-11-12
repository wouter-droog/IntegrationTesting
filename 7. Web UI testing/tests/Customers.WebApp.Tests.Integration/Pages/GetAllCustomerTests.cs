using Bogus;
using Customers.WebApp.Models;
using FluentAssertions;
using Microsoft.Playwright;
using Xunit;

namespace Customers.WebApp.Tests.Integration.Pages;

[Collection("Test collection")]
public class GetAllCustomerTests : IAsyncLifetime
{
    private IPage? _page;

    private readonly SharedTestContext _testContext;

    private readonly Faker<Customer> _customerGenerator = new Faker<Customer>()
        .RuleFor(x => x.Email, faker => faker.Person.Email)
        .RuleFor(x => x.FullName, faker => faker.Person.FullName)
        .RuleFor(x => x.GitHubUsername, SharedTestContext.ValidGitHubUsername)
        .RuleFor(x => x.DateOfBirth, faker => DateOnly.FromDateTime(faker.Person.DateOfBirth.Date));

    public GetAllCustomerTests(SharedTestContext testContext)
    {
        _testContext = testContext;
    }

    [Fact]
    public async Task GetAll_ReturnsCustomer_WhenCustomerExists()
    {
        // Arrange
        _page = await _testContext.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            BaseURL = SharedTestContext.AppUrl
        });
        var customer = await CreateCustomer(_page);

        // Act
        await _page.GotoAsync("customers");

        // Assert
        (await _page.Locator("//tbody/tr[1]/td[1]").InnerTextAsync()).Should().Be(customer.FullName);
        (await _page.Locator("//tbody/tr[1]/td[2]").InnerTextAsync()).Should().Be(customer.Email);
        (await _page.Locator("//tbody/tr[1]/td[3]").InnerTextAsync()).Should().Be(customer.GitHubUsername);
        (await _page.Locator("//tbody/tr[1]/td[4]").InnerTextAsync()).Should().Be(customer.DateOfBirth.ToString("dd/MM/yyyy"));
    }

    public async Task DisposeAsync()
    {
        if (_page is not null)
        {
            await _page!.CloseAsync();
        }
    }

    public Task InitializeAsync() => Task.CompletedTask;

    private async Task<Customer> CreateCustomer(IPage _page)
    {
        await _page.GotoAsync("add-customer");
        var customer = _customerGenerator.Generate();

        await _page.FillAsync("input[id=fullname]", customer.FullName);
        await _page.FillAsync("input[id=email]", customer.Email);
        await _page.FillAsync("input[id=github-username]", customer.GitHubUsername);
        await _page.FillAsync("input[id=dob]", customer.DateOfBirth.ToString("yyyy-MM-dd"));

        await _page.ClickAsync("button[type=submit]");
        return customer;
    }
}