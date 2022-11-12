using Bogus;
using Customers.WebApp.Models;
using FluentAssertions;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Customers.WebApp.Tests.Integration.Pages;

[Collection("Test collection")]
public class UpdateCustomerTests : IAsyncLifetime
{
    private IPage _page;

    private readonly SharedTestContext _testContext;
    private readonly Faker<Customer> _customerGenerator = new Faker<Customer>()
        .RuleFor(x => x.Email, faker => faker.Person.Email)
        .RuleFor(x => x.FullName, faker => faker.Person.FullName)
        .RuleFor(x => x.GitHubUsername, SharedTestContext.ValidGitHubUsername)
        .RuleFor(x => x.DateOfBirth, faker => DateOnly.FromDateTime(faker.Person.DateOfBirth.Date));


    public UpdateCustomerTests(SharedTestContext testContext)
    {
        _testContext = testContext;
    }

    [Fact]
    public async Task Update_UpdateCustomer_WhenDataIsValid()
    {
        // Arrange
        _page = await _testContext.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            BaseURL = SharedTestContext.AppUrl
        });

        _ = await CreateCustomer(_page);
        var customer = _customerGenerator
            .Generate();

        var linkElement = _page.Locator("article>p>a").First;
        var link = await linkElement.GetAttributeAsync("href");
        var id = link?.Split("/").Last();
        await _page.GotoAsync($"update-customer/{id}");

        // Act
        await _page.FillAsync("input[id=fullname]", customer.FullName);
        await _page.FillAsync("input[id=email]", customer.Email);
        await _page.FillAsync("input[id=github-username]", customer.GitHubUsername);
        await _page.FillAsync("input[id=dob]", customer.DateOfBirth.ToString("yyyy-MM-dd"));

        await _page.ClickAsync("button[type=submit]");

        // Assert
        await _page.GotoAsync($"customer/{id}");

        (await _page.Locator("p[id=fullname-field]").InnerTextAsync()).Should().Be(customer.FullName);
        (await _page.Locator("p[id=email-field]").InnerTextAsync()).Should().Be(customer.Email);
        (await _page.Locator("p[id=github-username-field]").InnerTextAsync()).Should().Be(customer.GitHubUsername);
        (await _page.Locator("p[id=dob-field]").InnerTextAsync()).Should().Be(customer.DateOfBirth.ToString("dd/MM/yyyy"));
    }

    [Fact]
    public async Task Update_ShowsError_WhenEmailIsInvalid()
    {
        // Arrange
        _page = await _testContext.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            BaseURL = SharedTestContext.AppUrl
        });

        _ = await CreateCustomer(_page);
        var customer = _customerGenerator
            .Generate();

        var linkElement = _page.Locator("article>p>a").First;
        var link = await linkElement.GetAttributeAsync("href");
        var id = link?.Split("/").Last();
        await _page.GotoAsync($"update-customer/{id}");

        // Act
        await _page.FillAsync("input[id=fullname]", customer.FullName);
        await _page.FillAsync("input[id=email]", "NOT_EMAIL");
        await _page.FillAsync("input[id=github-username]", customer.GitHubUsername);
        await _page.FillAsync("input[id=dob]", customer.DateOfBirth.ToString("yyyy-MM-dd"));

        // Assert
        var element = _page.Locator("li.validation-message").First;
        var text = await element.InnerTextAsync();
        text.Should().Be("Invalid email format");
    }

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

    public async Task DisposeAsync()
    {
        if (_page is not null)
        {
            await _page.CloseAsync();
        }
    }

    public Task InitializeAsync() => Task.CompletedTask;
}

