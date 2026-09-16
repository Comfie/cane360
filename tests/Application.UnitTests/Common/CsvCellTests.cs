using System.Globalization;
using Cane360.Application.Common.Models;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Common;

public sealed class CsvCellTests
{
    [TestCase("=1+1")]
    [TestCase("+SUM(A1:A2)")]
    [TestCase("-1+2")]
    [TestCase("@SUM(A1:A2)")]
    [TestCase("  =1+1")]
    [TestCase("\tformula")]
    [TestCase("\rformula")]
    [TestCase("\nformula")]
    public void UntrustedSpreadsheetControlTextIsNeutralized(string value)
    {
        CsvCell.Text(value).ShouldBe($"\"'{value}\"");
    }

    [Test]
    public void TextPreservesUnicodeQuotesDelimitersAndNewlines()
    {
        CsvCell.Text("Café, \"field\"\nrecord").ShouldBe("\"Café, \"\"field\"\"\nrecord\"");
        CsvCell.Text(null).ShouldBe("\"\"");
    }

    [Test]
    public void AuthoritativeNegativeNumbersAndDatesRemainInvariantUnderCommaDecimalCulture()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CsvCell.Number(-1234.56m).ShouldBe("-1234.56");
            CsvCell.Number(null).ShouldBe(string.Empty);
            CsvCell.Date(new DateOnly(2041, 11, 5)).ShouldBe("2041-11-05");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
