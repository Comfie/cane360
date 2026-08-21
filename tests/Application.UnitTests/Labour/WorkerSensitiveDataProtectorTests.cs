using Cane360.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Labour;

public class WorkerSensitiveDataProtectorTests
{
    [Test]
    public void EncryptsMasksAndRoundTripsWithoutPlaintextStorage()
    {
        var protector = CreateProtector();
        var tenantId = Guid.NewGuid();
        var farmId = Guid.NewGuid();
        var workerId = Guid.NewGuid();

        var protectedValue = protector.Protect(tenantId, farmId, workerId, "63-123456-A-12");

        protectedValue.DisplayMask.ShouldBe("••••••12");
        protectedValue.Ciphertext.ShouldNotBe(System.Text.Encoding.UTF8.GetBytes("63123456A12"));
        protectedValue.FarmScopedFingerprint.Length.ShouldBe(32);
        protector.Reveal(tenantId, farmId, workerId, protectedValue.Ciphertext,
            protectedValue.Nonce, protectedValue.Tag, protectedValue.KeyId).ShouldBe("63123456A12");
    }

    [Test]
    public void FingerprintIsDeterministicInsideFarmAndDifferentAcrossFarms()
    {
        var protector = CreateProtector();
        var tenantId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var farmOne = Guid.NewGuid();
        var farmTwo = Guid.NewGuid();

        var first = protector.Protect(tenantId, farmOne, workerId, "63 123456 A 12");
        var repeat = protector.Protect(tenantId, farmOne, Guid.NewGuid(), "63-123456-a-12");
        var otherFarm = protector.Protect(tenantId, farmTwo, workerId, "63-123456-A-12");

        repeat.FarmScopedFingerprint.ShouldBe(first.FarmScopedFingerprint);
        otherFarm.FarmScopedFingerprint.ShouldNotBe(first.FarmScopedFingerprint);
        repeat.Ciphertext.ShouldNotBe(first.Ciphertext);
    }

    private static WorkerSensitiveDataProtector CreateProtector()
    {
        var values = new Dictionary<string, string?>
        {
            ["Cane360Security:NationalId:ActiveKeyId"] = "test-v1",
            ["Cane360Security:NationalId:Keys:test-v1"] = Convert.ToBase64String(Enumerable.Range(1, 32).Select(value => (byte)value).ToArray()),
            ["Cane360Security:NationalId:FingerprintKey"] = Convert.ToBase64String(Enumerable.Range(33, 32).Select(value => (byte)value).ToArray())
        };
        return new WorkerSensitiveDataProtector(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
    }
}
