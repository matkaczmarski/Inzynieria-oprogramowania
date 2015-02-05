using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ServerConsole
{


    /// <uwagi/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.mini.pw.edu.pl/ucc/")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.mini.pw.edu.pl/ucc/", IsNullable = false)]
    public partial class Register
    {

        private RegisterType typeField;

        private string[] solvableProblemsField;

        private byte parallelThreadsField;

        /// <uwagi/>
        public RegisterType Type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <uwagi/>
        [System.Xml.Serialization.XmlArrayItemAttribute("ProblemName", IsNullable = false)]
        public string[] SolvableProblems
        {
            get
            {
                return this.solvableProblemsField;
            }
            set
            {
                this.solvableProblemsField = value;
            }
        }

        /// <uwagi/>
        public byte ParallelThreads
        {
            get
            {
                return this.parallelThreadsField;
            }
            set
            {
                this.parallelThreadsField = value;
            }
        }
    }

    /// <uwagi/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.mini.pw.edu.pl/ucc/")]
    public enum RegisterType
    {

        /// <uwagi/>
        TaskManager,

        /// <uwagi/>
        ComputationalNode,
    }

}
