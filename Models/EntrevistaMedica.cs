using System;
using System.Collections.Generic;
using System.Text;

namespace Alistar.App.Models
{
    public class EntrevistaMedica
    {
        public string Altura { get; set; } = string.Empty;
        public string Peso { get; set; } = string.Empty;
        public string ProblemaPostura { get; set; } = string.Empty;
        public string ObservacaoProblemaPostura { get; set; } = string.Empty;
        public string DificuldadeVisualOuPrecisaOculos { get; set; } = string.Empty;
        public string TesteAuditivoAlterado { get; set; } = string.Empty;
        public string ObservacaoTesteAuditivo { get; set; } = string.Empty;
        public string PressaoArterial { get; set; } = string.Empty;
        public string FrequenciaCardiaca { get; set; } = string.Empty;
        public string Respiracao { get; set; } = string.Empty;
        public string FamiliaTemDoencasGraves { get; set; } = string.Empty;
        public string JaTeveProblemaCardiacoOuRespiratorio { get; set; } = string.Empty;
        public string JaTeveAnsiedadeDepressaoOuAcompanhamentoPsicologico { get; set; } = string.Empty;
        public string TemDificuldadeParaDormir { get; set; } = string.Empty; 
    }
}
