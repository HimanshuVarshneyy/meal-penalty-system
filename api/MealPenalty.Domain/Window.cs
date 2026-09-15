namespace MealPenalty.Domain;

public class Window
{
    public int Index { get; set; }
    public decimal Start { get; set; }
    public decimal End { get; set; }
    public decimal Length => End - Start;
}
