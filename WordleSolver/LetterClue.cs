namespace WordleSolver;

public abstract record LetterClue
{
    public record Absent : LetterClue;
    public record Present(int Position) : LetterClue;
    public record Correct(int Position): LetterClue;
}