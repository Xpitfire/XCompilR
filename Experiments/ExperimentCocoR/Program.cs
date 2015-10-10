namespace VeApps.Experiments
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser p = new Parser(new Scanner("BuildTestFiles/Demo.cs"));
            p.Parse();
        }
    }
}
