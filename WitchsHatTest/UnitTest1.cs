using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WitchsHat;

namespace WitchsHatTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            WHAnalyzer analyzer = new WHAnalyzer();
            analyzer.Analyze(@"
var Class1 = Class.create({
    initialize: function() {
        var hoge = new Sprite(32, 32);
    }
});");
            Assert.AreEqual("Class1", analyzer.classes["Class1"].Name);
            Assert.AreEqual("initialize", analyzer.classes["Class1"].Functions[0].Name);
            Assert.AreEqual("Sprite", analyzer.localVars["hoge"]);

        }
    }
}
