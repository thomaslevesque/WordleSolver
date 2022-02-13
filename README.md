# Wordle solver

Automatically solves today's [Wordle](https://www.nytimes.com/games/wordle/).

```
Usage:
  WordleSolver [options]

Options:
  -p, --browser-path <browser-path>  Browser path (defaults to the bundled browser) []
  -s, --show-browser                 Show the browser (hidden by default) [default: False]
  --version                          Show version information
  -?, -h, --help                     Show help and usage information
```

This program uses [Playwright](https://playwright.dev/dotnet/) to automatically type words and read the clues given by the game. 

By default, it uses the browser bundled with Playwright. You might need to run `playwright.ps1 install chromium` from the output directory to install the bundled browser.

Alternatively, you can specify the path to the Chrome, Chromium or Edge executable.
