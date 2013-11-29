using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReSharper.Weaver.Fody
{
    public class Class1
    {
      void M()
      {
        
      }

      /*
   
   [NotNull] on fields - check at ctor end, maybe in method
   [NotNull] on return value - before .ret
   [NotNull] on out parameters - read and check
   [NotNull] on nullable? бред
   [NotNull] on parameters - check first
   [NotNull] on field in ctor
   
   */
    }
}
