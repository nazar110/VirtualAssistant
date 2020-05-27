using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualAssistant.Models
{
    public class UserProfile
    {
        public string Name { get; set; }
        public string Multimedia { get; set; }
        public string ForGames { get; set; }
        public string ThinScreen { get; set; }
        public string NvidiaCard { get; set; }
        public string WithWaterCooling { get; set; }
        public string ForWorkWithImages { get; internal set; }
    }
}

