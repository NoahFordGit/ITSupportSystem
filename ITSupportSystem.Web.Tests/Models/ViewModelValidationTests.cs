using System.ComponentModel.DataAnnotations;
using ITSupportSystem.Web.Models;

namespace ITSupportSystem.Web.Tests.Models;

/// <summary>
/// Checks the [Required], [EmailAddress], etc. attributes on the form view models
/// directly, the same way MVC model binding runs them.
/// </summary>
public class ViewModelValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static bool HasErrorFor(List<ValidationResult> results, string member) =>
        results.Any(r => r.MemberNames.Contains(member));

    private static RegisterViewModel ValidRegister() => new()
    {
        FirstName = "Jane",
        LastName = "Doe",
        Email = "jane@example.com",
        Password = "Passw0rd!"
    };

    // ---------- LoginViewModel ----------

    [Fact]
    public void Login_Valid_HasNoErrors()
    {
        var model = new LoginViewModel { Email = "jane@example.com", Password = "anything" };

        Assert.Empty(Validate(model));
    }

    [Fact]
    public void Login_Empty_RequiresEmailAndPassword()
    {
        var results = Validate(new LoginViewModel());

        Assert.True(HasErrorFor(results, nameof(LoginViewModel.Email)));
        Assert.True(HasErrorFor(results, nameof(LoginViewModel.Password)));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("jane.example.com")]
    public void Login_BadEmail_IsInvalid(string email)
    {
        var results = Validate(new LoginViewModel { Email = email, Password = "anything" });

        Assert.True(HasErrorFor(results, nameof(LoginViewModel.Email)));
    }

    [Fact]
    public void Login_RememberMe_DefaultsToFalse()
    {
        Assert.False(new LoginViewModel().RememberMe);
    }

    // ---------- RegisterViewModel ----------

    [Fact]
    public void Register_Valid_HasNoErrors()
    {
        Assert.Empty(Validate(ValidRegister()));
    }

    [Fact]
    public void Register_Empty_RequiresEveryField()
    {
        var results = Validate(new RegisterViewModel());

        Assert.True(HasErrorFor(results, nameof(RegisterViewModel.FirstName)));
        Assert.True(HasErrorFor(results, nameof(RegisterViewModel.LastName)));
        Assert.True(HasErrorFor(results, nameof(RegisterViewModel.Email)));
        Assert.True(HasErrorFor(results, nameof(RegisterViewModel.Password)));
    }

    [Fact]
    public void Register_BadEmail_IsInvalid()
    {
        var model = ValidRegister();
        model.Email = "not-an-email";

        Assert.True(HasErrorFor(Validate(model), nameof(RegisterViewModel.Email)));
    }

    [Theory]
    [InlineData(7, false)]   // one short of MinLength(8)
    [InlineData(8, true)]
    [InlineData(100, true)]
    [InlineData(101, false)] // one over MaxLength(100)
    public void Register_PasswordLength_IsChecked(int length, bool valid)
    {
        var model = ValidRegister();
        model.Password = new string('a', length);

        Assert.Equal(!valid, HasErrorFor(Validate(model), nameof(RegisterViewModel.Password)));
    }

    [Theory]
    [InlineData(nameof(RegisterViewModel.FirstName))]
    [InlineData(nameof(RegisterViewModel.LastName))]
    public void Register_NameOver100Chars_IsInvalid(string property)
    {
        var model = ValidRegister();
        typeof(RegisterViewModel).GetProperty(property)!.SetValue(model, new string('a', 101));

        Assert.True(HasErrorFor(Validate(model), property));
    }

    [Fact]
    public void Register_EmailOver256Chars_IsInvalid()
    {
        var model = ValidRegister();
        model.Email = new string('a', 250) + "@example.com";

        Assert.True(HasErrorFor(Validate(model), nameof(RegisterViewModel.Email)));
    }

    // ---------- AuthPageViewModel ----------

    [Fact]
    public void AuthPage_StartsWithEmptyFormsNotNulls()
    {
        var page = new AuthPageViewModel();

        Assert.NotNull(page.Login);
        Assert.NotNull(page.Register);
    }
}
