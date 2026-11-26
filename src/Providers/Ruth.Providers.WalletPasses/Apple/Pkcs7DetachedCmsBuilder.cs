using System.Formats.Asn1;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Ruth.Providers.WalletPasses.Apple;

/// <summary>Pure .NET 10 RFC 5652 DER CMS / PKCS#7 SignedData detached signature generator and verifier for Apple .pkpass manifests.</summary>
public static class Pkcs7DetachedCmsBuilder
{
    private const string OidData = "1.2.840.113549.1.7.1";
    private const string OidSignedData = "1.2.840.113549.1.7.2";
    private const string OidContentTypeAttr = "1.2.840.113549.1.9.3";
    private const string OidMessageDigestAttr = "1.2.840.113549.1.9.4";
    private const string OidSigningTimeAttr = "1.2.840.113549.1.9.5";
    private const string OidSha256 = "2.16.840.1.101.3.4.2.1";
    private const string OidRsaEncryption = "1.2.840.113549.1.1.1";

    /// <summary>Creates a detached RFC 5652 DER PKCS#7 SignedData signature over <paramref name="manifestBytes" />.</summary>
    public static byte[] SignDetached(
        ReadOnlySpan<byte> manifestBytes,
        X509Certificate2 signerCertificate,
        RSA signerPrivateKey,
        X509Certificate2? wwdrIntermediateCertificate,
        DateTimeOffset signingTimeUtc
    )
    {
        ArgumentNullException.ThrowIfNull(signerCertificate);
        ArgumentNullException.ThrowIfNull(signerPrivateKey);

        var manifestDigest = SHA256.HashData(manifestBytes);
        var sortedAttrs = BuildSortedSignedAttributeEntries(manifestDigest, signingTimeUtc);
        var signedAttrsSetBytes = EncodeSignedAttributesForHashing(sortedAttrs);
        var signatureBytes = signerPrivateKey.SignData(
            signedAttrsSetBytes,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );

        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            writer.WriteObjectIdentifier(OidSignedData);
            using (
                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true))
            )
            {
                WriteSignedDataBody(
                    writer,
                    signerCertificate,
                    wwdrIntermediateCertificate,
                    sortedAttrs,
                    signatureBytes
                );
            }
        }

        return writer.Encode();
    }

    /// <summary>Verifies a detached DER PKCS#7 SignedData signature produced by <see cref="SignDetached" /> against <paramref name="manifestBytes" />.</summary>
    public static bool VerifyDetached(ReadOnlySpan<byte> manifestBytes, ReadOnlySpan<byte> cmsBytes)
    {
        var reader = new AsnReader(cmsBytes.ToArray(), AsnEncodingRules.DER);
        var contentInfo = reader.ReadSequence();
        if (
            !string.Equals(
                contentInfo.ReadObjectIdentifier(),
                OidSignedData,
                StringComparison.Ordinal
            )
        )
        {
            return false;
        }

        var explicit0 = contentInfo.ReadSequence(
            new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true)
        );
        var signedData = explicit0.ReadSequence();
        _ = signedData.ReadInteger();
        _ = signedData.ReadSetOf();
        _ = signedData.ReadSequence();

        var certsTag = new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true);
        var certsSeq = signedData.ReadSequence(certsTag);
        var signerCertDer = certsSeq.ReadEncodedValue();
        using var signerCert = X509CertificateLoader.LoadCertificate(signerCertDer.Span);
        using var rsaPublic = signerCert.GetRSAPublicKey();
        if (rsaPublic is null)
        {
            return false;
        }

        var signerInfos = signedData.ReadSetOf();
        var signerInfo = signerInfos.ReadSequence();
        _ = signerInfo.ReadInteger();
        _ = signerInfo.ReadSequence();
        _ = signerInfo.ReadSequence();

        var implicitAttrs = signerInfo.ReadEncodedValue();
        var attrsReader = new AsnReader(implicitAttrs, AsnEncodingRules.DER);
        var attrsInner = attrsReader.PeekContentBytes();
        var reconstructedSet = WrapAsDerSet(attrsInner.Span);

        if (!ContainsExpectedMessageDigest(reconstructedSet, SHA256.HashData(manifestBytes)))
        {
            return false;
        }

        _ = signerInfo.ReadSequence();
        var signature = signerInfo.ReadOctetString();
        return rsaPublic.VerifyData(
            reconstructedSet,
            signature,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
    }

    private static void WriteSignedDataBody(
        AsnWriter writer,
        X509Certificate2 signerCertificate,
        X509Certificate2? wwdrIntermediateCertificate,
        byte[][] sortedAttrs,
        byte[] signatureBytes
    )
    {
        using (writer.PushSequence())
        {
            writer.WriteInteger(1);
            using (writer.PushSetOf())
            {
                WriteAlgorithmIdentifier(writer, OidSha256);
            }

            using (writer.PushSequence())
            {
                writer.WriteObjectIdentifier(OidData);
            }

            using (
                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true))
            )
            {
                writer.WriteEncodedValue(signerCertificate.RawData);
                if (wwdrIntermediateCertificate is not null)
                {
                    writer.WriteEncodedValue(wwdrIntermediateCertificate.RawData);
                }
            }

            using (writer.PushSetOf())
            {
                WriteSignerInfo(writer, signerCertificate, sortedAttrs, signatureBytes);
            }
        }
    }

    private static void WriteSignerInfo(
        AsnWriter writer,
        X509Certificate2 signerCertificate,
        byte[][] sortedAttrs,
        byte[] signatureBytes
    )
    {
        using (writer.PushSequence())
        {
            writer.WriteInteger(1);
            using (writer.PushSequence())
            {
                writer.WriteEncodedValue(signerCertificate.IssuerName.RawData);
                var serialBigEndian = signerCertificate.SerialNumberBytes;
                writer.WriteInteger(
                    new BigInteger(serialBigEndian.Span, isUnsigned: false, isBigEndian: true)
                );
            }

            WriteAlgorithmIdentifier(writer, OidSha256);
            using (
                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true))
            )
            {
                foreach (var attr in sortedAttrs)
                {
                    writer.WriteEncodedValue(attr);
                }
            }

            WriteAlgorithmIdentifier(writer, OidRsaEncryption);
            writer.WriteOctetString(signatureBytes);
        }
    }

    private static byte[][] BuildSortedSignedAttributeEntries(
        byte[] manifestDigest,
        DateTimeOffset signingTimeUtc
    )
    {
        byte[][] entries =
        [
            EncodeAttribute(OidContentTypeAttr, w => w.WriteObjectIdentifier(OidData)),
            EncodeAttribute(OidSigningTimeAttr, w =>
                w.WriteUtcTime(signingTimeUtc.ToUniversalTime())
            ),
            EncodeAttribute(OidMessageDigestAttr, w => w.WriteOctetString(manifestDigest)),
        ];
        Array.Sort(entries, static (a, b) => a.AsSpan().SequenceCompareTo(b));
        return entries;
    }

    private static byte[] EncodeAttribute(string oid, Action<AsnWriter> writeValue)
    {
        var w = new AsnWriter(AsnEncodingRules.DER);
        using (w.PushSequence())
        {
            w.WriteObjectIdentifier(oid);
            using (w.PushSetOf())
            {
                writeValue(w);
            }
        }

        return w.Encode();
    }

    private static byte[] EncodeSignedAttributesForHashing(byte[][] sortedAttrs)
    {
        var w = new AsnWriter(AsnEncodingRules.DER);
        using (w.PushSetOf())
        {
            foreach (var attr in sortedAttrs)
            {
                w.WriteEncodedValue(attr);
            }
        }

        return w.Encode();
    }

    private static void WriteAlgorithmIdentifier(AsnWriter writer, string oid)
    {
        using (writer.PushSequence())
        {
            writer.WriteObjectIdentifier(oid);
            writer.WriteNull();
        }
    }

    private static byte[] WrapAsDerSet(ReadOnlySpan<byte> innerAttributes)
    {
        var w = new AsnWriter(AsnEncodingRules.DER);
        using (w.PushSetOf())
        {
            var reader = new AsnReader(innerAttributes.ToArray(), AsnEncodingRules.DER);
            while (reader.HasData)
            {
                w.WriteEncodedValue(reader.ReadEncodedValue().Span);
            }
        }

        return w.Encode();
    }

    private static bool ContainsExpectedMessageDigest(byte[] setBytes, byte[] expectedDigest)
    {
        var setReader = new AsnReader(setBytes, AsnEncodingRules.DER).ReadSetOf();
        while (setReader.HasData)
        {
            var attr = setReader.ReadSequence();
            if (
                string.Equals(
                    attr.ReadObjectIdentifier(),
                    OidMessageDigestAttr,
                    StringComparison.Ordinal
                )
            )
            {
                var vals = attr.ReadSetOf();
                var digest = vals.ReadOctetString();
                return CryptographicOperations.FixedTimeEquals(digest, expectedDigest);
            }
        }

        return false;
    }
}
