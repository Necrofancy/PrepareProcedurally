namespace Necrofancy.PrepareProcedurally.Solving.StateEdits;

public static class EditSequence
{
    public static IDisposable Apply(IEnumerable<Hack> hacks)
    {
        var context = new EditContext(hacks);
        context.Apply();
        return context;
    }

    private class EditContext(IEnumerable<Hack> hacks) : IDisposable
    {
        private readonly Stack<Hack> hackStack = new();

        public void Apply()
        {
            try
            {
                foreach (var hack in hacks)
                {
                    hack.Do();
                    hackStack.Push(hack);
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            while (hackStack.Count > 0)
            {
                var hack = hackStack.Pop();
                try
                {
                    hack.Undo();
                }
                catch (Exception e)
                {
                    Logging.Error($"Error undoing generation-steering edit ({hack.Description}):{e}");
                }
            }
        }
    }
}
