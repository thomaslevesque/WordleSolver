namespace WordleSolver;

public class WordData
{
    private readonly IReadOnlyDictionary<string, int> _weights;

    public WordData(IReadOnlyList<string> words)
    {
        Words = words;
        var numberOfLetters = words.Count * words[0].Length;
        var letterFrequency = words
            .SelectMany(w => w)
            .GroupBy(letter => letter)
            .ToDictionary(g => g.Key, g => (int)(100.0 * g.Count() / numberOfLetters));

        int ComputeWeight(string word)
        {
            int frequencyScore = word.Sum(letter => letterFrequency[letter]);
            int distinctLetters = word.Distinct().Count();
            return frequencyScore * distinctLetters;
        }

        _weights = words.ToDictionary(word => word, ComputeWeight);
    }

    public IReadOnlyList<string> Words { get; }

    public int GetWeight(string word) => _weights[word];
}