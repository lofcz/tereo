using System.Collections.Concurrent;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.NGram;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Directory = System.IO.Directory;

namespace TeReoLocalizer.Shared.Code;

public class InvertedIndex : IDisposable
{
    private const string IndexVersion = "1.0";
    private const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
    
    private readonly FSDirectory directory;
    private readonly Analyzer analyzer;
    private readonly IndexWriter writer;
    private readonly ConcurrentDictionary<string, string> indexedStrings;

    public InvertedIndex(string indexPath)
    {
        if (!Directory.Exists(indexPath))
        {
            Directory.CreateDirectory(indexPath);
        }
        
        directory = FSDirectory.Open(indexPath);
        analyzer = new NGramAnalyzer(AppLuceneVersion);
        writer = new IndexWriter(directory, new IndexWriterConfig(AppLuceneVersion, analyzer)
        {
            OpenMode = OpenMode.CREATE_OR_APPEND
        });
        indexedStrings = new ConcurrentDictionary<string, string>();

        if (!IsIndexValid())
        {
            RebuildIndex();
        }
        else
        {
            LoadExistingStrings();
        }
    }

    private bool IsIndexValid()
    {
        using DirectoryReader? reader = writer.GetReader(true);
        IndexSearcher searcher = new IndexSearcher(reader);
        TermQuery query = new TermQuery(new Term("meta", "version"));
        ScoreDoc[]? hits = searcher.Search(query, 1).ScoreDocs;

        if (hits.Length == 0)
        {
            return false;
        }

        Document? doc = searcher.Doc(hits[0].Doc);
        string version = doc.Get("version");
        return version == IndexVersion; // && !HasDuplicateIds(); // disabled due to perf
    }
    
    public bool DeleteDocument(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentException("ID cannot be null or empty.", nameof(id));
        }
        
        bool documentExists = indexedStrings.Values.Contains(id);

        if (documentExists)
        {
            writer.DeleteDocuments(new Term("id", id));

            string? contentToRemove = indexedStrings.FirstOrDefault(pair => pair.Value == id).Key;
            
            if (contentToRemove is not null)
            {
                indexedStrings.TryRemove(contentToRemove, out _);
            }
            
            writer.Commit();
        }

        return documentExists;
    }

    private bool HasDuplicateIds()
    {
        using DirectoryReader? reader = writer.GetReader(true);
        IndexSearcher searcher = new IndexSearcher(reader);
        MatchAllDocsQuery query = new MatchAllDocsQuery();
        ScoreDoc[]? hits = searcher.Search(query, int.MaxValue).ScoreDocs;

        HashSet<string> ids = [];
        
        foreach (ScoreDoc? hit in hits)
        {
            Document? doc = searcher.Doc(hit.Doc);
            string id = doc.Get("id");
            
            if (!string.IsNullOrEmpty(id))
            {
                if (!ids.Add(id))
                {
                    return true; 
                }
            }
        }
        
        return false;
    }

    public void RebuildIndex()
    {
        writer.DeleteAll();
        Document versionDoc =
        [
            new StringField("meta", "version", Field.Store.YES),
            new StringField("version", IndexVersion, Field.Store.YES)
        ];
        writer.AddDocument(versionDoc);
        writer.Commit();
    }

    private void LoadExistingStrings()
    {
        using DirectoryReader? reader = writer.GetReader(true);
        IndexSearcher searcher = new IndexSearcher(reader);
        MatchAllDocsQuery query = new MatchAllDocsQuery();
        ScoreDoc[]? hits = searcher.Search(query, int.MaxValue).ScoreDocs;

        foreach (ScoreDoc? hit in hits)
        {
            Document? doc = searcher.Doc(hit.Doc);
            string content = doc.Get("content");
            string id = doc.Get("id");
            
            if (!string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(id))
            {
                indexedStrings[content] = id;
            }
        }
    }
    
    public void IndexDocument(string content, string id)
    {
        writer.UpdateDocument(new Term("id", id), new Document
        {
            new StringField("id", id, Field.Store.YES),
            new TextField("content", content, Field.Store.YES)
        });

        indexedStrings[content] = id;
    }

    public void SynchronizeIndex(List<IndexDocument> sourceDocuments, IProgress<int>? progress = null)
    {
        const int reportIntervalPercent = 10;
        
        Dictionary<string, string> sourceDocumentMap = sourceDocuments.ToDictionary(doc => doc.Id, doc => doc.Content);
        int total = sourceDocuments.Count;
        int processed = 0;
        int lastReportedProgress = 0;
        
        foreach (IndexDocument doc in sourceDocuments)
        {
            if (!indexedStrings.TryGetValue(doc.Content, out string? existingId) || existingId != doc.Id)
            {
                IndexDocument(doc.Content, doc.Id);
            }
            
            processed++;

            if (progress is not null)
            {
                int currentProgress = (processed * 100) / total;
            
                if (currentProgress >= lastReportedProgress + reportIntervalPercent)
                {
                    progress.Report(currentProgress);
                    lastReportedProgress = currentProgress;
                }   
            }
        }
        
        foreach (KeyValuePair<string, string> kvp in indexedStrings)
        {
            if (!sourceDocumentMap.ContainsKey(kvp.Value))
            {
                writer.DeleteDocuments(new Term("id", kvp.Value));
                indexedStrings.TryRemove(kvp.Key, out _);
            }
            
            processed++;

            if (progress is not null)
            {
                int currentProgress = (processed * 100) / total;
            
                if (currentProgress >= lastReportedProgress + reportIntervalPercent)
                {
                    progress.Report(currentProgress);
                    lastReportedProgress = currentProgress;
                }   
            }
        }

        writer.Commit();
    }

    public List<IndexDocument> Search(string substring)
    {
        using DirectoryReader? reader = writer.GetReader(true);
        IndexSearcher searcher = new IndexSearcher(reader);
        
        string escapedSubstring = QueryParserBase.Escape(substring);
        WildcardQuery query = new WildcardQuery(new Term("content", $"*{escapedSubstring}*"));
        ScoreDoc[]? hits = searcher.Search(query, int.MaxValue).ScoreDocs;

        List<IndexDocument> results = [];

        foreach (ScoreDoc? hit in hits)
        {
            Document? doc = searcher.Doc(hit.Doc);
            string content = doc.Get("content");
            string id = doc.Get("id");
            results.Add(new IndexDocument { Content = content, Id = id });
        }

        return results;
    }

    public List<SearchResult> SearchWithHighlights(string substring)
    {
        using DirectoryReader? reader = writer.GetReader(true);
        IndexSearcher searcher = new IndexSearcher(reader);

        string escapedSubstring = QueryParserBase.Escape(substring);

        WildcardQuery query = new WildcardQuery(new Term("content", $"*{escapedSubstring}*"));
        ScoreDoc[]? hits = searcher.Search(query, int.MaxValue).ScoreDocs;

        List<SearchResult> results = [];

        foreach (ScoreDoc? hit in hits)
        {
            Document? doc = searcher.Doc(hit.Doc);
            string content = doc.Get("content");
            string id = doc.Get("id");

            List<MatchPosition> offsets = [];

            int index = content.IndexOf(substring, StringComparison.InvariantCulture);
            while (index >= 0)
            {
                offsets.Add(new MatchPosition { Start = index, End = index + substring.Length });
                index = content.IndexOf(substring, index + 1, StringComparison.InvariantCulture);
            }

            results.Add(new SearchResult
            {
                Id = id,
                Content = content,
                Matches = offsets
            });
        }

        return results;
    }


    public void Dispose()
    {
        writer.Dispose();
        directory.Dispose();
        analyzer.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class IndexDocument
{
    public string Content { get; set; }
    public string Id { get; set; }
}

public class SearchResult
{
    public string Id { get; set; }
    public string Content { get; set; }
    public List<MatchPosition> Matches { get; set; }
}

public class MatchPosition
{
    public int Start { get; set; }
    public int End { get; set; }
}

public class NGramAnalyzer(LuceneVersion matchVersion) : Analyzer
{
    protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
    {
        NGramTokenizer tokenizer = new NGramTokenizer(matchVersion, reader, 3, 10);
        return new TokenStreamComponents(tokenizer);
    }
}