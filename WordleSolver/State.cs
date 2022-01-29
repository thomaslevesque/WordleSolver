using System.Collections.Immutable;

namespace WordleSolver;

record State(
    IImmutableList<string> Attempts,
    IImmutableDictionary<char, IImmutableSet<LetterClue>> LetterClues,
    IImmutableDictionary<int, char> KnownPositions)
{
    public static State Default => new(ImmutableList<string>.Empty, ImmutableDictionary<char, IImmutableSet<LetterClue>>.Empty, ImmutableDictionary<int, char>.Empty);
	
    public State Update(string word, LetterClue[] result)
    {
        var lettersWithClues = word.Zip(result).ToArray();
		
        var letterClues = LetterClues;
        var knownPositions = KnownPositions;
        foreach (var (letter, clue) in lettersWithClues)
        {
            if (!letterClues.TryGetValue(letter, out var clues))
            {
                clues = ImmutableHashSet<LetterClue>.Empty;
            }
			
            clues = clues.Add(clue);
            letterClues = letterClues.SetItem(letter, clues);
			
            if (clue is LetterClue.Correct(int position))
            {
                knownPositions = knownPositions.SetItem(position, letter);
            }
        }

        return this with { Attempts = Attempts.Add(word), LetterClues = letterClues, KnownPositions = knownPositions };
    }
    
    public IImmutableSet<LetterClue> GetCluesForLetter(char letter) => LetterClues.TryGetValue(letter, out var clues)
        ? clues
        : ImmutableHashSet<LetterClue>.Empty;
}