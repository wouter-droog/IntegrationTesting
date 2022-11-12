using Bogus;
using Customers.WebApp.Models;
using FluentAssertions;
using Microsoft.Playwright;
using Xunit;

namespace Customers.WebApp.Tests.Integration.Pages;

[Collection("Test collection")]
public class GetCustomerTests : IAsyncLifetime
{
    private IPage? _page;

    private readonly SharedTestContext _testContext;
    private readonly Faker<Customer> _customerGenerator = new Faker<Customer>()
        .RuleFor(x => x.Email, faker => faker.Person.Email)
        .RuleFor(x => x.FullName, faker => faker.Person.FullName)
        .RuleFor(x => x.GitHubUsername, SharedTestContext.ValidGitHubUsername)
        .RuleFor(x => x.DateOfBirth, faker => DateOnly.FromDateTime(faker.Person.DateOfBirth.Date));
    
    public GetCustomerTests(SharedTestContext testContext)
    {
        _testContext = testContext;
    }

    [Fact]
    public async Task Get_ReturnsCustomer_WhenCustomerExists()
    {
        // Arrange
        _page = await _testContext.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            BaseURL = SharedTestContext.AppUrl
        });
        var customer = await CreateCustomer(_page);
        
        // Act
        var linkElement = _page.Locator("article>p>a").First;
        var link = await linkElement.GetAttributeAsync("href");
        await _page.GotoAsync(link!);
        
        // Assert
        (await _page.Locator("p[id=fullname-field]").InnerTextAsync()).Should().Be(customer.FullName);
        (await _page.Locator("p[id=email-field]").InnerTextAsync()).Should().Be(customer.Email);
        (await _page.Locator("p[id=github-username-field]").InnerTextAsync()).Should().Be(customer.GitHubUsername);
        (await _page.Locator("p[id=dob-field]").InnerTextAsync()).Should().Be(customer.DateOfBirth.ToString("dd/MM/yyyy"));
    }

    [Fact]
    public async Task Get_ReturnsNoCustomer_WhenNoCustomerExists()
    {
        // Arrange
        var _page = await _testContext.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            BaseURL = SharedTestContext.AppUrl
        });
        var customerUrl = $"{SharedTestContext.AppUrl}/customer/{Guid.NewGuid()}";

        // Act
        await _page.GotoAsync(customerUrl);

        // Assert
        (await _page.Locator("p").First.InnerTextAsync())
            .Should().Be("No customer found with this id");
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
