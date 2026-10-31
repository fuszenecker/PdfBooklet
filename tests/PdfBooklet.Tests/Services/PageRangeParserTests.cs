using PdfBooklet.Services;

namespace PdfBooklet.Tests.Services;

[TestClass]
public sealed class PageRangeParserTests
{
    private static IReadOnlyList<int> Parse(string? text, int pageCount = 20)
    {
        Assert.IsTrue(new PageRangeParser().TryParse(text, pageCount, out var pages, out var error), error);
        return pages;
    }

    private static string Fail(string text, int pageCount = 20)
    {
        Assert.IsFalse(new PageRangeParser().TryParse(text, pageCount, out var pages, out var error));
        Assert.IsNull(pages);
        return error;
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Blank_SelectsEveryPage(string? text)
    {
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, Parse(text, 5).ToArray());
    }

    [TestMethod]
    public void SinglePagesAndRanges_AreZeroBasedInTheGivenOrder()
    {
        CollectionAssert.AreEqual(new[] { 9, 0, 1, 2, 6 }, Parse("10, 1-3; 7").ToArray());
    }

    [TestMethod]
    public void OpenRanges_RunToTheEdgesOfTheDocument()
    {
        CollectionAssert.AreEqual(new[] { 0, 1, 7, 8, 9 }, Parse("-2, 8-", 10).ToArray());
    }

    [TestMethod]
    public void WhitespaceDashesAndEmptyItems_AreTolerated()
    {
        CollectionAssert.AreEqual(new[] { 2, 3, 4, 5 }, Parse(" 3 – 4 ,, 5 —6, ").ToArray());
    }

    [TestMethod]
    public void RepeatedPages_AreKept()
    {
        CollectionAssert.AreEqual(new[] { 0, 0, 1 }, Parse("1, 1-2").ToArray());
    }

    [TestMethod]
    public void PageOutsideTheDocument_IsRejected()
    {
        StringAssert.Contains(Fail("1-21"), "Page 21 does not exist");
        StringAssert.Contains(Fail("0"), "Page 0 does not exist");
    }

    [TestMethod]
    public void BackwardsRange_IsRejected()
    {
        StringAssert.Contains(Fail("8-5"), "ends before it starts");
    }

    [TestMethod]
    [DataRow("abc")]
    [DataRow("1.5")]
    [DataRow("+3")]
    [DataRow("1-2-3")]
    [DataRow("-")]
    public void Garbage_IsRejected(string text)
    {
        Assert.IsFalse(string.IsNullOrEmpty(Fail(text)));
    }

    [TestMethod]
    public void OnlySeparators_IsRejected()
    {
        StringAssert.Contains(Fail(" , ; "), "no pages");
    }
}
