public static class PhaseInfo
{
    public static string SceneFor(Tier t) => t.ToString();

    public static Tier? Next(Tier t) => t switch
    {
        Tier.Junior => Tier.Pleno,
        Tier.Pleno => Tier.Senior,
        _ => null
    };

    public static string Title(Tier t) => t switch
    {
        Tier.Junior => "Fase 1 - Dev Junior",
        Tier.Pleno => "Fase 2 - Dev Pleno",
        _ => "Fase 3 - Dev Senior"
    };
}
