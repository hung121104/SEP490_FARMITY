public static class BookAnimations
{
    public const string TurnRToL = "turnRToL";
    public const string TurnLToR = "turnLToR";
    public const string ResetState = "resetState";
}

public enum BookAnimation
{
    TurnRToL,
    TurnLToR,
    ResetState
}

public static class BookAnimationExtensions
{
    public static string ToTriggerName(this BookAnimation animation)
    {
        return animation switch
        {
            BookAnimation.TurnRToL => BookAnimations.TurnRToL,
            BookAnimation.TurnLToR => BookAnimations.TurnLToR,
            BookAnimation.ResetState => BookAnimations.ResetState,
            _ => BookAnimations.TurnLToR
        };
    }
}
