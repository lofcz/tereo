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

    [TearDown]
    public void TearDown()
    {
        SharedIndex.Dispose();
    }

    private static InvertedIndex SetupIndex(string str)
    {
        InvertedIndex index = new InvertedIndex($".reoindex_{str}");
        
        List<IndexDocument> sourceDocuments =
        [
            new IndexDocument { Content = "public v[oi]d Method() ú", Id = "1" },
            new IndexDocument { Content = "int x = 5 * 3; ú", Id = "2" },
            new IndexDocument { Content = "if (condition) { }", Id = "3" },
            new IndexDocument { Content = "tohle je test", Id = "4" },
            new IndexDocument { Content = "if (condition2) { } v[oi]d", Id = "3" }, // explicit overwrite of an existing id
            new IndexDocument { Content = "ahoCorasick1", Id = "5" },
            new IndexDocument { Content = "ahoCorasick2", Id = "6" },
            new IndexDocument { Content = "ahoCorasick3", Id = "7" },
            new IndexDocument { Content = "ahoCorasick4", Id = "8" },
            new IndexDocument { Content = "ahoCorasick5 ú", Id = "9" },
            new IndexDocument { Content = "ahoCorasick6", Id = "10" },
            new IndexDocument { Content = "superlongtestlongerthanmaxngram", Id = "11" },
            new IndexDocument { Content = "this is a random very long text which we should be able to search and index correctly", Id = "12" }
        ];

        index.SynchronizeIndex(sourceDocuments);
        return index;
    }

    [Test]
    [TestCase("ú", 3)]
    [TestCase("X = 5 * 3", 1)]
    [TestCase("condition2", 1)]
    [TestCase("[oi]", 2)]
    [TestCase("if", 1)]
    [TestCase("ahoCorasick", 6)]
    [TestCase("uperlongtestlongerthanmaxngra", 1)]
    [TestCase("his is a random very long text which we should be able to search and index correctl", 1)]
    public void CaseInsensitive(string query, int expected)
    {
        List<SearchResult> results = SharedIndex.Search(query);
        Assert.That(results, Has.Count.EqualTo(expected));
    }
    
    [Test]
    [TestCase("X = 5 * 3", 0)]
    [TestCase("uperlongtestlongerthanmaxngra", 1)]
    [TestCase("uperlongtestlonGerthanmaxngra", 0)]
    [TestCase("his is a random very long text which we should be able to search and index correctl", 1)]
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
    [TestCase("uperlongtestlongerthanmaxngra", 0)]
    [TestCase("superlongtestlongerthanmaxngram", 1)]
    public void CaseSensitiveWholeWords(string query, int expected)
    {
        List<SearchResult> results = SharedIndex.Search(query, true, true);
        Assert.That(results, Has.Count.EqualTo(expected));
    }
}