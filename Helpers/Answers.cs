using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualAssistant.Helpers
{
    public class Answers
    {
        public Dictionary<string, string> patternsAndAnswers { get; } = new Dictionary<string, string>()
        {
            ["YYY"] = "Asus Zenbook Pro",
            ["YYNN"] = "Asus AMD Pro",
            ["YYNYN"] = "MSI Apache Ge62",
            ["YYNYY"] = "Asus GX700",
            ["YNYY"] = "Lenovo IdeaPad 510-151KB",
            ["YNYNY"] = "Apple MacBook Air 13",
            ["YNYNN"] = "Asus N752VX"
        };

        public string GetRecommendation(string[] userAnswers)
        {
            string pattern = string.Empty;
            foreach (var item in userAnswers)
            {
                pattern += item;
            }
            if (patternsAndAnswers.ContainsKey(pattern))
            {
                return patternsAndAnswers[pattern];
            }
            else
            {
                return null;
            }
        }
    }
}
