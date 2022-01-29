using System.Diagnostics;
using System.Text;
using Microsoft.Playwright;
using WordleSolver;

var words = LoadWords();
var weightFunction = GetWeightFunction(words);

var bestStartingWords = new[]
{
    "TRACE",
    "CRATE",
    "LATER",
    "ADIEU",
    "IRATE",
    "SOARE",
    "AROSE",
};

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    ExecutablePath = FindChromeExe(),
    Headless = false,
});

var page = await browser.NewPageAsync();
await page.GotoAsync("https://www.powerlanguage.co.uk/wordle/");
var closeHelp = page.Locator("game-modal .overlay .close-icon");
await closeHelp.ClickAsync();

var currentWord = bestStartingWords.PickRandom();
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
        return 0;
    }

    state = state.Update(currentWord, result);
    words.RemoveAll(w => !IsCandidate(w, state));
    if (words.Any())
    {
        currentWord = words.PickRandomWeighted(weightFunction);
    }
    else
    {
        Console.WriteLine("Failed to guess the word: No more possible words; that's probably a bug.");
        return 1;
    }
}

Console.WriteLine("Failed to guess the word in 6 attempts.");
return 1;

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


static Func<string, int> GetWeightFunction(IList<string> words)
{
    var numberOfLetters = words.Count * words[0].Length;
    var letterFrequency = words
        .SelectMany(w => w)
        .GroupBy(letter => letter)
        .ToDictionary(g => g.Key, g => (int)(100.0 * g.Count() / numberOfLetters));
    var distinctLetterCount = words.ToDictionary(word => word, word => word.Distinct().Count());
    
    return word =>
    {
        int frequencyScore = word.Sum(letter => letterFrequency[letter]);
        int distinctLetters = distinctLetterCount[word];
        return frequencyScore * distinctLetters;
    };
}

static List<string> LoadWords()
{
    using var stream = typeof(Program).Assembly.GetManifestResourceStream("WordleSolver.words.txt")!;
    using var reader = new StreamReader(stream);
    var list = new List<string>();
    while (reader.ReadLine() is string word)
    {
        list.Add(word.ToUpperInvariant());
    }

    return list;
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

static string FindChromeExe()
{
    var folders = new[]
    {
        Environment.SpecialFolder.ProgramFiles,
        Environment.SpecialFolder.ProgramFilesX86,
    };
    const string relativePath = @"Google\Chrome\Application\chrome.exe";
    foreach (var folder in folders)
    {
        var folderPath = Environment.GetFolderPath(folder);
        var path = Path.Combine(folderPath, relativePath);
        if (File.Exists(path))
            return path;
    }
    
    throw new InvalidOperationException("Chrome.exe cannot be found");
}
