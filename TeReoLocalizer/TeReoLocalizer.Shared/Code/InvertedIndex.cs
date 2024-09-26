using System.Collections.Concurrent;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.NGram;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Directory = System.IO.Directory;

namespace TeReoLocalizer.Shared.Code;

public class InvertedIndex : IDisposable
{
    private const string IndexVersion = "1.0.3";
    private const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
    
    private static readonly char[] WordDelimiters = [' ', '\t', '\n', '\r'];
    
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
        Document doc =
        [
            new StringField("id", id, Field.Store.YES),
            new TextField("content", content.ToLowerInvariant(), Field.Store.NO),
            new StringField("content_original", content, Field.Store.YES)
        ];
        
        string[] words = content.Split(WordDelimiters, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string word in words)
        {
            doc.Add(new StringField("whole_word", word.ToLowerInvariant(), Field.Store.NO));
        }

        writer.UpdateDocument(new Term("id", id), doc);
        indexedStrings[content] = id;
    }

    public void SynchronizeIndex(List<IndexDocument> sourceDocuments, IProgress<int>? progress = null)
    {
        const int reportIntervalPercent = 10;

        Dictionary<string, string> sourceDocumentMap = [];
        
        foreach (IndexDocument doc in sourceDocuments)
        {
            sourceDocumentMap[doc.Id] = doc.Content;
        }
        
        int total = sourceDocuments.Count;
        int processed = 0;
        int lastReportedProgress = 0;
    
        foreach (KeyValuePair<string, string> kvp in sourceDocumentMap)
        {
            string id = kvp.Key;
            string content = kvp.Value;

            if (!indexedStrings.TryGetValue(content, out string? existingId) || existingId != id)
            {
                IndexDocument(content, id);
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
        
        foreach (KeyValuePair<string, string> kvp in indexedStrings.ToList())
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

    public List<SearchResult> Search(string substring, bool caseSensitive = false, bool wholeWords = false, int page = 1, int pageSize = 150)
    {
        using DirectoryReader? reader = writer.GetReader(true);
        IndexSearcher searcher = new IndexSearcher(reader);
        
        Query query;
        
        if (wholeWords)
        {
            BooleanQuery booleanQuery = [];
            string[] words = substring.Split([' '], StringSplitOptions.RemoveEmptyEntries);
            
            foreach (string word in words)
            {
                TermQuery termQuery = new TermQuery(new Term("whole_word", word.ToLowerInvariant()));
                booleanQuery.Add(termQuery, Occur.MUST);
            }
            
            query = booleanQuery;
        }
        else
        {
            query = new TermQuery(new Term("content", substring.ToLowerInvariant()));
        }
        
        int start = (page - 1) * pageSize;
        
        TopDocs topDocs = searcher.Search(query, start + pageSize);
        ScoreDoc[]? hits = topDocs.ScoreDocs;

        List<SearchResult> results = [];
        
        for (int i = start; i < Math.Min(start + pageSize, hits.Length); i++)
        {
            Document? doc = searcher.Doc(hits[i].Doc);
            string content = doc.Get("content_original");
            string id = doc.Get("id");
            int matchIndex = wholeWords ? FindWholeWordMatchIndex(content, substring, caseSensitive) : content.IndexOf(substring, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

            if (matchIndex >= 0)
            {
                results.Add(new SearchResult
                {
                    Content = content,
                    Id = id,
                    MatchStartIndex = matchIndex
                });
            }
        }

        return results;
    }

    private static int FindWholeWordMatchIndex(string content, string searchString, bool caseSensitive)
    {
        StringComparison comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        string[] contentWords = content.Split(WordDelimiters, StringSplitOptions.RemoveEmptyEntries);
        string[] searchWords = searchString.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i <= contentWords.Length - searchWords.Length; i++)
        {
            bool match = true;
            
            for (int j = 0; j < searchWords.Length; j++)
            {
                if (!string.Equals(contentWords[i + j], searchWords[j], comparison))
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return content.IndexOf(contentWords[i], i is 0 ? 0 : content.IndexOf(contentWords[i - 1], StringComparison.Ordinal) + contentWords[i - 1].Length, comparison);
            }
        }

        return -1;
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
    public int MatchStartIndex { get; set; }
}

public class SearchResult
{
    public string Content { get; set; }
    public string Id { get; set; }
    public int MatchStartIndex { get; set; }
}

public class NGramAnalyzer(LuceneVersion matchVersion) : Analyzer
{
    protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
    {
        NGramTokenizer tokenizer = new NGramTokenizer(matchVersion, reader, 3, 10);
        return new TokenStreamComponents(tokenizer);
    }
}