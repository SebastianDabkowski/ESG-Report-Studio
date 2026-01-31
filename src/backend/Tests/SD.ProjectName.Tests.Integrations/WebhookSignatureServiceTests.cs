using SD.ProjectName.Modules.Integrations.Application;

namespace SD.ProjectName.Tests.Integrations;

public class WebhookSignatureServiceTests
{
    [Fact]
    public void GenerateSignature_ShouldGenerateConsistentSignature()
    {
        // Arrange
        var service = new WebhookSignatureService();
        var payload = "test payload";
        var secret = "test-secret";

        // Act
        var signature1 = service.GenerateSignature(payload, secret);
        var signature2 = service.GenerateSignature(payload, secret);

        // Assert
        Assert.Equal(signature1, signature2);
        Assert.NotEmpty(signature1);
    }

    [Fact]
    public void GenerateSignature_ShouldGenerateDifferentSignatureForDifferentPayload()
    {
        // Arrange
        var service = new WebhookSignatureService();
        var payload1 = "test payload 1";
        var payload2 = "test payload 2";
        var secret = "test-secret";

        // Act
        var signature1 = service.GenerateSignature(payload1, secret);
        var signature2 = service.GenerateSignature(payload2, secret);

        // Assert
        Assert.NotEqual(signature1, signature2);
    }

    [Fact]
    public void GenerateSignature_ShouldGenerateDifferentSignatureForDifferentSecret()
    {
        // Arrange
        var service = new WebhookSignatureService();
        var payload = "test payload";
        var secret1 = "test-secret-1";
        var secret2 = "test-secret-2";

        // Act
        var signature1 = service.GenerateSignature(payload, secret1);
        var signature2 = service.GenerateSignature(payload, secret2);

        // Assert
        Assert.NotEqual(signature1, signature2);
    }

    [Fact]
    public void VerifySignature_ShouldReturnTrueForValidSignature()
    {
        // Arrange
        var service = new WebhookSignatureService();
        var payload = "test payload";
        var secret = "test-secret";
        var signature = service.GenerateSignature(payload, secret);

        // Act
        var isValid = service.VerifySignature(payload, signature, secret);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifySignature_ShouldReturnFalseForInvalidSignature()
    {
        // Arrange
        var service = new WebhookSignatureService();
        var payload = "test payload";
        var secret = "test-secret";
        var invalidSignature = "invalid-signature";

        // Act
        var isValid = service.VerifySignature(payload, invalidSignature, secret);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifySignature_ShouldReturnFalseForWrongSecret()
    {
        // Arrange
        var service = new WebhookSignatureService();
        var payload = "test payload";
        var secret1 = "test-secret-1";
        var secret2 = "test-secret-2";
        var signature = service.GenerateSignature(payload, secret1);

        // Act
        var isValid = service.VerifySignature(payload, signature, secret2);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void GenerateSigningSecret_ShouldGenerateUniqueSecrets()
    {
        // Arrange
        var service = new WebhookSignatureService();

        // Act
        var secret1 = service.GenerateSigningSecret();
        var secret2 = service.GenerateSigningSecret();

        // Assert
        Assert.NotEmpty(secret1);
        Assert.NotEmpty(secret2);
        Assert.NotEqual(secret1, secret2);
    }

    [Fact]
    public void GenerateVerificationToken_ShouldGenerateUniqueTokens()
    {
        // Arrange
        var service = new WebhookSignatureService();

        // Act
        var token1 = service.GenerateVerificationToken();
        var token2 = service.GenerateVerificationToken();

        // Assert
        Assert.NotEmpty(token1);
        Assert.NotEmpty(token2);
        Assert.NotEqual(token1, token2);
    }
}
