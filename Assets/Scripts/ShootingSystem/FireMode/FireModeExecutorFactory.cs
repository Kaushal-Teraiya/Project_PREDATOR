public static class FireModeExecutorFactory
{
    public static IFireModeExecutor Create(FireMode mode)
    {
        switch (mode)
        {
            case FireMode.Automatic:
                return new FireMode_AutomaticExecutor();

            case FireMode.SemiAuto:
                return new FireMode_SemiAutoExecutor();
            
            case FireMode.Burst:
                return new FireMode_BurstExecutor();

        }

        return null;
    }
}
