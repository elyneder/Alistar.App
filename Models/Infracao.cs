using System;
using System.Collections.Generic;
using System.Text;

namespace Alistar.App.Models
{
    public class Infracao
    {
        public string JaFoiDetidoPelaPolicia { get; set; } = string.Empty;
        public string QualFoiAInfracao { get; set; } = string.Empty;
        public string OutrosAtosInfracionais { get; set; } = string.Empty;
        
    }
}
