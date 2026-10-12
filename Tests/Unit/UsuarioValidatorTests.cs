using FluentAssertions;
using Shared.Helpers;

namespace Tests.Unit;

public class UsuarioValidatorTests
{
    [Fact]
    public void DatosValidos_NoDevuelveError()
    {
        UsuarioValidator.ObtenerError("Carolina", "carolina@gmail.com").Should().BeNull();
    }

    [Theory]
    [InlineData(null, "a@b.com")]
    [InlineData("", "a@b.com")]
    [InlineData("   ", "a@b.com")]
    [InlineData("Carolina", null)]
    [InlineData("Carolina", "")]
    [InlineData("Carolina", "correo-invalido")]
    [InlineData("Carolina", "Otro <a@b.com>")]
    public void DatosInvalidos_DevuelveError(string? nombre, string? email)
    {
        UsuarioValidator.ObtenerError(nombre, email).Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void NombreMayorA100Caracteres_DevuelveError()
    {
        UsuarioValidator.ObtenerError(new string('a', 101), "a@b.com").Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void EmailMayorA150Caracteres_DevuelveError()
    {
        var email = new string('a', 145) + "@b.com";
        UsuarioValidator.ObtenerError("Carolina", email).Should().NotBeNullOrEmpty();
    }
}
