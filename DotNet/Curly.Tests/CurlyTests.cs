using NUnit.Framework;

namespace Curly.Test
{
    [TestFixture]
    public class CurlyTests
    {
        [Test]
        [TestCase("{or:true,false}")]
        [TestCase("{or:True,False}")]
        [TestCase("{and:True,True}")]
        [TestCase("{any:True;False;false,True}")]
        [TestCase("{any:b;c;d,b}")]
        [TestCase("{all:a;a;a,a}")]
        [TestCase("{all:{any:True;False;false,True};{all:a;a;a,a};{all:True;True;True,True},True}")]
        public void BoolTrueTests(string testcase)
        {
            Assert.True(CurlyDsl.TryEvaluate(testcase, out bool result));
            Assert.True(result);
            Assert.AreEqual(CurlyDsl.ExpandOne(testcase), "True");
        }

        [Test]
        [TestCase("{or:ue,fa}")]
        [TestCase("{and:True,False}")]
        [TestCase("{any:never;ever;again,this}")]
        [TestCase("{any:b;c;d,y}")]
        [TestCase("{all:never;ever;again,again}")]
        [TestCase("{all:{or:ue,fa};{all:a;a;a,a};{all:never;ever;again,again};{any:b;c;d,y},True}")]
        public void BoolFalseTests(string testcase)
        {
            Assert.True(CurlyDsl.TryEvaluate(testcase, out bool result));
            Assert.False(result);
            Assert.AreEqual(CurlyDsl.ExpandOne(testcase), "False");
        }
    }
}
