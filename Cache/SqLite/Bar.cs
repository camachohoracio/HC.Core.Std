using System;
using System.Collections.Generic;

namespace HC.Core.Cache.SqLite
{
    public class Bar : Foo
    {
        public DateTime m_dateFoo { get; set; }

        public Bar(
                String str,
                int j,
                double d,
                DateTime dateFoo) :

            base(str, j, d, DateTime.Now,
                    DateTime.Now)
        {

            m_dateFoo = dateFoo;
        }

        public Bar()
        {
        }

        public static List<Bar> getBarList(int intSize)
        {

            List<Bar> list = new List<Bar>();

            for (int i = 0; i < intSize; i++)
            {

                String strRow = "str_" + i;
                Bar item = new Bar(
                        strRow,
                        i,
                        i + 1,
                        DateTime.Now);
                item.setHidden(i + "_hidden");
                item.m_list = new List<String>();
                item.m_list.Add(strRow + "_a");
                item.m_list.Add(strRow + "_b");
                list.Add(item);
            }

            return list;
        }
    }
}
