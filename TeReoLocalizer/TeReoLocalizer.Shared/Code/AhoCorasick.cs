namespace TeReoLocalizer.Shared;

public class AhoCorasick
{
    class TrieNode
    {
        public Dictionary<char, TrieNode> Children { get; } = new Dictionary<char, TrieNode>();
        public TrieNode? Failure { get; set; }
        public TrieNode? Output { get; set; }
        public bool IsEndOfWord { get; set; }
        public string? Word { get; set; }
    }

    readonly TrieNode root = new TrieNode();

    public void AddPattern(string pattern)
    {
        TrieNode node = root;
        foreach (char c in pattern)
        {
            if (!node.Children.TryGetValue(c, out TrieNode? child))
            {
                child = new TrieNode();
                node.Children[c] = child;
            }
            node = child;
        }
        node.IsEndOfWord = true;
        node.Word = pattern;
    }

    public void BuildFailureAndOutputLinks()
    {
        Queue<TrieNode> queue = new Queue<TrieNode>();

        foreach (TrieNode child in root.Children.Values)
        {
            child.Failure = root;
            queue.Enqueue(child);
        }

        while (queue.Count > 0)
        {
            TrieNode current = queue.Dequeue();

            foreach ((char c, TrieNode? child) in current.Children)
            {
                queue.Enqueue(child);

                TrieNode? failure = current.Failure!;
                
                while (failure != null && !failure.Children.ContainsKey(c))
                {
                    failure = failure.Failure;
                }

                child.Failure = failure?.Children.GetValueOrDefault(c) ?? root;
                child.Output = child.Failure.IsEndOfWord ? child.Failure : child.Failure.Output;
            }
        }
    }

    public List<(int Index, string Pattern)> Search(string text)
    {
        List<(int, string)> results = [];
        TrieNode current = root;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            while (current != root && !current.Children.ContainsKey(c))
            {
                current = current.Failure!;
            }

            if (current.Children.TryGetValue(c, out TrieNode? next))
            {
                current = next;
            }

            if (current.IsEndOfWord)
            {
                results.Add((i - current.Word!.Length + 1, current.Word));
            }

            TrieNode? output = current.Output;
            while (output != null)
            {
                results.Add((i - output.Word!.Length + 1, output.Word));
                output = output.Output;
            }
        }

        return results;
    }
}