public class GiftResult
{
    public bool success;
    public int affectionGain;
    public string reactionText;

    public GiftResult(bool success, int affectionGain, string reactionText)
    {
        this.success = success;
        this.affectionGain = affectionGain;
        this.reactionText = reactionText;
    }
}