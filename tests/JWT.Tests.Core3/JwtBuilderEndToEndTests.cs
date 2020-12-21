using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FluentAssertions;
using JWT.Algorithms;
using JWT.Builder;
using JWT.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JWT.Tests
{
    [TestClass]
    public class JwtBuilderEndToEndTests
    {
        [TestMethod]
        public void Encode_and_Decode_With_Certificate()
        {
            using var rsa = RSA.Create();
            rsa.FromXmlString(TestData.ServerRsaPrivateKey);

            using var certPub = new X509Certificate2(Encoding.ASCII.GetBytes(TestData.ServerRsaPublicKey2));
            using var certPubPriv = new X509Certificate2(certPub.CopyWithPrivateKey(rsa).Export(X509ContentType.Pfx));

            var builder = new JwtBuilder();
            var algorithm = new RS256Algorithm(certPubPriv);

            const string iss = "test";
            var exp = new DateTimeOffset(2038, 1, 19, 3, 14, 8, 0, TimeSpan.Zero).ToUnixTimeSeconds();

            var token = builder.WithAlgorithm(algorithm)
                               .AddHeader(HeaderName.KeyId, certPub.Thumbprint)
                               .AddHeader(HeaderName.X5c, new[] { Convert.ToBase64String(certPub.Export(X509ContentType.Cert)) })
                               .AddClaim("iss", iss)
                               .AddClaim("exp", exp)
                               .AddClaim(nameof(Customer.FirstName), TestData.Customer.FirstName)
                               .AddClaim(nameof(Customer.Age), TestData.Customer.Age)
                               .Encode();

            token.Should()
                 .NotBeNullOrEmpty("because the token should contains some data");
            token.Split('.')
                 .Should()
                 .HaveCount(3, "because the token should consist of three parts");

            var header = builder.DecodeHeader<JwtHeader>(token);

            header.Type
                  .Should()
                  .Be("JWT");
            header.Algorithm
                  .Should()
                  .Be("RS256");
            header.KeyId
                  .Should()
                  .Be(TestData.ServerRsaPublicThumbprint1);

            var jwt = builder.WithAlgorithm(algorithm)
                             .MustVerifySignature()
                             .Decode<Dictionary<string, object>>(token);

            jwt["iss"].Should().Be(iss);
            jwt["exp"].Should().Be(exp);
            jwt[nameof(Customer.FirstName)].Should().Be(TestData.Customer.FirstName);
            jwt[nameof(Customer.Age)].Should().Be(TestData.Customer.Age);
        }
    }
}
