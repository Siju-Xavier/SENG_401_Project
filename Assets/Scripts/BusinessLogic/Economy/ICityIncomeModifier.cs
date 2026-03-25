namespace BusinessLogic
{
    using GameState;

    public interface ICityIncomeModifier
    {
        string ModifierName { get; }
        int Priority { get; }
        int ModifyIncome(City city, int currentIncome, int currentLevel);
        bool IsActive { get; }
    }
}
