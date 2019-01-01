namespace HC.Core.DynamicCompilation
{

    public class TestClass2
    {
        public double DblValue { get; set; }

        public bool BlnValue { get; set; }

        public int IntValue { get; set; }

        public string StrValue { get; set; }

        public int LngValue { get; set; }
    }

    public class MyUnknownObject
    {
        public MyUnknownObject MyUnknownObjectTest { get; set; }
        public MyUnknownObject2 MyUnknownObjectTest2 { get; set; }
        public string A { get; set; }
        public string B { get; set; }
    }
    
    public class MyUnknownObject2
    {
        public MyUnknownObject MyUnknownObjectTest { get; set; }
        public string A { get; set; }
        public string B { get; set; }
    }
}



