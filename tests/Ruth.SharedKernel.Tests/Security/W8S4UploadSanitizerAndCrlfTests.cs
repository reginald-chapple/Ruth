using System.Text;
using Ruth.SharedKernel.Security;
using Xunit;

namespace Ruth.SharedKernel.Tests.Security;

public sealed class W8S4UploadSanitizerAndCrlfTests
{
    private static readonly byte[] _validPng =
    [
        0x89,
        0x50,
        0x4E,
        0x47,
        0x0D,
        0x0A,
        0x1A,
        0x0A,
        0x00,
        0x00,
        0x00,
        0x0D,
        0x49,
        0x48,
        0x44,
        0x52,
    ];

    private static readonly byte[] _validPdf = "%PDF-1.7\n1 0 obj\n<<>>\nendobj\n%%EOF"u8.ToArray();

    private static readonly byte[] _validDocxZip = [0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00];

    [Fact]
    public void FileContentSecurityInspector_AcceptsValidMagicByteFormats()
    {
        var png = FileContentSecurityInspector.InspectAndValidateUpload(
            _validPng,
            "receipt-scan.png",
            "image/png"
        );
        Assert.True(png.IsValid);
        Assert.Equal("image/png", png.CanonicalMimeType);

        var pdf = FileContentSecurityInspector.InspectAndValidateUpload(
            _validPdf,
            "irs-determination-letter.pdf",
            "application/pdf"
        );
        Assert.True(pdf.IsValid);
        Assert.Equal("application/pdf", pdf.CanonicalMimeType);

        var docx = FileContentSecurityInspector.InspectAndValidateUpload(
            _validDocxZip,
            "grant-report.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        );
        Assert.True(docx.IsValid);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            docx.CanonicalMimeType
        );

        var csv = FileContentSecurityInspector.InspectAndValidateUpload(
            "donor_id,amount\n101,50.00\n"u8,
            "import-batch.csv",
            "text/csv"
        );
        Assert.True(csv.IsValid);

        var checkImg = FileContentSecurityInspector.ValidateCheckImage(_validPng, "image/png");
        Assert.True(checkImg.IsValid);
    }

    [Theory]
    [InlineData("../etc/passwd.png")]
    [InlineData("..\\..\\Windows\\win.ini.png")]
    [InlineData("%2e%2e%2fsecret.png")]
    [InlineData("%252e%252e%252fsecret.png")]
    [InlineData(".htaccess")]
    [InlineData("subdir/logo.png")]
    [InlineData("drive:logo.png")]
    public void FileContentSecurityInspector_RejectsPathTraversalFileNames(string maliciousName)
    {
        var result = FileContentSecurityInspector.InspectAndValidateUpload(
            _validPng,
            maliciousName,
            "image/png"
        );
        Assert.False(result.IsValid);
        Assert.Equal("Core.BinaryFile.PathTraversalDetected", result.FailureCode);
    }

    [Fact]
    public void FileContentSecurityInspector_BlocksSvgHtmlPolyglotExecutableAndMimeSpoofing()
    {
        var svgPayload = "<svg xmlns=\"http://www.w3.org/2000/svg\" onload=\"alert(1)\"></svg>"u8;
        Assert.False(
            FileContentSecurityInspector
                .InspectAndValidateUpload(svgPayload, "badge.svg", "image/svg+xml")
                .IsValid
        );
        Assert.False(
            FileContentSecurityInspector
                .InspectAndValidateUpload(svgPayload, "badge.png", "image/png")
                .IsValid
        );
        Assert.False(
            FileContentSecurityInspector
                .InspectAndValidateUpload(_validPng, "double.svg.png", "image/png")
                .IsValid
        );

        var pngScriptPolyglot = _validPng
            .Concat("<script>fetch('/api/auth/me')</script>"u8.ToArray())
            .ToArray();
        var polyglotRes = FileContentSecurityInspector.InspectAndValidateUpload(
            pngScriptPolyglot,
            "avatar.png",
            "image/png"
        );
        Assert.False(polyglotRes.IsValid);
        Assert.Equal("Core.BinaryFile.ActiveContentBlocked", polyglotRes.FailureCode);

        byte[] peExe = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00];
        Assert.False(
            FileContentSecurityInspector
                .InspectAndValidateUpload(peExe, "photo.png", "image/png")
                .IsValid
        );

        var spoofedPdfWithPngMagic = FileContentSecurityInspector.InspectAndValidateUpload(
            _validPng,
            "statement.pdf",
            "application/pdf"
        );
        Assert.False(spoofedPdfWithPngMagic.IsValid);
        Assert.Equal("Core.BinaryFile.MimeMagicMismatch", spoofedPdfWithPngMagic.FailureCode);

        Assert.False(
            FileContentSecurityInspector.ValidateCheckImage(pngScriptPolyglot, "image/png").IsValid
        );
        Assert.False(
            FileContentSecurityInspector.ValidateCheckImage(_validPng, "application/pdf").IsValid
        );
    }

    [Theory]
    [InlineData("<script>alert('xss')</script><p>Hello</p>")]
    [InlineData("<ScRiPt src=\"https://evil.test/x.js\"></ScRiPt><p>Hello</p>")]
    [InlineData("<img src=\"x\" onerror=\"alert(1)\" /><p>Hello</p>")]
    [InlineData("<svg><animate onbegin=\"alert(1)\" attributeName=\"x\" /></svg><p>Hello</p>")]
    [InlineData("<math><mtext><script>alert(1)</script></mtext></math><p>Hello</p>")]
    [InlineData("<details open ontoggle=\"alert(1)\"><p>Hello</p></details>")]
    [InlineData("<iframe srcdoc=\"<script>alert(1)</script>\"></iframe><p>Hello</p>")]
    [InlineData("<a href=\"javascript:alert(1)\">Hello</a>")]
    [InlineData("<a href=\"&#106;avascript:alert(1)\">Hello</a>")]
    [InlineData("<a href=\"java&#x09;script:alert(1)\">Hello</a>")]
    [InlineData("<a href=\"data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==\">Hello</a>")]
    [InlineData("<a href=\"//evil.example/phish\">Hello</a>")]
    [InlineData(
        "<p style=\"color:#222;width:expression(alert(1));background:url(javascript:1)\">Hello</p>"
    )]
    public void AllowlistHtmlSanitizer_StripsOwaspXssPayloads_WhilePreservingSafeContent(
        string payload
    )
    {
        var sanitized = AllowlistHtmlSanitizer.SanitizeFragment(payload);

        Assert.Contains("Hello", sanitized, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<svg", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<math", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<iframe", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onbegin", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ontoggle", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("data:text/html", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("//evil.example", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expression(", sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AllowlistHtmlSanitizer_EnforcesNoopenerOnBlankLinks_AndPreservesSafeMergeUrlTokens()
    {
        var linkHtml =
            "<a href=\"https://charity.example.org/donate\" target=\"_blank\" style=\"color: #0f6cbd; position: fixed\">Donate</a>";
        var clean = AllowlistHtmlSanitizer.SanitizeFragment(linkHtml);
        Assert.Contains("rel=\"noopener noreferrer\"", clean, StringComparison.Ordinal);
        Assert.Contains("style=\"color:#0f6cbd\"", clean, StringComparison.Ordinal);
        Assert.DoesNotContain("position", clean, StringComparison.OrdinalIgnoreCase);

        var templateHtml = "<a href=\"{{ConfirmUrl}}\">Confirm Account</a>";
        Assert.Contains(
            "href=\"{{ConfirmUrl}}\"",
            AllowlistHtmlSanitizer.SanitizeFragment(templateHtml, allowMergeTokensInUrls: true),
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "{{ConfirmUrl}}",
            AllowlistHtmlSanitizer.SanitizeFragment(templateHtml, allowMergeTokensInUrls: false),
            StringComparison.Ordinal
        );

        var tableAndCommentsHtml =
            "<!-- comment --><!DOCTYPE html><table width=100% cellpadding='4'><tr align=left><td colspan=2 disabled>Cell</td></tr></table><dangling";
        var tableClean = AllowlistHtmlSanitizer.SanitizeFragment(tableAndCommentsHtml);
        Assert.DoesNotContain("comment", tableClean, StringComparison.Ordinal);
        Assert.DoesNotContain("DOCTYPE", tableClean, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "<table width=\"100%\" cellpadding=\"4\">",
            tableClean,
            StringComparison.Ordinal
        );
        Assert.Contains("<td colspan=\"2\">Cell</td>", tableClean, StringComparison.Ordinal);
        Assert.Contains("&lt;dangling", tableClean, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("donor@example.org\r\nBcc: spy@evil.test", true)]
    [InlineData("donor@example.org\nSubject: Injected", true)]
    [InlineData("donor@example.org%0d%0aBcc:spy@evil.test", true)]
    [InlineData("donor@example.org%0aCc:spy@evil.test", true)]
    [InlineData("\"Attacker\" <donor@example.org>", false)]
    [InlineData("donor@example.org, second@example.org", false)]
    public void EmailHeaderSanitizer_RejectsCrlfAndSmtpEnvelopeInjectionInAddresses(
        string rawEmail,
        bool hasCrlfInjection
    )
    {
        Assert.Equal(hasCrlfInjection, EmailHeaderSanitizer.ContainsHeaderInjection(rawEmail));
        Assert.False(EmailHeaderSanitizer.TrySanitizeEmailAddress(rawEmail, out _));

        var subject = EmailHeaderSanitizer.SanitizeHeaderValue(
            "Receipt #104\r\nBcc: spy@evil.test%0d%0aX-Injected: 1"
        );
        Assert.DoesNotContain('\r', subject);
        Assert.DoesNotContain('\n', subject);
        Assert.DoesNotContain("%0d", subject, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("%0a", subject, StringComparison.OrdinalIgnoreCase);

        var valid = new StringBuilder("supporter+2026@grace-foundation.org").ToString();
        Assert.True(EmailHeaderSanitizer.TrySanitizeEmailAddress(valid, out var normalized));
        Assert.Equal("supporter+2026@grace-foundation.org", normalized);
    }
}
