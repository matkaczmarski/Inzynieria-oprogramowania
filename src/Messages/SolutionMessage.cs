﻿using System;
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
    public partial class Solutions
    {

        private string problemTypeField;

        private ulong idField;

        private byte[] commonDataField;

        private SolutionsSolution[] solutions1Field;

        /// <uwagi/>
        public string ProblemType
        {
            get
            {
                return this.problemTypeField;
            }
            set
            {
                this.problemTypeField = value;
            }
        }

        /// <uwagi/>
        public ulong Id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <uwagi/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "base64Binary")]
        public byte[] CommonData
        {
            get
            {
                return this.commonDataField;
            }
            set
            {
                this.commonDataField = value;
            }
        }

        /// <uwagi/>
        [System.Xml.Serialization.XmlArrayAttribute("Solutions")]
        [System.Xml.Serialization.XmlArrayItemAttribute("Solution", IsNullable = false)]
        public SolutionsSolution[] Solutions1
        {
            get
            {
                return this.solutions1Field;
            }
            set
            {
                this.solutions1Field = value;
            }
        }
    }

    /// <uwagi/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.mini.pw.edu.pl/ucc/")]
    public partial class SolutionsSolution
    {

        private ulong taskIdField;

        private bool taskIdFieldSpecified;

        private bool timeoutOccuredField;

        private SolutionsSolutionType typeField;

        private ulong computationsTimeField;

        private byte[] dataField;

        /// <uwagi/>
        public ulong TaskId
        {
            get
            {
                return this.taskIdField;
            }
            set
            {
                this.taskIdField = value;
            }
        }

        /// <uwagi/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool TaskIdSpecified
        {
            get
            {
                return this.taskIdFieldSpecified;
            }
            set
            {
                this.taskIdFieldSpecified = value;
            }
        }

        /// <uwagi/>
        public bool TimeoutOccured
        {
            get
            {
                return this.timeoutOccuredField;
            }
            set
            {
                this.timeoutOccuredField = value;
            }
        }

        /// <uwagi/>
        public SolutionsSolutionType Type
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
        public ulong ComputationsTime
        {
            get
            {
                return this.computationsTimeField;
            }
            set
            {
                this.computationsTimeField = value;
            }
        }

        /// <uwagi/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "base64Binary")]
        public byte[] Data
        {
            get
            {
                return this.dataField;
            }
            set
            {
                this.dataField = value;
            }
        }
    }

    /// <uwagi/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.mini.pw.edu.pl/ucc/")]
    public enum SolutionsSolutionType
    {

        /// <uwagi/>
        Ongoing,

        /// <uwagi/>
        Partial,

        /// <uwagi/>
        Final,
    }

}
