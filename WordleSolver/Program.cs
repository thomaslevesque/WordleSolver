using Microsoft.Playwright;
using WordleSolver;

var words = LoadWords();
using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    ExecutablePath = FindChromeExe(),
    Headless = false,
});

var wordData = new WordData(words);
var solver = new Solver(wordData, browser);
await solver.Solve();

static IReadOnlyList<string> LoadWords()
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
