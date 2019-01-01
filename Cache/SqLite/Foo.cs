using System;
using System.Collections.Generic;
using HC.Core.Logging;

namespace HC.Core.Cache.SqLite
{
    public class Foo : AbstractFoo
    {

        private string m_stringHidden { get; set; }
        public string m_string { get; set; }
        public int m_j { get; set; }
        public double m_d { get; set; }
        public DateTime m_jodaTime { get; set; }
        public List<String> m_list { get; set; }

        public Foo(
            string str,
            int j,
            double d) :
            this(str,
                j, d,
                DateTime.Now,
                DateTime.Now) { }

        public Foo(
                string str,
                int j,
                double d,
                DateTime date,
                DateTime jodaDate)
        {
            m_string = str;
            m_j = j;
            m_d = d;
            m_dateCol = date;
            m_jodaTime = jodaDate;
        }

        public Foo() { }

        public void setHidden(String str)
        {
            m_stringHidden = str;
        }

        public String GetHidden()
        {
            return m_stringHidden;
        }

        public static List<Foo> GetFooList(int intSize)
        {
            var fooList = new List<Foo>();

            for (int i = 0; i < intSize; i++)
            {

                String strRow = "str_" + i;
                var foo = new Foo(
                        strRow,
                        i,
                        i + 1,
                        DateTime.Now,
                        DateTime.Now);
                foo.setHidden(i + "_hidden");
                foo.m_list = new List<String>
                                 {
                                     strRow + "_a", 
                                     strRow + "_b"
                                 };
                fooList.Add(foo);
            }

            return fooList;
        }

        public bool Compare(Foo foo)
        {

            try
            {
                bool blnEquals = m_string.Equals(foo.m_string) &&
                    m_j == foo.m_j &&
                    m_d == foo.m_d &&
                    m_dateCol.Equals(foo.m_dateCol) &&
                    m_jodaTime.Equals(foo.m_jodaTime) &&
                    m_list[0].Equals(foo.m_list[0]) &&
                    m_list[1].Equals(foo.m_list[1]);

                if (!blnEquals)
                {
                    Console.WriteLine("Not equals");
                }
                return blnEquals;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }

            return false;
        }

        public String GetKey()
        {
            return "key_" + m_j;
        }

        public bool CompareSimple(Foo foo)
        {
            bool blnEquals =
                    m_j == foo.m_j &&
                    m_list[0].Equals(foo.m_list[0]) &&
                    m_list[1].Equals(foo.m_list[1]);

            return blnEquals;
        }
    }
}
