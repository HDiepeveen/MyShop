using MyShop.Infrastructure.Notifications;

namespace MyShop.Infrastructure.Tests;

public sealed class EmailHtmlTests
{
    [Theory]
    [InlineData("http://127.0.0.1:4200/winkel/e-mail-bevestigen?userId=123&token=a%2Bb%3D")]
    [InlineData("https://shop.example/winkel/wachtwoord-herstellen?token=a%2Fb&userId=123")]
    public void Links_preserve_tokens_and_encode_query_separators(string link)
    {
        var encoded = System.Net.WebUtility.HtmlEncode(link);
        Assert.Contains($"<a href=\"{encoded}\">{encoded}</a>", EmailHtml.FromPlainText($"Open:\r\n\r\n{link}\nEinde"));
    }

    [Fact]
    public void Customer_text_is_encoded_and_line_breaks_preserved()
    {
        var html = EmailHtml.FromPlainText("<script>alert('x')</script>\r\nPrijs & btw\njavascript:alert(1)");
        Assert.DoesNotContain("<script>", html);
        Assert.DoesNotContain("<a ", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("<br>\nPrijs &amp; btw<br>", html);
    }
}
