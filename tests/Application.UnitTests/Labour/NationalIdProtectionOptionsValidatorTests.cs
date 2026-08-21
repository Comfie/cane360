using Cane360.Infrastructure.Security;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Labour;

public class NationalIdProtectionOptionsValidatorTests
{
    private readonly NationalIdProtectionOptionsValidator _validator = new();

    [Test]
    public void AcceptsDistinctThirtyTwoByteKeys()
    {
        var result = _validator.Validate(null, ValidOptions());

        result.Succeeded.ShouldBeTrue();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void RejectsMissingActiveKeyId(string? activeKeyId)
    {
        var options = ValidOptions(activeKeyId: activeKeyId);

        var result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
    }

    [Test]
    public void RejectsActiveKeyIdThatDoesNotResolve()
    {
        var options = ValidOptions(activeKeyId: "missing-key");

        var result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
    }

    [TestCase("not-base64")]
    [TestCase("AQID")]
    public void RejectsInvalidEncryptionKey(string encryptionKey)
    {
        var options = ValidOptions(encryptionKey: encryptionKey);

        var result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
    }

    [TestCase("not-base64")]
    [TestCase("AQID")]
    public void RejectsInvalidFingerprintKey(string fingerprintKey)
    {
        var options = ValidOptions(fingerprintKey: fingerprintKey);

        var result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
    }

    [Test]
    public void RejectsMatchingKeysWithoutIncludingValuesInFailure()
    {
        var key = Encode(1);
        var options = ValidOptions(encryptionKey: key, fingerprintKey: key);

        var result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotContain(key);
    }

    private static NationalIdProtectionOptions ValidOptions(
        string? activeKeyId = "local-v1",
        string? encryptionKey = null,
        string? fingerprintKey = null)
    {
        return new NationalIdProtectionOptions
        {
            ActiveKeyId = activeKeyId!,
            Keys = new Dictionary<string, string>
            {
                ["local-v1"] = encryptionKey ?? Encode(1)
            },
            FingerprintKey = fingerprintKey ?? Encode(33)
        };
    }

    private static string Encode(int start) =>
        Convert.ToBase64String(Enumerable.Range(start, 32).Select(value => (byte)value).ToArray());
}
