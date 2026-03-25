using System;
using System.Collections.Generic;
using System.Text;
using Yitter.IdGenerator;

namespace Dabp.Utils.IdGenerator
{
    public class IdGeneratorConfig
    {
        public static void Init()
        {
            var options = new IdGeneratorOptions(1);
            options.BaseTime = new DateTime(2020, 1, 1);

            YitIdHelper.SetIdGenerator(options);
        }
    }
}
