using TeReoLocalizer.Shared.Code;

namespace TeReoLocalizer.Shared.Tests;

public class Tests
{
    private static InvertedIndex SharedIndex;
    
    [SetUp]
    public void Setup()
    {
        SharedIndex = SetupIndex("shared");
    }

    [TearDownAttribute]
    public void TearDown()
    {
        SharedIndex.Dispose();
    }

    private static InvertedIndex SetupIndex(string str)
    {
        InvertedIndex index = new InvertedIndex($"{Consts.Cfg.Repository}\\.reoindex_{str}");
        
        List<IndexDocument> sourceDocuments =
        [
            new IndexDocument { Content = "public v[oi]d Method()", Id = "1" },
            new IndexDocument { Content = "int x = 5 * 3;", Id = "2" },
            new IndexDocument { Content = "if (condition) { }", Id = "3" },
            new IndexDocument { Content = "tohle je test", Id = "4" },
            new IndexDocument { Content = "if (condition2) { } v[oi]d", Id = "3" }
        ];

        index.SynchronizeIndex(sourceDocuments);
        return index;
    }

    [Test]
    [TestCase("X = 5 * 3", 1)]
    [TestCase("condition2", 1)]
    [TestCase("[oi]", 2)]
    public void CaseInsensitive(string query, int expected)
    {
        List<SearchResult> results = SharedIndex.Search(query);
        Assert.That(results, Has.Count.EqualTo(expected));
    }
    
    [Test]
    [TestCase("X = 5 * 3", 0)]
    public void CaseSensitive(string query, int expected)
    {
        List<SearchResult> results = SharedIndex.Search(query, true);
        Assert.That(results, Has.Count.EqualTo(expected));
    }
    
    [Test]
    [TestCase("tohle je", 1)]
    [TestCase("tohle", 1)]
    [TestCase("ohle", 0)]
    [TestCase("tohl", 0)]
    [TestCase("tohle je test", 1)]
    [TestCase("tohle test", 0)]
    public void CaseSensitiveWholeWords(string query, int expected)
    {
        List<SearchResult> results = SharedIndex.Search(query, true, true);
        Assert.That(results, Has.Count.EqualTo(expected));
    }
}