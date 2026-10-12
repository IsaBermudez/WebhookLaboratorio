using FluentAssertions;
using Shared.Helpers;

namespace Tests.Unit;

public class HmacHelperTests
{
    private const string Secret = "secret";
    private const string Payload = "hello";
    // HMAC-SHA256("hello", clave "secret") calculado de forma independiente.
    private const string FirmaConocida = "88aab3ede8d3adf94d26ab90d3bafd4a2083070c3bcce9c014ee04a443847c0b";

    [Fact]
    public void CalcularFirma_DevuelveVectorConocido()
    {
        HmacHelper.CalcularFirma(Payload, Secret).Should().Be(FirmaConocida);
    }

    [Fact]
    public void FirmaEsValida_ConFirmaCorrecta_DevuelveTrue()
    {
        HmacHelper.FirmaEsValida(Payload, Secret, FirmaConocida).Should().BeTrue();
    }

    [Fact]
    public void FirmaEsValida_AceptaHexadecimalEnMayusculas()
    {
        HmacHelper.FirmaEsValida(Payload, Secret, FirmaConocida.ToUpperInvariant()).Should().BeTrue();
    }

    [Fact]
    public void FirmaEsValida_ConBodyAlterado_DevuelveFalse()
    {
        HmacHelper.FirmaEsValida("hello!", Secret, FirmaConocida).Should().BeFalse();
    }

    [Fact]
    public void FirmaEsValida_ConOtraClave_DevuelveFalse()
    {
        HmacHelper.FirmaEsValida(Payload, "otra-clave", FirmaConocida).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-es-hexadecimal")]
    [InlineData("abcd")]
    public void FirmaEsValida_ConFirmaAusenteOMalformada_DevuelveFalse(string? firma)
    {
        HmacHelper.FirmaEsValida(Payload, Secret, firma).Should().BeFalse();
    }
}
