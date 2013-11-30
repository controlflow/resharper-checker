using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReSharper.Weaver.TestData;

namespace ReSharper.Weaver.Foo
{
  class Program
  {
    static void Main(string[] args)
    {
      var simpleClass = new SimpleClass();
      //simpleClass.Foo();
    }
  }
}
