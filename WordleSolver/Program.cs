using System.CommandLine;
using Microsoft.Playwright;
using WordleSolver;

var browserPathOption = new Option<string?>(
    new[] { "--browser-path", "-p" },
    () => null,
    "Browser path (defaults to the bundled browser)");

var showBrowserOption = new Option<bool>(
    new[] { "--show-browser", "-s" },
    () => false,
    "Show the browser (hidden by default)");

var rootCommand = new RootCommand("Solves today's Wordle")
{
    browserPathOption,
    showBrowserOption,
};
rootCommand.SetHandler(async (string? browserPath, bool showBrowser) =>
{
    var words = LoadWords();
    using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    {
        ExecutablePath = browserPath,
        Headless = !showBrowser,
    });

    var wordData = new WordData(words);
    var solver = new Solver(wordData, browser);
    await solver.Solve();
}, browserPathOption, showBrowserOption);

rootCommand.Invoke(args);

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
