using System;

/// <summary>
///     Money struct represending copper, silver, and gold.
///     Stores the total amount internally as totalCopper
/// </summary>
public struct Money
{
    private readonly int copper;
    private readonly uint gold;
    private readonly int silver;

    private readonly long totalCopper;

    public Money(long totalCopperAmount)
    {
        if (totalCopperAmount < 0)
            totalCopperAmount = 0;

        totalCopper = totalCopperAmount;
        gold = (uint) Math.Floor(totalCopperAmount/(10000.0));
        totalCopperAmount -= (long) gold*10000;
        silver = (int) Math.Floor((totalCopperAmount/(100.0)));
        copper = (int) (totalCopperAmount - silver*100);
    }

    public Money(int copper, int silver, uint gold)
    {
        this.gold = gold;
        this.silver = silver;
        this.copper = copper;

        totalCopper = copper + silver*100 + gold*10000;
    }

    public int Copper
    {
        get { return copper; }
    }

    public int Silver
    {
        get { return silver; }
    }

    public uint Gold
    {
        get { return gold; }
    }

    public override string ToString()
    {
        string str = "";
        if (gold > 0)
            str += gold + " Gold ";
        if (silver > 0)
            str += silver + " Silver ";
        if (copper > 0)
            str += copper + " Copper ";

        return str.Trim();
    }

    public long TotalCopper()
    {
        return totalCopper;
    }

    public static Money operator +(Money money1, Money money2)
    {
        return new Money(money1.totalCopper + money2.totalCopper);
    }

    public static Money operator -(Money money1, Money money2)
    {
        return new Money(money1.totalCopper - money2.totalCopper);
    }
}