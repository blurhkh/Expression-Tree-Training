using System;

namespace DeepCopyByExpressionTree
{
    class Program
    {
        static void Main(string[] args)
        {
            var a = new A
            {
                AProp1 = 1,
                AProp2 = "test1",
                B = new B
                {
                    BProp1 = 2,
                    BProp2 = "testb"
                }
            };

            var a2 = DeepCopyUtils.Copy<A, A>(a);

            var c = DeepCopyUtils.Copy<A, C>(a);
        }
    }
}
