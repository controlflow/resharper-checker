using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReSharper.Weaver.TestData;

namespace ReSharper.Weaver.Tests
{
  [TestClass]
  public class UnitTest1
  {


    [TestMethod]
    public void TestMethod1()
    {
      var assembly = typeof (Class1).Assembly;
      var fullName = MockAssemblyResolver.FindPath(assembly);




      Console.WriteLine(fullName);
    }
  }
}