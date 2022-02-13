using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.Playwright;

namespace WordleSolver;

public class Solver
{
    private static readonly ImmutableArray<string> BestStartingWords =
        ImmutableArray.Create(
            "TRACE",
            "CRATE",
            "LATER",
            "ADIEU",
            "IRATE",
            "SOARE",
            "AROSE"
        );
    
    private readonly WordData _wordData;
    private readonly IBrowser _browser;

    public Solver(WordData wordData, IBrowser browser)
    {
        _wordData = wordData;
        _browser = browser;
    }

    public async Task Solve()
    {
        var page = await _browser.NewPageAsync();
        await page.GotoAsync("https://www.nytimes.com/games/wordle/");

        // Close GDPR banner if it's visible
        var closeGdpr = page.Locator("#pz-gdpr-btn-closex");
        if (await closeGdpr.IsVisibleAsync())
        {
            await closeGdpr.ClickAsync();
        }

        // Close help dialog if it's visible
        var closeHelp = page.Locator("game-modal .overlay .close-icon");
        if (await closeHelp.IsVisibleAsync())
        {
            await closeHelp.ClickAsync();
        }

        var words = _wordData.Words.ToList();
        var currentWord = BestStartingWords.PickRandom();
        var state = State.Default;

        for (int attemptNumber = 1; attemptNumber <= 6; attemptNumber++)
        {
            Console.WriteLine($"Attempt {attemptNumber}: {currentWord}");
            await SubmitWord(page, currentWord);
            var (word, result) = await ReadResult(page, attemptNumber);
            Debug.Assert(word == currentWord);
    
            if (result.All(c => c is LetterClue.Correct))
            {
                Console.WriteLine($"Guessed the word in {attemptNumber} tries.");
                return;
            }

            state = state.Update(currentWord, result);
            words.RemoveAll(w => !IsCandidate(w, state));
            if (words.Any())
            {
                currentWord = words.PickRandomWeighted(_wordData.GetWeight);
            }
            else
            {
                Console.WriteLine("Failed to guess the word: No more possible words; that's probably a bug.");
                return;
            }
        }
        
        Console.WriteLine("Failed to guess the word in 6 attempts.");
    }
    
    static bool IsCandidate(string word, State state)
    {
        // Already tried
        if (state.Attempts.Contains(word))
            return false;
        // Doesn't match letters with known positions
        if (state.KnownPositions.Any(kp => word[kp.Key] != kp.Value))
            return false;

        // Doesn't contain letter known to be in word
        foreach (var clue in state.LetterClues)
        {
            // Just check Present clues, since Correct clues were checked just before
            if (clue.Value.OfType<LetterClue.Present>().Any() && !word.Contains(clue.Key))
                return false;
        }
    
    
        for (int i = 0; i < word.Length; i++)
        {
            var letter = word[i];
            var clues = state.GetCluesForLetter(letter);
            // Contains letters we know are not in the word
            if (clues.Count > 0 && clues.All(c => c is LetterClue.Absent))
                return false;
            // Contains letters that are in the word but in the wrong position
            if (clues.Any(c => c is LetterClue.Present(var position) && position == i))
                return false;
        }
    
        return true;
    }

    static async Task SubmitWord(IPage page, string word)
    {
        await page.Keyboard.TypeAsync(word);
        await page.Keyboard.PressAsync("Enter");
        await Task.Delay(2000);
    }

    static async Task<(string Word, LetterClue[] Result)> ReadResult(IPage page, int attemptNumber)
    {
        var board = page.Locator("#board");
        var row = board.Locator($"game-row:nth-child({attemptNumber})");
        var tiles = await row.Locator("game-tile").ElementHandlesAsync();
        var word = new StringBuilder();
        var result = new LetterClue[tiles.Count];
        for (int i = 0; i < tiles.Count; i++)
        {
            var tile = tiles[i];
            var letter = await tile.GetAttributeAsync("letter");
            word.Append(letter);
            var evaluation = await tile.GetAttributeAsync("evaluation");
            result[i] = evaluation switch
            {
                "absent" => new LetterClue.Absent(),
                "present" => new LetterClue.Present(i),
                "correct" => new LetterClue.Correct(i),
                _ => throw new InvalidOperationException($"Unknown evaluation value: '{evaluation}'")
            };
        }
	
        return (word.ToString().ToUpperInvariant(), result);
    }

}