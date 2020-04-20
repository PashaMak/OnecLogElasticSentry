using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnecLogElastic
{
    public class ForTest
    {
        public string[] ParseStringToArrayObject(string line)
        {
            string[] result;

            LineLog logData = new LineLog();
            result = logData.ParseStringToArrayObject(line);

            return result;
        }
    }
}
